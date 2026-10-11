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
    /// progress simulation run on a single background worker; patches apply on the next world update.
    /// </summary>
    public partial class ChallengeEngine : Module
    {
        private readonly Dictionary<string, List<IExtractor>> _extractorsByEvent;
        private readonly HashSet<PlayerState> _pruned = [];
        private readonly List<string> _pruneScratch = [];
        private readonly SituationMemory _situations = new();
        private Dictionary<string, List<(ChallengeDefinition Challenge, ChallengeTask Task)>> _tasksByType =
            new(StringComparer.Ordinal);
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
            _extractorsByEvent = Registry.All
                .GroupBy(e => e.EventClassName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
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
            foreach (IExtractor extractor in Registry.All)
            {
                if ((!UsesAnyType(extractor) && !_situations.Watches(extractor.EventClassName))
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

            _boundEvents = events;
            _boundListeners = listeners;
        }

        /// <summary>Drops per-player engine and situation state when the player leaves.</summary>
        public void ForgetPlayer(CCSPlayerController player, PlayerState state)
        {
            _pruned.Remove(state);
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
            _boundEvents = [];
            _boundListeners = [];
            _pruned.Clear();
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
                || !_extractorsByEvent.TryGetValue(eventName, out List<IExtractor>? extractors))
            {
                return HookResult.Continue;
            }

            bool enrich = _situations.AnyEnabled;
            foreach (IExtractor extractor in extractors)
            {
                if (!UsesAnyType(extractor))
                {
                    continue;
                }

                try
                {
                    List<ProgressTarget>? targets = FilterTargets(extractor.Targets(gameEvent));
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

        private List<ProgressTarget>? FilterTargets(
            IEnumerable<(CCSPlayerController? Player, string Type)> targets)
        {
            List<ProgressTarget>? valid = null;
            foreach ((CCSPlayerController? player, string type) in targets)
            {
                if (player is not { IsValid: true }
                    || !_tasksByType.TryGetValue(type, out List<(ChallengeDefinition Challenge, ChallengeTask Task)>? candidates))
                {
                    continue;
                }

                string? steamId = player.NetworkIDString;
                if (!SteamIds.IsCreditable(steamId))
                {
                    continue;
                }

                if (!PlayerStates.TryGetValue(player, out PlayerState? state)
                    || state.SteamId.Length == 0
                    || !state.SteamId.Equals(steamId, StringComparison.OrdinalIgnoreCase))
                {
                    BindPlayer(player, steamId);
                }

                (valid ??= []).Add(new ProgressTarget
                {
                    SteamId = steamId,
                    Slot = player.Slot,
                    UserId = player.UserId,
                    Name = player.PlayerName ?? string.Empty,
                    Index = player.Index,
                    Candidates = candidates,
                });
            }

            return valid;
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
            if (FilterTargets([(player, type)]) is { } targets)
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
    }
}
