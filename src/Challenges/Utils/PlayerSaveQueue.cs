using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Challenges
{
    /// <summary>
    /// Budgeted player saves. The game thread queues steam ids, serializes up to
    /// <see cref="PluginConfigPersistence.AutosavePerTick"/> dirty states per tick, and hands the bytes to a
    /// single background writer. Per steam id only the newest handed-off bytes are written.
    /// </summary>
    internal sealed class PlayerSaveQueue
    {
        private static readonly TimeSpan WriterJoinTimeout = TimeSpan.FromSeconds(5);

        private readonly PlayerArchive _archive;
        private readonly PluginConfig _config;
        private readonly Action<string> _warn;
        private readonly Action<string> _debug;
        private readonly Queue<string> _queue = new();
        private readonly HashSet<string> _queued = new(StringComparer.OrdinalIgnoreCase);
        private readonly Channel<PendingWrite> _writes =
            Channel.CreateUnbounded<PendingWrite>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
                AllowSynchronousContinuations = false,
            });
        /// <summary>Newest handed-off version per steam id; removed once that version is on disk.</summary>
        private readonly ConcurrentDictionary<string, long> _inFlight = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentQueue<FailedWrite> _failed = new();
        private readonly CancellationTokenSource _abort = new();

        private Task? _writer;
        private long _version;
        private bool _closed;

        public PlayerSaveQueue(PlayerArchive archive, PluginConfig config, Action<string> warn, Action<string> debug)
        {
            _archive = archive;
            _config = config;
            _warn = warn;
            _debug = debug;
        }

        /// <summary>Game thread: remembers the steam id for a later budgeted save (no duplicates).</summary>
        public void Enqueue(PlayerState state)
        {
            string steamId = state.SteamId;
            if (steamId.Length == 0)
            {
                _warn($"skip save: empty steam id (user '{state.Username}')");
                return;
            }

            if (_closed || !_queued.Add(steamId))
            {
                return;
            }

            _queue.Enqueue(steamId);
        }

        /// <summary>
        /// Game thread, once per tick: re-marks failed writes as dirty, then serializes queued dirty states
        /// within the configured budget.
        /// </summary>
        public void Pump()
        {
            if ((_queue.Count == 0 && _failed.IsEmpty) || _closed)
            {
                return;
            }

            DrainFailed();
            int budget = Math.Max(1, _config.Persistence.AutosavePerTick);
            int serialized = 0;
            while (serialized < budget && _queue.TryDequeue(out string? steamId))
            {
                _queued.Remove(steamId);
                if (TryHandOff(steamId))
                {
                    serialized++;
                }
            }
        }

        /// <summary>
        /// Game thread, on teardown: stops the writer, waits for it, and writes everything that is still queued
        /// or not yet on disk synchronously. Later <see cref="Enqueue"/> calls are ignored.
        /// </summary>
        public void FlushSync()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;
            _writes.Writer.TryComplete();
            if (_writer != null)
            {
                try
                {
                    if (!_writer.Wait(WriterJoinTimeout))
                    {
                        _abort.Cancel();
                        _warn("player save writer did not finish in time");
                    }
                }
                catch (Exception ex)
                {
                    _warn($"player save writer join failed: {ex.Message}");
                }
            }

            DrainFailed();
            foreach (string steamId in _inFlight.Keys)
            {
                if (_archive.TryGet(steamId) is { } state)
                {
                    state.Dirty = true;
                    if (_queued.Add(steamId))
                    {
                        _queue.Enqueue(steamId);
                    }
                }
            }

            _inFlight.Clear();
            while (_queue.TryDequeue(out string? steamId))
            {
                if (_archive.TryGet(steamId) is { Dirty: true } state)
                {
                    _archive.TryWrite(_config, state, _warn, _debug);
                }
            }

            _queued.Clear();
        }

        private bool TryHandOff(string steamId)
        {
            if (_archive.TryGet(steamId) is not { Dirty: true } state)
            {
                return false;
            }

            if (_archive.IsWriteBlocked(state.SteamId))
            {
                _warn($"skip save: writes blocked for {state.SteamId} (corrupt file left in place)");
                return false;
            }

            byte[] bytes;
            try
            {
                bytes = PlayerFiles.Serialize(state);
            }
            catch (Exception ex)
            {
                _warn($"failed to serialize player {state.SteamId}: {ex.Message}");
                return true;
            }

            long version = ++_version;
            _inFlight[state.SteamId] = version;
            EnsureWriter();
            if (!_writes.Writer.TryWrite(new PendingWrite(
                    state.SteamId,
                    PlayerFiles.FilePath(_config, state.SteamId),
                    bytes,
                    version)))
            {
                _inFlight.TryRemove(KeyValuePair.Create(state.SteamId, version));
                return true;
            }

            state.Dirty = false;
            if (_config.Debug.Enable)
            {
                _debug(
                    $"autosave {state.Username} ({state.SteamId}) "
                    + $"solved={state.Statistics.AmountChallengesSolved} schedules={state.Challenges.Count}");
            }

            return true;
        }

        private void DrainFailed()
        {
            while (_failed.TryDequeue(out FailedWrite failed))
            {
                if (failed.SteamId.Length == 0)
                {
                    _warn(failed.Error);
                    continue;
                }

                _warn($"failed to save player {failed.SteamId}: {failed.Error}");
                // A newer hand-off for the same steam id supersedes this one.
                if (_inFlight.TryRemove(KeyValuePair.Create(failed.SteamId, failed.Version))
                    && _archive.TryGet(failed.SteamId) is { } state)
                {
                    state.Dirty = true;
                }
            }
        }

        private void EnsureWriter() =>
            _writer ??= Task.Factory.StartNew(
                WriterLoop,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default).Unwrap();

        /// <summary>Background thread: file I/O only, never touches player state or the game.</summary>
        private async Task WriterLoop()
        {
            try
            {
                await foreach (PendingWrite write in _writes.Reader.ReadAllAsync(_abort.Token).ConfigureAwait(false))
                {
                    if (_abort.IsCancellationRequested)
                    {
                        return;
                    }

                    if (!_inFlight.TryGetValue(write.SteamId, out long newest) || newest != write.Version)
                    {
                        continue;
                    }

                    try
                    {
                        PlayerFiles.WriteBytes(write.Path, write.Bytes);
                        _inFlight.TryRemove(KeyValuePair.Create(write.SteamId, write.Version));
                    }
                    catch (Exception ex)
                    {
                        _failed.Enqueue(new FailedWrite(write.SteamId, write.Version, ex.Message));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _failed.Enqueue(new FailedWrite(string.Empty, 0, $"player save writer stopped: {ex.Message}"));
            }
        }

        private readonly record struct PendingWrite(string SteamId, string Path, byte[] Bytes, long Version);

        private readonly record struct FailedWrite(string SteamId, long Version, string Error);
    }
}
