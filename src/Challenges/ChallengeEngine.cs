using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Extractors;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges
{
    /// <summary>
    /// Turns game events into task progress. Native extraction stays on the game thread; rule checks and
    /// progress simulation run on a single background worker; patches apply in the plugin's tick.
    /// </summary>
    public partial class ChallengeEngine : Module
    {
        private const int MaxSlots = 65;

        /// <summary>Reused target scratch list. Game thread only, not reentrant.</summary>
        private readonly List<(CCSPlayerController? Player, string Type)> _targetBuffer = [];
        /// <summary>Resolved identity per player slot, keyed by user id. Game thread only.</summary>
        private readonly SlotIdentity?[] _slots = new SlotIdentity?[MaxSlots];
        private readonly HashSet<PlayerState> _pruned = [];
        private readonly List<string> _pruneScratch = [];
        private readonly SituationMemory _situations = new();
        private Dictionary<string, List<(ChallengeDefinition Challenge, ChallengeTask Task)>> _tasksByType =
            new(StringComparer.Ordinal);
        private Dictionary<string, IExtractor[]> _activeExtractorsByEvent = new(StringComparer.Ordinal);
        private List<string> _boundEvents = [];
        private List<string> _boundListeners = [];
        private bool _usesHostageKey;
        private bool? _hasHostages;
        private CCSGameRulesProxy? _gameRulesProxy;
        private Notifications? _notifications;
        private bool _destroyed;

        public ChallengeEngine(State state, IStringLocalizer localizer, bool isHotReloaded)
            : base(state, localizer)
        {
            RebuildEvents();
        }

        /// <summary>Used by the generated button listener when grenade situation rules are loaded.</summary>
        internal SituationMemory Situations => _situations;

        public override List<string> Events => _boundEvents;
        public override List<string> Listeners => _boundListeners;

        private Notifications Notes => _notifications ??= GetModule<Notifications>();

        private RunningSchedule? CurrentSchedule => GetModule<Schedules>().Current;

        /// <summary>
        /// Refreshes the type→task index and binds only the events whose extractors feed an active type.
        /// Requires <see cref="Schedules"/> to be registered already.
        /// </summary>
        public void RebuildEvents()
        {
            Dictionary<string, List<(ChallengeDefinition Challenge, ChallengeTask Task)>> byType =
                new(StringComparer.Ordinal);
            bool hostageKey = false;
            bool isBotKey = false;

            if (CurrentSchedule is { } schedule)
            {
                foreach (ChallengeDefinition challenge in schedule.Challenges)
                {
                    foreach (ChallengeTask task in challenge.Tasks)
                    {
                        if (!byType.TryGetValue(task.Type, out List<(ChallengeDefinition, ChallengeTask)>? list))
                        {
                            byType[task.Type] = list = [];
                        }

                        list.Add((challenge, task));

                        if (hostageKey && isBotKey)
                        {
                            continue;
                        }

                        foreach (ChallengeRule rule in task.Rules)
                        {
                            if (!hostageKey && rule.Key == "global.hashostages")
                            {
                                hostageKey = true;
                            }
                            else if (!isBotKey && rule.Key.EndsWith(".isbot", StringComparison.Ordinal))
                            {
                                isBotKey = true;
                            }

                            if (hostageKey && isBotKey)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            _tasksByType = byType;
            _usesHostageKey = hostageKey;
            EventData.IncludeIsBot = isBotKey;
            _situations.Configure(byType.Values.SelectMany(static list => list.Select(static t => t.Task)));

            HashSet<string> bound = new(StringComparer.Ordinal);
            List<string> events = [];
            List<string> listeners = [];
            Dictionary<string, List<IExtractor>> active = new(StringComparer.Ordinal);
            foreach (IExtractor extractor in Registry.All)
            {
                bool used = UsesAnyType(extractor);
                if (used && extractor.Kind == ExtractorKind.GameEvent)
                {
                    if (!active.TryGetValue(extractor.EventClassName, out List<IExtractor>? list))
                    {
                        active[extractor.EventClassName] = list = [];
                    }

                    list.Add(extractor);
                }

                if ((!used && !_situations.Watches(extractor.EventClassName))
                    || !bound.Add(extractor.EventClassName))
                {
                    continue;
                }

                if (extractor.Kind == ExtractorKind.Listener)
                {
                    listeners.Add(extractor.EventClassName);
                }
                else
                {
                    events.Add(extractor.EventClassName);
                }
            }

            Dictionary<string, IExtractor[]> activeByEvent = new(active.Count, StringComparer.Ordinal);
            foreach ((string eventName, List<IExtractor> list) in active)
            {
                activeByEvent[eventName] = [.. list];
            }

            _activeExtractorsByEvent = activeByEvent;
            _boundEvents = events;
            _boundListeners = listeners;
        }

        /// <summary>Drops per-player engine and situation state when the player leaves.</summary>
        public void ForgetPlayer(CCSPlayerController player, PlayerState state)
        {
            _pruned.Remove(state);
            int slot = player.Slot;
            if ((uint)slot < MaxSlots)
            {
                _slots[slot] = null;
            }

            _situations.ForgetPlayer(player);
        }

        /// <summary>Clears round-scoped caches (hostages, situation memory).</summary>
        public void InvalidateRoundCache()
        {
            _hasHostages = null;
            _situations.Clear();
        }

        public override void Destroy()
        {
            _destroyed = true;
            _tasksByType = new(StringComparer.Ordinal);
            _activeExtractorsByEvent = new(StringComparer.Ordinal);
            _boundEvents = [];
            _boundListeners = [];
            _pruned.Clear();
            Array.Clear(_slots);
            _gameRulesProxy = null;
            _situations.Clear();
            EventData.IncludeIsBot = false;
        }

        /// <summary>Listener entry point used by generated <c>ChallengeEngine.Listeners.cs</c> handlers.</summary>
        private void HandleListener(
            string listenerName,
            Dictionary<string, string> data,
            List<(CCSPlayerController? Player, string Type)> targets)
        {
            if (!CanHandle())
            {
                return;
            }

            try
            {
                List<ProgressTarget>? valid = FilterTargets(targets);
                if (valid == null)
                {
                    if (GlobalConfig.Debug.Enable)
                    {
                        DebugPrint($"{listenerName} no targets");
                    }

                    return;
                }

                Enqueue(listenerName, data, valid);
            }
            catch (Exception ex)
            {
                Log($"{listenerName} failed: {ex.Message}");
            }
        }

        private HookResult HandleEvent(string eventName, GameEvent gameEvent)
        {
            if (_destroyed || !GlobalConfig.Enabled)
            {
                return HookResult.Continue;
            }

            bool hasTasks = _tasksByType.Count > 0;
            if (!hasTasks && !_situations.AnyEnabled)
            {
                return HookResult.Continue;
            }

            if (_situations.Watches(eventName))
            {
                try
                {
                    _situations.Observe(eventName, gameEvent, EnqueueOne);
                }
                catch (Exception ex)
                {
                    Log($"situation {eventName} failed: {ex.Message}");
                }
            }

            if (!hasTasks
                || !_activeExtractorsByEvent.TryGetValue(eventName, out IExtractor[]? extractors))
            {
                return HookResult.Continue;
            }

            bool enrich = _situations.AnyEnabled;
            foreach (IExtractor extractor in extractors)
            {
                try
                {
                    _targetBuffer.Clear();
                    extractor.Targets(gameEvent, _targetBuffer);
                    List<ProgressTarget>? targets = FilterTargets(_targetBuffer);
                    if (targets == null)
                    {
                        if (GlobalConfig.Debug.Enable)
                        {
                            DebugPrint($"{eventName} no targets");
                        }

                        continue;
                    }

                    Dictionary<string, string> data = [];
                    extractor.Fill(gameEvent, data);
                    if (enrich)
                    {
                        _situations.Enrich(eventName, gameEvent, data);
                    }

                    Enqueue(eventName, data, targets);
                }
                catch (Exception ex)
                {
                    Log($"{eventName} failed: {ex.Message}");
                }
            }

            return HookResult.Continue;
        }

        private bool CanHandle() =>
            !_destroyed && !_flushed && _tasksByType.Count > 0 && GlobalConfig.Enabled;

        private List<ProgressTarget>? FilterTargets(List<(CCSPlayerController? Player, string Type)> targets)
        {
            List<ProgressTarget>? valid = null;
            for (int i = 0; i < targets.Count; i++)
            {
                (CCSPlayerController? player, string type) = targets[i];
                if (!_tasksByType.TryGetValue(type, out List<(ChallengeDefinition Challenge, ChallengeTask Task)>? candidates)
                    || player is not { IsValid: true }
                    || ResolveIdentity(player) is not { } identity)
                {
                    continue;
                }

                (valid ??= []).Add(new ProgressTarget
                {
                    SteamId = identity.SteamId,
                    Slot = identity.Slot,
                    UserId = identity.UserId,
                    Name = GlobalConfig.Debug.Enable ? player.PlayerName ?? string.Empty : string.Empty,
                    Index = (uint)(identity.Slot + 1),
                    Candidates = candidates,
                });
            }

            return valid;
        }

        /// <summary>
        /// Steam id and bound state for a valid controller. A cached slot entry is reused while the
        /// user id matches, so the steam id string and the session dictionary are not read again.
        /// </summary>
        private SlotIdentity? ResolveIdentity(CCSPlayerController player)
        {
            int slot = player.Slot;
            int? userId = player.UserId;
            bool cacheable = userId != null && (uint)slot < MaxSlots;
            if (cacheable && _slots[slot] is { } cached && cached.UserId == userId)
            {
                return cached;
            }

            string? steamId = player.NetworkIDString;
            if (!SteamIds.IsCreditable(steamId))
            {
                return null;
            }

            if (!PlayerStates.TryGetValue(player, out PlayerState? state)
                || state.SteamId.Length == 0
                || !state.SteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase))
            {
                state = BindPlayer(player, steamId);
            }

            SlotIdentity identity = new(slot, userId, steamId, state);
            if (cacheable)
            {
                _slots[slot] = identity;
            }

            return identity;
        }

        private void Enqueue(string source, Dictionary<string, string> data, List<ProgressTarget> targets)
        {
            if (CurrentSchedule is not { } schedule || targets.Count == 0)
            {
                return;
            }

            MergeGlobalData(data);
            _ = TryEnqueueWork(new ProgressWorkItem
            {
                Source = source,
                Data = data,
                ScheduleKey = schedule.Key,
                Schedule = schedule,
                Debug = GlobalConfig.Debug.Enable,
                AllowBots = GlobalConfig.Debug.AllowBots,
                Now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Targets = targets,
            });
        }

        /// <summary>Enqueue progress for a single player (e.g. synthetic <c>player_trade</c>).</summary>
        private void EnqueueOne(CCSPlayerController player, string type, Dictionary<string, string> data)
        {
            _targetBuffer.Clear();
            _targetBuffer.Add((player, type));
            if (FilterTargets(_targetBuffer) is { } targets)
            {
                Enqueue(type, data, targets);
            }
        }

        private bool UsesAnyType(IExtractor extractor)
        {
            foreach (string type in extractor.ChallengeTypes)
            {
                if (_tasksByType.ContainsKey(type))
                {
                    return true;
                }
            }

            return false;
        }

        private void MergeGlobalData(Dictionary<string, string> data)
        {
            data["global.iswarmup"] = EventData.Bool(GetGameRules()?.WarmupPeriod ?? false);
            data["global.isduringround"] = EventData.Bool(State.DuringRound);
            data["global.mapname"] = Server.MapName;
            if (!_usesHostageKey)
            {
                return;
            }

            _hasHostages ??= Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hostage_entity").Any();
            data["global.hashostages"] = EventData.Bool(_hasHostages.Value);
        }

        private CCSGameRules? GetGameRules()
        {
            if (_gameRulesProxy is not { IsValid: true })
            {
                _gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
                    .FirstOrDefault();
            }

            return _gameRulesProxy?.GameRules;
        }

        private void PruneOutdated(PlayerState state, string scheduleKey)
        {
            if (scheduleKey.Length == 0 || !_pruned.Add(state))
            {
                return;
            }

            _pruneScratch.Clear();
            foreach (string key in state.Challenges.Keys)
            {
                if (key != scheduleKey)
                {
                    _pruneScratch.Add(key);
                }
            }

            if (_pruneScratch.Count == 0)
            {
                return;
            }

            foreach (string key in _pruneScratch)
            {
                if (GlobalConfig.Debug.Enable)
                {
                    DebugPrint($"deleting outdated progress for schedule {key}");
                }

                state.Challenges.Remove(key);
            }

            state.Dirty = true;
        }

        private sealed class SlotIdentity(int slot, int? userId, string steamId, PlayerState state)
        {
            public int Slot { get; } = slot;
            public int? UserId { get; } = userId;
            public string SteamId { get; } = steamId;
            public PlayerState State { get; } = state;
        }
    }
}
