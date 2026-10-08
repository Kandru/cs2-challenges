using Challenges.Configs;

namespace Challenges.Utils
{
    /// <summary>
    /// Steam-id keyed player progress for the life of the plugin instance.
    /// Session controllers point at the same <see cref="PlayerState"/> objects.
    /// </summary>
    public sealed class PlayerArchive
    {
        public readonly record struct OfflineScore(string SteamId, string Name, int Current, int Total);

        private readonly Dictionary<string, PlayerState> _bySteam = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _blockedWrites = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, OfflineScore> _offlineScores = new(StringComparer.OrdinalIgnoreCase);
        private bool _loaded;
        private bool _offlineScoresBuilt;
        private string? _offlineScheduleKey;

        public Dictionary<string, OfflineScore>.ValueCollection OfflineScores => _offlineScores.Values;

        /// <summary>
        /// Loads player files once. <paramref name="warn"/> is for corrupt/blocked files;
        /// <paramref name="debug"/> is for routine load details (caller gates on debug mode).
        /// </summary>
        public void EnsureLoaded(PluginConfig config, Action<string>? warn = null, Action<string>? debug = null)
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            string dir = PlayerFiles.DirectoryPath(config);
            if (!Directory.Exists(dir))
            {
                return;
            }

            string[] paths = Directory.GetFiles(dir, "*.json");
            if (paths.Length == 0)
            {
                return;
            }

