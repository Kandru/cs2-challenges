using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Attributes;
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
        /// <summary>Same for generated listener handlers, so a listener can never clobber an event's list.</summary>
        private readonly List<(CCSPlayerController? Player, string Type)> _listenerTargets = [];
        /// <summary>Resolved identity per player slot, keyed by user id. Game thread only.</summary>
        private readonly SlotIdentity?[] _slots = new SlotIdentity?[MaxSlots];
        private readonly HashSet<PlayerState> _pruned = [];
        private readonly List<string> _pruneScratch = [];
        private readonly AttributeRegistry _attributes;
        private Dictionary<string, TypePlan> _types = new(StringComparer.Ordinal);
        private Dictionary<string, ExtractorPlan[]> _activeExtractorsByEvent = new(StringComparer.Ordinal);
        private Dictionary<string, ExtractorPlan> _activeListeners = new(StringComparer.Ordinal);
        private List<string> _boundEvents = [];
        private List<string> _boundListeners = [];
        /// <summary>The <c>global.*</c> keys some loaded rule reads; only these are written per event.</summary>
        private GlobalKeyFlags _globalKeys;
        private bool? _hasHostages;
        /// <summary>Read once on first use; the engine lives for one map.</summary>
        private string? _mapName;
        private CCSGameRulesProxy? _gameRulesProxy;
        private Notifications? _notifications;
        private bool _destroyed;

        public ChallengeEngine(State state, IStringLocalizer localizer, bool isHotReloaded)
            : base(state, localizer)
        {
            _attributes = new AttributeRegistry(EnqueueOne);
            RebuildEvents();
        }

        /// <summary>Attribute providers; the generated button listener forwards to them first.</summary>
        internal AttributeRegistry Providers => _attributes;

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
            Dictionary<string, TypePlan> byType = new(StringComparer.Ordinal);
            List<ChallengeTask> allTasks = [];
            GlobalKeyFlags globalKeys = GlobalKeyFlags.None;

            if (CurrentSchedule is { } schedule)
            {
                foreach (ChallengeDefinition challenge in schedule.Challenges)
                {
                    foreach (ChallengeTask task in challenge.Tasks)
                    {
                        GlobalGate gate = TaskGate(task);
                        if (!byType.TryGetValue(task.Type, out TypePlan? plan))
                        {
                            byType[task.Type] = plan = new TypePlan(byType.Count, task.Type) { Gate = gate };
                        }
                        else
                        {
                            plan.Gate &= gate;
                        }

                        plan.Tasks.Add((challenge, task));
                        allTasks.Add(task);
                        foreach (CompiledRule rule in task.CompiledRules)
                        {
                            globalKeys |= GlobalKey(rule.Key);
                        }
                    }
                }
            }

            GlobalGate gateUnion = GlobalGate.None;
            foreach (TypePlan plan in byType.Values)
            {
                gateUnion |= plan.Gate;
            }

            TypePlan[] typeList = new TypePlan[byType.Count];
            foreach (TypePlan plan in byType.Values)
            {
                typeList[plan.Id] = plan;
            }

            _types = byType;
            _typeList = typeList;
            _typeLayout = Interlocked.Increment(ref _nextTypeLayout);
            _scheduleKey = CurrentSchedule?.Key ?? string.Empty;
            _gateUnion = gateUnion;
            _gateReads = (globalKeys & GlobalKeyFlags.IsWarmup) != 0
                ? gateUnion | GlobalGate.WarmupBits
                : gateUnion;
            _globalKeys = globalKeys;
            _attributes.Configure(allTasks);

            HashSet<string> bound = new(StringComparer.Ordinal);
            List<string> events = [];
            List<string> listeners = [];
            Dictionary<string, List<ExtractorPlan>> active = new(StringComparer.Ordinal);
            Dictionary<string, ExtractorPlan> activeListeners = new(StringComparer.Ordinal);
            int globalKeyCount = BitOperations.PopCount((uint)globalKeys);
            foreach (IExtractor extractor in Registry.All)
            {
                TypePlan[] types = ActiveTypes(extractor, byType);
                bool used = types.Length > 0;
                if (used && extractor.Kind == ExtractorKind.Listener)
                {
                    activeListeners[extractor.EventClassName] =
                        new ExtractorPlan(extractor, types, _attributes, globalKeyCount);
                }
                else if (used)
                {
                    if (!active.TryGetValue(extractor.EventClassName, out List<ExtractorPlan>? list))
                    {
                        active[extractor.EventClassName] = list = [];
                    }

                    list.Add(new ExtractorPlan(extractor, types, _attributes, globalKeyCount));
                }

                if ((!used && !_attributes.Watches(extractor.EventClassName))
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

            Dictionary<string, ExtractorPlan[]> activeByEvent = new(active.Count, StringComparer.Ordinal);
            foreach ((string eventName, List<ExtractorPlan> list) in active)
            {
                activeByEvent[eventName] = [.. list];
            }

            _activeExtractorsByEvent = activeByEvent;
            _activeListeners = activeListeners;
            _boundEvents = events;
            _boundListeners = listeners;
        }

        /// <summary>Drops per-player engine and attribute state when the player leaves.</summary>
        public void ForgetPlayer(CCSPlayerController player, PlayerState state)
        {
            _pruned.Remove(state);
            if (SteamIds.IsCreditable(state.SteamId))
            {
                DiscardShadow(state.SteamId);
            }

            int slot = player.Slot;
            if ((uint)slot < MaxSlots)
            {
                _slots[slot] = null;
            }

            _attributes.ForgetSlot(slot);
        }

        /// <summary>Clears round-scoped caches (hostages, attribute providers).</summary>
        public void InvalidateRoundCache()
        {
            _hasHostages = null;
            _attributes.Clear();
        }

        public override void Destroy()
        {
            _destroyed = true;
            _types = new(StringComparer.Ordinal);
            _gateUnion = GlobalGate.None;
            _gateReads = GlobalGate.None;
            _globalKeys = GlobalKeyFlags.None;
            _typeList = [];
            _activeExtractorsByEvent = new(StringComparer.Ordinal);
            _activeListeners = new(StringComparer.Ordinal);
            _boundEvents = [];
            _boundListeners = [];
            _pruned.Clear();
            Array.Clear(_slots);
            _gameRulesProxy = null;
            _attributes.Clear();
        }

        /// <summary>
        /// First step of a generated listener handler: one dictionary lookup when no loaded type uses the
        /// listener, then the global gates. False means the handler returns without reading anything.
        /// </summary>
        private bool BeginListener(
            string listenerName,
            [NotNullWhen(true)] out ExtractorPlan? plan,
            out GlobalGate closed)
        {
            closed = GlobalGate.None;
            if (!CanHandle() || !_activeListeners.TryGetValue(listenerName, out plan))
            {
                plan = null;
                return false;
            }

            closed = ReadClosedGates();
            if (!plan.AllClosed(closed))
            {
                return true;
            }

            if (GlobalConfig.Debug.Enable)
            {
                DebugPrint($"{listenerName} gated");
            }

            return false;
        }

        /// <summary>Cleared target scratch list for generated listener handlers.</summary>
        private List<(CCSPlayerController? Player, string Type)> ListenerTargets()
        {
            _listenerTargets.Clear();
            return _listenerTargets;
        }

        /// <summary>Generated listener handlers: valid targets, or null when the handler can stop.</summary>
        private List<ProgressTarget>? FilterListenerTargets(
            string listenerName,
            List<(CCSPlayerController? Player, string Type)> targets,
            GlobalGate closed)
        {
            try
            {
                List<ProgressTarget>? valid = FilterTargets(targets, closed);
                if (valid == null && GlobalConfig.Debug.Enable)
                {
                    DebugPrint($"{listenerName} no targets");
                }

                return valid;
            }
            catch (Exception ex)
            {
                Log($"{listenerName} failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>Last step of a generated listener handler.</summary>
        private void HandleListener(
            string listenerName,
            Dictionary<string, string> data,
            List<ProgressTarget> targets,
            GlobalGate closed)
        {
            try
            {
                Enqueue(listenerName, data, targets, closed);
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

            bool hasTasks = _types.Count > 0;
            if (!hasTasks && !_attributes.AnyActive)
            {
                return HookResult.Continue;
            }

            AttributeWatch? watch = _attributes.WatchOf(eventName);
            if (watch != null)
            {
                try
                {
                    _attributes.Observe(watch, eventName, gameEvent);
                }
                catch (Exception ex)
                {
                    Log($"attributes {eventName} failed: {ex.Message}");
                }
            }

            if (hasTasks && _activeExtractorsByEvent.TryGetValue(eventName, out ExtractorPlan[]? extractors))
            {
                HandleExtractors(eventName, gameEvent, extractors);
            }

            if (watch != null)
            {
                try
                {
                    _attributes.AfterEvent(watch, eventName, gameEvent);
                }
                catch (Exception ex)
                {
                    Log($"attributes {eventName} failed: {ex.Message}");
                }
            }

            return HookResult.Continue;
        }

        /// <summary>Per active extractor: gates, targets, fill, provider values, enqueue.</summary>
        private void HandleExtractors(string eventName, GameEvent gameEvent, ExtractorPlan[] extractors)
        {
            GlobalGate closed = ReadClosedGates();
            foreach (ExtractorPlan plan in extractors)
            {
                if (plan.AllClosed(closed))
                {
                    if (GlobalConfig.Debug.Enable)
                    {
                        DebugPrint($"{eventName} gated");
                    }

                    continue;
                }

                IExtractor extractor = plan.Extractor;
                try
                {
                    _targetBuffer.Clear();
                    extractor.Targets(gameEvent, _targetBuffer, plan.Demand);
                    List<ProgressTarget>? targets = FilterTargets(_targetBuffer, closed);
                    if (targets == null)
                    {
                        if (GlobalConfig.Debug.Enable)
                        {
                            DebugPrint($"{eventName} no targets");
                        }

                        continue;
                    }

                    Dictionary<string, string> data = new(plan.Demand.Capacity);
                    extractor.Fill(gameEvent, data, plan.Demand);
                    plan.Attributes?.Write(eventName, gameEvent, extractor, data);
                    Enqueue(eventName, data, targets, closed);
                }
                catch (Exception ex)
                {
                    Log($"{eventName} failed: {ex.Message}");
                }
            }
        }

        private bool CanHandle() =>
            !_destroyed && !_flushed && _types.Count > 0 && GlobalConfig.Enabled;

        /// <summary>
        /// Valid, creditable targets whose type is loaded and not closed by <paramref name="closed"/>.
        /// Null when none remain. Clears <paramref name="targets"/>.
        /// </summary>
        private List<ProgressTarget>? FilterTargets(
            List<(CCSPlayerController? Player, string Type)> targets,
            GlobalGate closed)
        {
            List<ProgressTarget>? valid = null;
            for (int i = 0; i < targets.Count; i++)
            {
                (CCSPlayerController? player, string type) = targets[i];
                if (!_types.TryGetValue(type, out TypePlan? plan)
                    || plan.IsClosed(closed)
                    || player is not { IsValid: true }
                    || ResolveIdentity(player) is not { } identity)
                {
                    continue;
                }

                // Nothing left to earn for this type; in-flight work may still reset tasks, so never skip then.
                if (!IsTypeOpen(identity.State, plan) && !HasPending(identity.SteamId))
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
                    Candidates = plan.Tasks,
                });
            }

            // Scratch lists must not keep controller wrappers alive until the next event.
            targets.Clear();
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

        /// <summary><paramref name="closed"/> is the event's <see cref="ReadClosedGates"/> result.</summary>
        private void Enqueue(
            string source,
            Dictionary<string, string> data,
            List<ProgressTarget> targets,
            GlobalGate closed)
        {
            if (CurrentSchedule is not { } schedule || targets.Count == 0)
            {
                return;
            }

            MergeGlobalData(data, closed);
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
            GlobalGate closed = ReadClosedGates();
            if (FilterTargets(_targetBuffer, closed) is { } targets)
            {
                Enqueue(type, data, targets, closed);
            }
        }

        private static TypePlan[] ActiveTypes(IExtractor extractor, Dictionary<string, TypePlan> types)
        {
            List<TypePlan>? active = null;
            foreach (string type in extractor.ChallengeTypes)
            {
                if (types.TryGetValue(type, out TypePlan? plan))
                {
                    (active ??= []).Add(plan);
                }
            }

            return active == null ? [] : [.. active];
        }

        /// <summary>
        /// Writes the <c>global.*</c> keys loaded rules read. The warmup value comes from
        /// <paramref name="closed"/>, which <see cref="ReadClosedGates"/> fills whenever that key is read.
        /// </summary>
        private void MergeGlobalData(Dictionary<string, string> data, GlobalGate closed)
        {
            GlobalKeyFlags keys = _globalKeys;
            if (keys == GlobalKeyFlags.None)
            {
                return;
            }

            if ((keys & GlobalKeyFlags.IsWarmup) != 0)
            {
                data["global.iswarmup"] = EventData.Bool((closed & GlobalGate.NoWarmup) != 0);
            }

            if ((keys & GlobalKeyFlags.IsDuringRound) != 0)
            {
                data["global.isduringround"] = EventData.Bool(State.DuringRound);
            }

            if ((keys & GlobalKeyFlags.MapName) != 0)
            {
                data["global.mapname"] = _mapName ??= Server.MapName;
            }

            if ((keys & GlobalKeyFlags.HasHostages) != 0)
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
            state.ProgressVersion++;
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
