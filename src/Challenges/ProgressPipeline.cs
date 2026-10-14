using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace Challenges
{
    /// <summary>
    /// Progress work between the game thread and a single background worker: in-flight counts, worker shadows
    /// and their seeds, the patch queue and saves deferred until a steam id is idle. Never calls into the game;
    /// the owner prunes archive states before a seed (<paramref name="prune"/>) and plays the game-side part of
    /// every applied patch (<paramref name="onApplied"/>). Everything except the worker runs on the game thread.
    /// </summary>
    internal sealed partial class ProgressPipeline(
        PlayerArchive archive,
        Action<PlayerState, string> prune,
        Action<ProgressPatch, PlayerState> onApplied)
    {
        public const int ApplyBatchSize = 32;
        private static readonly TimeSpan WorkerJoinTimeout = TimeSpan.FromSeconds(5);

        private readonly Channel<ProgressWorkItem> _work =
            Channel.CreateUnbounded<ProgressWorkItem>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            });

        private readonly ConcurrentQueue<ProgressPatch> _patches = new();
        /// <summary>In-flight targets per steam id. Game thread only.</summary>
        private readonly Dictionary<string, int> _pending =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Worker-side progress snapshots, kept across bursts. The worker reads and writes; the game thread
        /// removes an entry only when its pending count is zero, so no item for that steam id is in flight.
        /// </summary>
        private readonly ConcurrentDictionary<string, PlayerState> _shadows =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>
        /// Archive <see cref="PlayerState.ProgressVersion"/> each shadow was seeded from. The archive keeps one
        /// state object per steam id for the plugin's life, so the version alone identifies the seed.
        /// Game thread only.
        /// </summary>
        private readonly Dictionary<string, int> _seeds =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Shadows to drop (with their seed version) once their steam id goes idle. Game thread only.</summary>
        private readonly HashSet<string> _discardWhenIdle =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Saves deferred until the steam id has no pending work. Game thread only.</summary>
        private readonly Dictionary<string, Action> _saveWhenIdle =
            new(StringComparer.OrdinalIgnoreCase);

        private Task? _workerTask;
        private bool _flushed;

        /// <summary>True after <see cref="Flush"/>; no work is accepted any more.</summary>
        public bool IsFlushed => _flushed;

        private void EnsureWorker() =>
            _workerTask ??= Task.Factory.StartNew(
                WorkerLoop,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

        private async Task WorkerLoop()
        {
            try
            {
                await foreach (ProgressWorkItem item in _work.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    try
                    {
                        SimulateWorkItem(item);
                    }
                    catch (Exception ex)
                    {
                        Log($"progress worker failed: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"progress worker stopped: {ex.Message}");
            }
        }

        /// <summary>
        /// Completes the work channel, joins the worker, and applies remaining patches on the caller
        /// (main thread). Must run before the owner is destroyed and before player saves on unload.
        /// </summary>
        public void Flush()
        {
            if (_flushed)
            {
                return;
            }

            _flushed = true;
            _work.Writer.TryComplete();
            try
            {
                if (_workerTask != null && !_workerTask.Wait(WorkerJoinTimeout))
                {
                    Log("progress worker did not finish in time");
                }
            }
            catch (Exception ex)
            {
                Log($"progress worker join failed: {ex.Message}");
            }

            DrainPatches(int.MaxValue);
            FlushDeferredSaves();
            _shadows.Clear();
            _seeds.Clear();
            _discardWhenIdle.Clear();
            _pending.Clear();
        }

        /// <summary>True when this steam id still has unapplied progress work.</summary>
        public bool HasPending(string steamId) =>
            SteamIds.IsCreditable(steamId)
            && _pending.TryGetValue(steamId, out int count)
            && count > 0;

        /// <summary>
        /// Runs <paramref name="save"/> now when idle, otherwise after the last patch for this steam id.
        /// Returns true when the save was deferred.
        /// </summary>
        public bool WhenIdle(string steamId, Action save)
        {
            if (!HasPending(steamId))
            {
                save();
                return false;
            }

            _saveWhenIdle[steamId] = save;
            return true;
        }

        /// <summary>Counts every target as in flight, seeds idle steam ids when stale, and hands the item to the worker.</summary>
        public bool TryEnqueue(ProgressWorkItem item)
        {
            if (_flushed)
            {
                return false;
            }

            EnsureWorker();
            foreach (ProgressTarget target in item.Targets)
            {
                // Only an idle steam id may be reseeded: in-flight work means the shadow is ahead of the archive.
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_pending, target.SteamId, out _);
                if (++count == 1)
                {
                    target.Seed = SeedIfStale(target.SteamId, item.ScheduleKey);
                }
            }

            // Cannot fail: the channel is unbounded and only Flush completes it, after setting _flushed.
            _work.Writer.TryWrite(item);
            return true;
        }

        /// <summary>
        /// Game thread, steam id idle: a fresh snapshot when the worker has no shadow for it yet or the
        /// archive changed outside the pipeline since the last seed; otherwise null (keep the shadow).
        /// </summary>
        private ProgressSeed? SeedIfStale(string steamId, string scheduleKey)
        {
            PlayerState state = archive.GetOrCreate(steamId);
            prune(state, scheduleKey);
            if (_seeds.TryGetValue(steamId, out int version) && version == state.ProgressVersion)
            {
                return null;
            }

            _seeds[steamId] = state.ProgressVersion;
            return new ProgressSeed
            {
                Challenges = ChallengeProgress.CloneChallenges(state.Challenges),
                Solved = state.Statistics.AmountChallengesSolved,
            };
        }

        /// <summary>Worker side: hands a result to the game thread. Never calls into the game.</summary>
        private void PostPatch(ProgressPatch patch) => _patches.Enqueue(patch);

        /// <summary>
        /// Game thread, once per server tick: applies up to <see cref="ApplyBatchSize"/> patches with changes.
        /// Completion-only patches do not count against the budget.
        /// </summary>
        public void OnTick()
        {
            if (_patches.IsEmpty || _flushed)
            {
                return;
            }

            DrainPatches(ApplyBatchSize);
        }

        private void DrainPatches(int max)
        {
            int applied = 0;
            while (applied < max && _patches.TryDequeue(out ProgressPatch? patch))
            {
                if (patch.HasChanges)
                {
                    applied++;
                }

                try
                {
                    ApplyPatch(patch);
                }
                catch (Exception ex)
                {
                    Log($"progress apply failed: {ex.Message}");
                    DiscardShadow(patch.SteamId);
                    FinishPending(patch.SteamId);
                }
            }
        }

        private void ApplyPatch(ProgressPatch patch)
        {
            if (!patch.HasChanges)
            {
                if (patch.Failed)
                {
                    DiscardShadow(patch.SteamId);
                }

                FinishPending(patch.SteamId);
                return;
            }

            PlayerState state = archive.GetOrCreate(patch.SteamId);
            ApplyProgress(state, patch);
            onApplied(patch, state);
            FinishPending(patch.SteamId);
        }

        /// <summary>Writes the patch's absolute amounts, removals and solved count into the archive state.</summary>
        private static void ApplyProgress(PlayerState state, ProgressPatch patch)
        {
            string scheduleKey = patch.ScheduleKey;
            // Progress below the goal never closes or reopens a type, so the open-type cache stays valid then.
            if (patch.CompletesTask || patch.Removals != null)
            {
                state.OpenTypesDirty = true;
            }

            if (patch.Removals != null)
            {
                foreach ((string challengeId, string taskId) in patch.Removals)
                {
                    if (state.Challenges.TryGetValue(scheduleKey, out var challenges)
                        && challenges.TryGetValue(challengeId, out var tasks)
                        && tasks.Remove(taskId))
                    {
                        state.Dirty = true;
                    }
                }
            }

            if (patch.Upserts != null)
            {
                foreach (ProgressUpsert upsert in patch.Upserts)
                {
                    TaskProgress progress = ChallengeProgress.GetOrCreate(
                        state, scheduleKey, upsert.ChallengeId, upsert.TaskId);
                    progress.Amount = upsert.Amount;
                    progress.LastUpdate = upsert.LastUpdate;
                    state.Dirty = true;
                }
            }

            if (patch.SolvedAbsolute is int solved)
            {
                state.Statistics.AmountChallengesSolved = solved;
                state.Dirty = true;
            }
        }

        /// <summary>Game thread only: one call per finished target of a work item.</summary>
        private void FinishPending(string steamId)
        {
            if (_pending.TryGetValue(steamId, out int count) && count > 1)
            {
                _pending[steamId] = count - 1;
                return;
            }

            _pending.Remove(steamId);
            if (_discardWhenIdle.Remove(steamId))
            {
                RemoveShadow(steamId);
            }

            if (!_saveWhenIdle.Remove(steamId, out Action? save))
            {
                return;
            }

            RunDeferredSave(steamId, save);
        }

        /// <summary>
        /// Game thread: drops the worker shadow for <paramref name="steamId"/> now when idle, otherwise once
        /// its last in-flight target finished. The next enqueue reseeds from the archive.
        /// </summary>
        public void DiscardShadow(string steamId)
        {
            if (HasPending(steamId))
            {
                _discardWhenIdle.Add(steamId);
                return;
            }

            RemoveShadow(steamId);
        }

        private void RemoveShadow(string steamId)
        {
            _seeds.Remove(steamId);
            _shadows.TryRemove(steamId, out _);
        }

        private static void RunDeferredSave(string steamId, Action save)
        {
            try
            {
                save();
            }
            catch (Exception ex)
            {
                Log($"deferred save failed for {steamId}: {ex.Message}");
            }
        }

        private void FlushDeferredSaves()
        {
            if (_saveWhenIdle.Count == 0)
            {
                return;
            }

            KeyValuePair<string, Action>[] saves = [.. _saveWhenIdle];
            _saveWhenIdle.Clear();
            foreach ((string steamId, Action save) in saves)
            {
                RunDeferredSave(steamId, save);
            }
        }

        private static void Log(string message) => Console.WriteLine($"[Challenges] {message}");
    }
}
