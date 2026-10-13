using System.Numerics;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Extractors;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// The attribute providers and the shared <see cref="RoundRoster"/>. Configured on rebuild; only active
    /// providers are observed, and only providers some rule reads write values for an extractor.
    /// Volatile round memory: never persisted, cleared on round start, map end and unload. Game thread only.
    /// </summary>
    internal sealed class AttributeRegistry
    {
        /// <summary>Player suffixes of every provider, active or not (catalog and load validation).</summary>
        public static readonly IReadOnlyList<AttributeSuffix> AllPlayerSuffixes =
            [.. GrenadeProvider.Suffixes, .. SmokeProvider.Suffixes];

        /// <summary>Event keys of every provider, active or not (catalog and load validation).</summary>
        public static readonly IReadOnlyList<AttributeEventKey> AllEventKeys =
            [.. AceProvider.Keys, .. SmokeProvider.Keys];

        private static readonly string[] RosterEvents =
        [
            "EventRoundStart", "EventPlayerTeam", "EventPlayerDisconnect",
        ];

        private readonly RoundRoster _roster = new();
        private readonly GrenadeProvider _grenade = new();
        private readonly IAttributeProvider[] _providers;
        private readonly ProviderContext _context;
        private IAttributeProvider[] _active = [];
        private Dictionary<string, AttributeWatch> _watches = new(StringComparer.Ordinal);
        private bool _buttons;

        public AttributeRegistry(Action<CCSPlayerController, string, Dictionary<string, EventValue>> enqueueOne)
        {
            _providers = [new AceProvider(), new TradeProvider(), _grenade, new SmokeProvider()];
            _context = new ProviderContext(enqueueOne, _roster);
        }

        public bool AnyActive => _active.Length > 0;
        /// <summary>True while an active provider needs <see cref="Roster"/> kept current.</summary>
        public bool RosterEnabled { get; private set; }
        public RoundRoster Roster => _roster;

        /// <summary>Activates the providers the loaded rules use; reads canonical keys.</summary>
        public void Configure(IReadOnlyList<ChallengeTask> tasks)
        {
            List<IAttributeProvider> active = [];
            Dictionary<string, List<IAttributeProvider>> watchers = new(StringComparer.Ordinal);
            bool roster = false;
            bool buttons = false;
            foreach (IAttributeProvider provider in _providers)
            {
                if (!provider.Configure(tasks))
                {
                    continue;
                }

                active.Add(provider);
                roster |= provider.UsesRoster;
                buttons |= ReferenceEquals(provider, _grenade);
                foreach (string eventName in provider.WatchedEvents)
                {
                    if (!watchers.TryGetValue(eventName, out List<IAttributeProvider>? list))
                    {
                        watchers[eventName] = list = [];
                    }

                    list.Add(provider);
                }
            }

            if (roster)
            {
                foreach (string eventName in RosterEvents)
                {
                    watchers.TryAdd(eventName, []);
                }
            }

            Dictionary<string, AttributeWatch> watches = new(watchers.Count, StringComparer.Ordinal);
            foreach ((string eventName, List<IAttributeProvider> list) in watchers)
            {
                watches[eventName] = new AttributeWatch([.. list], roster && Array.IndexOf(RosterEvents, eventName) >= 0);
            }

            RosterEnabled = roster;
            _roster.Invalidate();
            _active = [.. active];
            _watches = watches;
            _buttons = buttons;
        }

        /// <summary>True when an active provider (or the roster) observes <paramref name="eventClassName"/>.</summary>
        public bool Watches(string eventClassName) => _watches.ContainsKey(eventClassName);

        /// <summary>Observers of <paramref name="eventClassName"/>; null when none. One lookup.</summary>
        public AttributeWatch? WatchOf(string eventClassName) =>
            _watches.TryGetValue(eventClassName, out AttributeWatch? watch) ? watch : null;

        public void Observe(AttributeWatch watch, string eventName, GameEvent gameEvent)
        {
            foreach (IAttributeProvider provider in watch.Providers)
            {
                provider.Observe(eventName, gameEvent, _context);
            }

            if (watch.Roster)
            {
                ObserveRoster(eventName, gameEvent);
            }
        }

        public void AfterEvent(AttributeWatch watch, string eventName, GameEvent gameEvent)
        {
            foreach (IAttributeProvider provider in watch.Providers)
            {
                provider.AfterEvent(eventName, gameEvent);
            }
        }

        /// <summary>Called by the generated button listener before anything else; free while grenades are unused.</summary>
        public void OnButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (_buttons)
            {
                _grenade.OnButtonsChanged(player, pressed, released);
            }
        }

        /// <summary>
        /// What the active providers must write for <paramref name="extractor"/> given the canonical rule
        /// <paramref name="keys"/> of its active types; null when nothing.
        /// </summary>
        public AttributeDemand? BuildDemand(IExtractor extractor, IReadOnlyCollection<string> keys)
        {
            IReadOnlyList<string> prefixes = extractor.PlayerPrefixes;
            List<ProviderDemand>? demands = null;
            foreach (IAttributeProvider provider in _active)
            {
                IReadOnlyList<AttributeSuffix> suffixes = provider.PlayerSuffixes;
                IReadOnlyList<AttributeEventKey> eventKeys = provider.EventKeys;
                if (suffixes.Count == 0 && eventKeys.Count == 0)
                {
                    continue;
                }

                uint[] wanted = new uint[prefixes.Count];
                uint events = 0;
                foreach (string key in keys)
                {
                    for (int i = 0; i < eventKeys.Count; i++)
                    {
                        if (string.Equals(eventKeys[i].EventClassName, extractor.EventClassName, StringComparison.Ordinal)
                            && string.Equals(eventKeys[i].Key, key, StringComparison.Ordinal))
                        {
                            events |= 1u << i;
                        }
                    }

                    for (int i = 0; i < prefixes.Count; i++)
                    {
                        int suffix = AttributeKeys.SuffixIndex(key, prefixes[i], suffixes);
                        if (suffix >= 0)
                        {
                            wanted[i] |= 1u << suffix;
                        }
                    }
                }

                if (events == 0 && Array.TrueForAll(wanted, mask => mask == 0))
                {
                    continue;
                }

                string[][] prefixKeys = new string[prefixes.Count][];
                for (int i = 0; i < prefixes.Count; i++)
                {
                    prefixKeys[i] = wanted[i] == 0 ? [] : AttributeKeys.For(prefixes[i], suffixes);
                }

                (demands ??= []).Add(new ProviderDemand(provider, wanted, prefixKeys, events));
            }

            return demands == null ? null : new AttributeDemand(prefixes.Count, [.. demands]);
        }

        public void Clear()
        {
            foreach (IAttributeProvider provider in _providers)
            {
                provider.Clear();
            }
        }

        public void ForgetSlot(int slot)
        {
            foreach (IAttributeProvider provider in _providers)
            {
                provider.ForgetSlot(slot);
            }

            _roster.Remove(slot);
        }

        private void ObserveRoster(string eventName, GameEvent gameEvent)
        {
            switch (eventName)
            {
                case "EventRoundStart":
                    _roster.Rebuild();
                    return;
                case "EventPlayerTeam":
                    EventPlayerTeam team = (EventPlayerTeam)gameEvent;
                    _roster.OnTeam(team.Userid, team.Team, team.Disconnect);
                    return;
                case "EventPlayerDisconnect":
                    if (((EventPlayerDisconnect)gameEvent).Userid is { IsValid: true } gone)
                    {
                        _roster.Remove(gone.Slot);
                    }

                    return;
            }
        }
    }

    /// <summary>The active providers observing one event, in registry order, and whether the roster follows it.</summary>
    internal sealed class AttributeWatch(IAttributeProvider[] providers, bool roster)
    {
        public IAttributeProvider[] Providers { get; } = providers;
        public bool Roster { get; } = roster;
    }

    /// <summary>What one provider writes for one extractor. Masks index the provider's tables.</summary>
    internal sealed class ProviderDemand(IAttributeProvider provider, uint[] suffixes, string[][] keys, uint eventKeys)
    {
        public IAttributeProvider Provider { get; } = provider;
        /// <summary>Wanted <see cref="IAttributeProvider.PlayerSuffixes"/> bits per extractor player prefix.</summary>
        public uint[] Suffixes { get; } = suffixes;
        /// <summary>Full keys per prefix, indexed like <see cref="IAttributeProvider.PlayerSuffixes"/>; empty when unwanted.</summary>
        public string[][] Keys { get; } = keys;
        /// <summary>Wanted <see cref="IAttributeProvider.EventKeys"/> bits.</summary>
        public uint EventKeys { get; } = eventKeys;
    }

    /// <summary>
    /// Provider values one extractor must add after <c>Fill</c>. Built at rebuild; null on the plan when no rule
    /// of its types reads a provider key, so the event path skips it with one null check.
    /// </summary>
    internal sealed class AttributeDemand
    {
        private readonly ProviderDemand[] _demands;
        /// <summary>True for player prefixes some provider writes.</summary>
        private readonly bool[] _prefixes;

        /// <summary>
        /// Upper bound of the keys the providers add to one event (part of <see cref="Utils.FillDemand.Capacity"/>).
        /// </summary>
        public int KeyCount { get; }

        public AttributeDemand(int prefixCount, ProviderDemand[] demands)
        {
            _demands = demands;
            _prefixes = new bool[prefixCount];
            foreach (ProviderDemand demand in demands)
            {
                KeyCount += BitOperations.PopCount(demand.EventKeys);
                for (int i = 0; i < prefixCount; i++)
                {
                    _prefixes[i] |= demand.Suffixes[i] != 0;
                    KeyCount += BitOperations.PopCount(demand.Suffixes[i]);
                }
            }
        }

        /// <summary>Game event path: reads each wanted controller once, then writes event keys.</summary>
        public void Write(string eventName, GameEvent gameEvent, IExtractor extractor, Dictionary<string, EventValue> d)
        {
            for (int i = 0; i < _prefixes.Length; i++)
            {
                if (_prefixes[i])
                {
                    WritePlayer(d, i, extractor.GetPrefixPlayer(gameEvent, i));
                }
            }

            foreach (ProviderDemand demand in _demands)
            {
                if (demand.EventKeys != 0)
                {
                    demand.Provider.WriteEvent(eventName, gameEvent, d, demand.EventKeys);
                }
            }
        }

        /// <summary>Player values for prefix <paramref name="prefixIndex"/>; generated listener handlers pass the controller.</summary>
        public void WritePlayer(Dictionary<string, EventValue> d, int prefixIndex, CCSPlayerController? player)
        {
            foreach (ProviderDemand demand in _demands)
            {
                uint suffixes = demand.Suffixes[prefixIndex];
                if (suffixes != 0)
                {
                    demand.Provider.WritePlayer(d, demand.Keys[prefixIndex], suffixes, player);
                }
            }
        }
    }
}