            var loaded = new (string Path, PlayerState? State, DateTime WriteTime, Exception? Error)[paths.Length];
            if (paths.Length < 4)
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    loaded[i] = ReadOne(paths[i]);
                }
            }
            else
            {
                Parallel.For(0, paths.Length, i => loaded[i] = ReadOne(paths[i]));
            }

            int bad = 0;
            var writeTimes = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            foreach ((string path, PlayerState? state, DateTime writeTime, Exception? error) in loaded)
            {
                if (error != null || state == null)
                {
                    bad++;
                    QuarantineOrBlock(path, Path.GetFileNameWithoutExtension(path), warn, debug, error);
                    continue;
                }

                string steamId = state.SteamId.Length > 0
                    ? state.SteamId
                    : Path.GetFileNameWithoutExtension(path);
                if (steamId.Length == 0)
                {
                    bad++;
                    warn?.Invoke($"player file missing steam id: {path}");
                    debug?.Invoke($"corrupt player file: {path} (missing steam id)");
                    continue;
                }

                state.SteamId = steamId;
                if (state.Username.Length == 0)
                {
                    state.Username = steamId;
                }

                if (!_bySteam.TryGetValue(steamId, out PlayerState? existing))
                {
                    _bySteam[steamId] = state;
                    writeTimes[steamId] = writeTime;
                    continue;
                }

                int existingWeight = ProgressWeight(existing);
                int incomingWeight = ProgressWeight(state);
                if (incomingWeight > existingWeight
                    || (incomingWeight == existingWeight && writeTime > writeTimes.GetValueOrDefault(steamId)))
                {
                    debug?.Invoke($"duplicate player file for {steamId}; keeping {Path.GetFileName(path)}");
                    _bySteam[steamId] = state;
                    writeTimes[steamId] = writeTime;
                }
                else
                {
                    debug?.Invoke($"duplicate player file for {steamId}; ignoring {Path.GetFileName(path)}");
                }
            }

            debug?.Invoke(
                bad > 0
                    ? $"player archive: {_bySteam.Count} loaded, {bad} failed"
                    : $"player archive: {_bySteam.Count} loaded");
        }

        public PlayerState? TryGet(string? steamId) =>
            !string.IsNullOrEmpty(steamId) && _bySteam.TryGetValue(steamId, out PlayerState? state)
                ? state
                : null;

        public PlayerState GetOrCreate(string steamId, out bool created)
        {
            if (_bySteam.TryGetValue(steamId, out PlayerState? state))
            {
                created = false;
                return state;
            }

            state = new PlayerState { SteamId = steamId, Username = steamId };
            _bySteam[steamId] = state;
            if (_offlineScoresBuilt)
            {
                _offlineScores[steamId] = new OfflineScore(steamId, steamId, 0, 0);
            }

            created = true;
            return state;
        }

        public PlayerState GetOrCreate(string steamId) => GetOrCreate(steamId, out _);

        /// <summary>
        /// Rebuilds offline rank snapshots only when the schedule key changes.
        /// </summary>
        public void EnsureOfflineScores(string? scheduleKey, RunningSchedule? schedule, Action<string>? debug = null)
        {
            if (_offlineScoresBuilt && _offlineScheduleKey == scheduleKey)
            {
                return;
            }

            _offlineScoresBuilt = true;
            _offlineScheduleKey = scheduleKey;
            _offlineScores.Clear();
            foreach (PlayerState state in _bySteam.Values)
            {
                if (state.SteamId.Length == 0)
                {
                    continue;
                }

                _offlineScores[state.SteamId] = BuildScore(state, schedule);
            }

            debug?.Invoke(
                $"offline score cache rebuilt for schedule '{scheduleKey ?? "(none)"}' ({_offlineScores.Count} rows)");
        }

        /// <summary>O(1) refresh after disconnect, or invalidate so the next All paint rebuilds.</summary>
        public void PatchOfflineScore(PlayerState state, RunningSchedule? schedule, Action<string>? debug = null)
        {
            if (state.SteamId.Length == 0)
            {
                return;
            }

            string? key = schedule?.Key;
            if (_offlineScoresBuilt && _offlineScheduleKey == key)
            {
                _offlineScores[state.SteamId] = BuildScore(state, schedule);
                debug?.Invoke($"offline score patched for {state.Username} ({state.SteamId})");
                return;
            }

            if (_offlineScoresBuilt)
            {
                debug?.Invoke($"offline score cache invalidated (schedule changed or not ready) for {state.SteamId}");
            }

            _offlineScoresBuilt = false;
        }

        public bool TryWrite(PluginConfig config, PlayerState state, Action<string>? warn = null, Action<string>? debug = null)
        {
            string steamId = state.SteamId;
            if (steamId.Length == 0)
            {
                warn?.Invoke($"skip save: empty steam id (user '{state.Username}')");
                return false;
            }

            if (_blockedWrites.Contains(steamId))
            {
                warn?.Invoke($"skip save: writes blocked for {steamId} (corrupt file left in place)");
                return false;
            }

            try
            {
                string path = PlayerFiles.FilePath(config, steamId);
                PlayerFiles.Write(path, state);
                state.Dirty = false;
                debug?.Invoke(
                    $"saved {state.Username} ({steamId}) "
                    + $"solved={state.Statistics.AmountChallengesSolved} schedules={state.Challenges.Count}");
                return true;
            }
            catch (Exception ex)
            {
                warn?.Invoke($"failed to save player {steamId}: {ex.Message}");
                state.Dirty = true;
                return false;
            }
        }

        public int FlushDirty(PluginConfig config, Action<string>? warn = null, Action<string>? debug = null)
        {
            int written = 0;
            int failed = 0;
            foreach (PlayerState state in _bySteam.Values)
            {
                if (!state.Dirty)
                {
                    continue;
                }

                if (TryWrite(config, state, warn, debug))
                {
                    written++;
                }
                else
                {
                    failed++;
                }
            }

            if (written > 0 || failed > 0)
            {
                debug?.Invoke($"flush dirty players: wrote {written}" + (failed > 0 ? $", failed {failed}" : string.Empty));
            }

            return written;
        }

        private static (string Path, PlayerState? State, DateTime WriteTime, Exception? Error) ReadOne(string path)
        {
            try
            {
                return (path, PlayerFiles.DeserializeBytes(File.ReadAllBytes(path)), File.GetLastWriteTimeUtc(path), null);
            }
            catch (Exception ex)
            {
                return (path, null, default, ex);
            }
        }

        private void QuarantineOrBlock(
            string path,
            string steamIdFromPath,
            Action<string>? warn,
            Action<string>? debug,
            Exception? error)
        {
            string detail = error?.Message ?? "null state";
            if (PlayerFiles.TryQuarantine(path, out string? quarantinePath))
            {
                warn?.Invoke($"quarantined corrupt player file {path} -> {quarantinePath}: {detail}");
                debug?.Invoke($"corrupt player file: {path} ({detail})");
                return;
            }

            if (steamIdFromPath.Length > 0)
            {
                _blockedWrites.Add(steamIdFromPath);
            }

            warn?.Invoke($"corrupt player file left in place; writes blocked for {steamIdFromPath}: {detail}");
            debug?.Invoke($"corrupt player file: {path} ({detail})");
        }

        private static int ProgressWeight(PlayerState state)
        {
            int n = state.Statistics.AmountChallengesSolved;
            foreach (var schedule in state.Challenges.Values)
            {
                foreach (var challenge in schedule.Values)
                {
                    n += challenge.Count;
                }
            }

            return n;
        }

        private static OfflineScore BuildScore(PlayerState state, RunningSchedule? schedule)
        {
            int current = schedule != null
                ? ChallengeProgress.CountSolvedInSchedule(state, schedule)
                : 0;
            string name = state.Username.Length > 0 ? state.Username : state.SteamId;
            return new OfflineScore(state.SteamId, name, current, state.Statistics.AmountChallengesSolved);
        }
    }
}
