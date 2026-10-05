using System.Globalization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Extractors;
using Challenges.Huds;
using Challenges.Utils;
using ChallengesShared.Events;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    /// <summary>
    /// Turns game events into task progress. Extraction happens inside the event hook, rules/progress/actions
    /// run on the next frame so the game thread never mutates state while the event is still being dispatched.
    /// </summary>
    public partial class ChallengeEngine : Blueprint
    {
        private readonly Dictionary<string, List<IExtractor>> _extractorsByEvent;
        private readonly HashSet<PlayerState> _pruned = [];
        private readonly List<string> _pruneScratch = [];
        private Dictionary<string, List<(ChallengeDefinition Challenge, ChallengeTask Task)>> _tasksByType =
            new(StringComparer.Ordinal);
        private List<string> _boundEvents = [];
        private List<string> _boundListeners = [];
        private bool _usesHostageKey;
        private bool? _hasHostages;
        private CCSGameRulesProxy? _gameRulesProxy;
        private Notifications? _notifications;
        private bool _destroyed;

        public ChallengeEngine(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            _extractorsByEvent = Registry.All
                .GroupBy(e => e.EventClassName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
            RebuildEvents();
        }

        public override List<string> Events => _boundEvents;
        public override List<string> Listeners => _boundListeners;

        private Notifications Notes => _notifications ??= GetClass<Notifications>();

        private RunningSchedule? CurrentSchedule => GetClass<Schedules>().Current;

        /// <summary>
        /// Refreshes the type→task index and binds only the events whose extractors feed an active type.
        /// Requires <see cref="Schedules"/> to be registered already.
        /// </summary>
        public void RebuildEvents()
        {
            Dictionary<string, List<(ChallengeDefinition Challenge, ChallengeTask Task)>> byType =
                new(StringComparer.Ordinal);
            bool hostageKey = false;

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

                        foreach (ChallengeRule rule in task.Rules)
                        {
                            if (rule.Key == "global.hashostages")
                            {
                                hostageKey = true;
                            }
                        }
                    }
                }
            }

            _tasksByType = byType;
            _usesHostageKey = hostageKey;

            HashSet<string> bound = new(StringComparer.Ordinal);
            List<string> events = [];
            List<string> listeners = [];
            foreach (IExtractor extractor in Registry.All)
            {
                if (!UsesAnyType(extractor) || !bound.Add(extractor.EventClassName))
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

        /// <summary>Drops per-player engine state when the player leaves.</summary>
        public void ForgetPlayer(PlayerState state) => _pruned.Remove(state);

        public void InvalidateRoundCache() => _hasHostages = null;

        public override void Destroy()
        {
            _destroyed = true;
            _tasksByType = new(StringComparer.Ordinal);
            _boundEvents = [];
            _boundListeners = [];
            _pruned.Clear();
            _gameRulesProxy = null;
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
                List<(CCSPlayerController Player, string Type)>? valid = FilterTargets(targets);
                if (valid != null)
                {
                    Enqueue(data, valid);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] {listenerName} failed: {ex.Message}");
            }
        }

        private HookResult HandleEvent(string eventName, GameEvent gameEvent)
        {
            if (!CanHandle()
                || !_extractorsByEvent.TryGetValue(eventName, out List<IExtractor>? extractors))
            {
                return HookResult.Continue;
            }

            foreach (IExtractor extractor in extractors)
            {
                if (!UsesAnyType(extractor))
                {
                    continue;
                }

                try
                {
                    List<(CCSPlayerController Player, string Type)>? targets = FilterTargets(extractor.Targets(gameEvent));
                    if (targets == null)
                    {
                        continue;
                    }

                    Dictionary<string, string> data = [];
                    extractor.Fill(gameEvent, data);
                    Enqueue(data, targets);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Challenges] {eventName} failed: {ex.Message}");
                }
            }

            return HookResult.Continue;
        }

        private bool CanHandle() =>
            !_destroyed && _tasksByType.Count > 0 && GlobalConfig.Enabled;

        private List<(CCSPlayerController Player, string Type)>? FilterTargets(
            IEnumerable<(CCSPlayerController? Player, string Type)> targets)
        {
            List<(CCSPlayerController Player, string Type)>? valid = null;
            foreach ((CCSPlayerController? player, string type) in targets)
            {
                if (player is { IsValid: true }
                    && (GlobalConfig.AllowBots || !player.IsBot)
                    && _tasksByType.ContainsKey(type))
                {
                    (valid ??= []).Add((player, type));
                }
            }

            return valid;
        }

        private void Enqueue(
            Dictionary<string, string> data,
            List<(CCSPlayerController Player, string Type)> targets)
        {
            MergeGlobalData(data);
            Server.NextFrame(() => ProcessTargets(targets, data));
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
            data["global.isduringround"] = EventData.Bool((bool)_globalStates[GlobalStates.DuringRound]);
            data["global.mapname"] = Server.MapName;
            if (_usesHostageKey)
            {
                _hasHostages ??= Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("hostage_entity").Any();
                data["global.hashostages"] = EventData.Bool(_hasHostages.Value);
            }
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

        private void ProcessTargets(List<(CCSPlayerController Player, string Type)> targets, Dictionary<string, string> data)
        {
            if (_destroyed)
            {
                return;
            }

            foreach ((CCSPlayerController player, string type) in targets)
            {
                try
                {
                    ProcessPlayer(player, type, data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Challenges] progress failed: {ex.Message}");
                }
            }
        }

        private void ProcessPlayer(CCSPlayerController player, string type, Dictionary<string, string> data)
        {
            if (!player.IsValid
                || CurrentSchedule is not { } schedule
                || !PlayerStates.TryGetValue(player, out PlayerState? state)
                || !_tasksByType.TryGetValue(type, out List<(ChallengeDefinition Challenge, ChallengeTask Task)>? candidates))
            {
                return;
            }

            PruneOutdated(state, schedule.Key);
            long now = UnixNow();

            // Snapshot first: finishing a task on this event must not unlock and credit the next one.
            List<(ChallengeDefinition Challenge, ChallengeTask Task)> eligible = [];
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in candidates)
            {
                if (IsEligible(state, schedule.Key, challenge, task, data, now))
                {
                    eligible.Add((challenge, task));
                }
            }

            if (eligible.Count == 0)
            {
                return;
            }

            HashSet<string> touched = new(StringComparer.Ordinal);
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in eligible)
            {
                if (!player.IsValid)
                {
                    return;
                }

                if (ChallengeProgress.IsTaskComplete(state, schedule.Key, challenge.Id, task))
                {
                    continue;
                }

                ApplyProgress(player, state, schedule, challenge, task, now);
                if (task.Visible)
                {
                    touched.Add(challenge.Id);
                }
            }

            if (touched.Count == 0)
            {
                return;
            }

            if (GlobalConfig.Gui.ShowOnProgress)
            {
                Tracker.ShowProgress(player, touched);
            }

            if (HudMenu.IsOpen(player))
            {
                Menu.Paint(player);
            }
        }

        private void PruneOutdated(PlayerState state, string scheduleKey)
        {
            if (!_pruned.Add(state))
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

            foreach (string key in _pruneScratch)
            {
                DebugPrint($"deleting outdated progress for schedule {key}");
                state.Challenges.Remove(key);
            }

        }

        private bool IsEligible(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge,
            ChallengeTask task,
            Dictionary<string, string> data,
            long now)
        {
            if (ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task)
                || !CanComplete(state, scheduleKey, challenge, task))
            {
                return false;
            }

            TaskProgress? progress = ChallengeProgress.GetProgress(state, scheduleKey, challenge.Id, task.Id);
            if (task.Cooldown > 0 && progress != null && progress.LastUpdate + task.Cooldown > now)
            {
                return false;
            }

            return CompliesWithRules(task, data);
        }

        private static bool CanComplete(PlayerState state, string scheduleKey, ChallengeDefinition challenge, ChallengeTask task)
        {
            foreach (string requiredId in task.Requires)
            {
                if (!challenge.TaskById.TryGetValue(requiredId, out ChallengeTask? required) || !ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, required))
                {
                    return false;
                }
            }

            return true;
        }

        private bool CompliesWithRules(ChallengeTask task, Dictionary<string, string> data)
        {
            foreach (ChallengeRule rule in task.Rules)
            {
                if (!data.TryGetValue(rule.Key, out string? current))
                {
                    DebugPrint($"rule {rule.Key} not found in data for type {task.Type}");
                    return false;
                }

                if (!EvaluateRule(rule, current))
                {
                    return false;
                }
            }

            return true;
        }

        private bool EvaluateRule(ChallengeRule rule, string current)
        {
            string target = rule.Value;
            switch (rule.Operator)
            {
                case "==":
                    return string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                case "!=":
                    return !string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                case ">":
                    return TryCompare(current, target, out int gt) && gt > 0;
                case "<":
                    return TryCompare(current, target, out int lt) && lt < 0;
                case ">=":
                    return TryCompare(current, target, out int ge) && ge >= 0;
                case "<=":
                    return TryCompare(current, target, out int le) && le <= 0;
                case "bool==":
                    return bool.TryParse(current, out bool a) && bool.TryParse(target, out bool b) && a == b;
                case "bool!=":
                    return bool.TryParse(current, out bool c) && bool.TryParse(target, out bool d) && c != d;
                case "contains":
                    return current.Contains(target, StringComparison.OrdinalIgnoreCase);
                case "!contains":
                    return !current.Contains(target, StringComparison.OrdinalIgnoreCase);
                default:
                    DebugPrint($"unknown operator {rule.Operator}");
                    return false;
            }
        }

        private static bool TryCompare(string current, string target, out int result)
        {
            result = 0;
            if (!float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out float left)
                || !float.TryParse(target, NumberStyles.Float, CultureInfo.InvariantCulture, out float right))
            {
                return false;
            }

            result = left.CompareTo(right);
            return true;
        }

        private void ApplyProgress(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask task,
            long now)
        {
            bool wasSolved = ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge);
            TaskProgress progress = GetOrCreateProgress(state, schedule.Key, challenge.Id, task.Id);
            progress.Amount++;
            progress.LastUpdate = now;

            if (progress.Amount < Math.Max(1, task.Amount))
            {
                Notes.NotifyProgress(player, challenge, task, progress.Amount);
                TriggerProgress(player, challenge, task, progress.Amount);
                return;
            }

            DebugPrint($"{player.PlayerName} completed {challenge.Id}/{task.Id}");
            bool solvedNow = task.Visible
                && !wasSolved
                && ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge);
            if (solvedNow)
            {
                state.Statistics.AmountChallengesSolved++;
            }

            Notes.NotifyCompletion(player, challenge, task);
            Notes.DiscordChallengeCompleted(player, challenge, task);
            RunActions(player, state, schedule, challenge, task, now);
            TriggerCompleted(player, challenge, task);

            if (solvedNow)
            {
                TriggerChallengeSolved(player, challenge);
            }
        }

        private static TaskProgress GetOrCreateProgress(PlayerState state, string scheduleKey, string challengeId, string taskId)
        {
            if (!state.Challenges.TryGetValue(scheduleKey, out var challenges))
            {
                state.Challenges[scheduleKey] = challenges = new Dictionary<string, Dictionary<string, TaskProgress>>();
            }

            if (!challenges.TryGetValue(challengeId, out var tasks))
            {
                challenges[challengeId] = tasks = new Dictionary<string, TaskProgress>();
            }

            if (!tasks.TryGetValue(taskId, out TaskProgress? progress))
            {
                tasks[taskId] = progress = new TaskProgress();
            }

            return progress;
        }

        private static long UnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
