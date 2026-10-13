using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using CounterStrikeSharp.API;

namespace Challenges
{
    public partial class ChallengeEngine
    {
        private const int ApplyBatchSize = 32;
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
        /// Worker-side progress snapshots. The worker reads and writes; the game thread removes an entry
        /// only when its pending count reached zero, so no item for that steam id is in flight.
        /// </summary>
        private readonly ConcurrentDictionary<string, PlayerState> _shadows =
            new(StringComparer.OrdinalIgnoreCase);
        /// <summary>Saves deferred until the steam id has no pending work. Game thread only.</summary>
        private readonly Dictionary<string, Action> _saveWhenIdle =
            new(StringComparer.OrdinalIgnoreCase);

        private Task? _workerTask;
        private int _pumpScheduled;
        private bool _flushed;

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
        /// (main thread). Must run before <see cref="Destroy"/> and before player saves on unload.
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
            _pending.Clear();
        }

        /// <summary>True when this steam id still has unapplied progress work.</summary>
        public bool HasPending(string steamId) =>
            SteamIds.IsCreditable(steamId)
            && _pending.TryGetValue(steamId, out int count)
            && count > 0;

        /// <summary>
        /// Runs <paramref name="save"/> now when idle, otherwise after the last patch for this steam id.
        /// </summary>
        public void WhenIdle(string steamId, Action save)
        {
            if (!SteamIds.IsCreditable(steamId) || !HasPending(steamId))
            {
                save();
                return;
            }

            _saveWhenIdle[steamId] = save;
        }

        private bool TryEnqueueWork(ProgressWorkItem item)
        {
            if (_flushed || _destroyed)
            {
                return false;
            }

            EnsureWorker();
            foreach (ProgressTarget target in item.Targets)
            {
                // First in-flight item for this steam id carries the live snapshot.
                ref int count = ref CollectionsMarshal.GetValueRefOrAddDefault(_pending, target.SteamId, out _);
                if (++count == 1)
                {
                    target.Seed = CloneSeed(target.SteamId, item.ScheduleKey);
                }
            }

            if (_work.Writer.TryWrite(item))
            {
                return true;
            }

            foreach (ProgressTarget target in item.Targets)
            {
                FinishPending(target.SteamId);
            }

            return false;
        }

        private ProgressSeed CloneSeed(string steamId, string scheduleKey)
        {
            PlayerState state = Archive.GetOrCreate(steamId);
            PruneOutdated(state, scheduleKey);
            return new ProgressSeed
            {
                Challenges = ChallengeProgress.CloneChallenges(state.Challenges),
                Solved = state.Statistics.AmountChallengesSolved,
            };
        }

        private void PostPatch(ProgressPatch patch)
        {
            _patches.Enqueue(patch);
            SchedulePump();
        }

        private void SchedulePump()
        {
            if (_flushed || _destroyed || Interlocked.CompareExchange(ref _pumpScheduled, 1, 0) != 0)
            {
                return;
            }

            Server.NextWorldUpdate(PumpPatches);
        }

        private void PumpPatches()
        {
            Interlocked.Exchange(ref _pumpScheduled, 0);
            if (_destroyed || _flushed)
            {
                return;
            }

            DrainPatches(ApplyBatchSize);
            if (!_patches.IsEmpty)
            {
                SchedulePump();
            }
        }

        private void DrainPatches(int max)
        {
            int applied = 0;
            while (applied < max && _patches.TryDequeue(out ProgressPatch? patch))
            {
                try
                {
                    ApplyPatch(patch);
                }
                catch (Exception ex)
                {
                    Log($"progress apply failed: {ex.Message}");
                    FinishPending(patch.SteamId);
                }

                applied++;
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
            _shadows.TryRemove(steamId, out _);
            if (!_saveWhenIdle.Remove(steamId, out Action? save))
            {
                return;
            }

            RunDeferredSave(steamId, save);
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
