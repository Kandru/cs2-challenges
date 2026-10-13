using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// A player-scoped key suffix a provider writes as <c>{prefix}.{suffix}</c> for any player prefix of any event,
    /// with its catalog value type. <c>tools/generate_events.py</c> reads the provider tables for catalog and docs.
    /// </summary>
    internal readonly record struct AttributeSuffix(string Suffix, string Type);

    /// <summary>An event-scoped key a provider writes on one game event, with its catalog value type.</summary>
    internal readonly record struct AttributeEventKey(string EventClassName, string Key, string Type);

    /// <summary>
    /// Round-scoped rule values no single event field carries (grenades in hand, smokes, aces, trades).
    /// A provider is active only while a loaded rule reads one of its keys or types; inactive providers are
    /// neither observed nor asked to write. Game thread only.
    /// </summary>
    internal interface IAttributeProvider
    {
        string Name { get; }
        /// <summary>Player-scoped suffixes; bit i of a suffix mask is entry i.</summary>
        IReadOnlyList<AttributeSuffix> PlayerSuffixes { get; }
        /// <summary>Event-scoped keys; bit i of an event key mask is entry i.</summary>
        IReadOnlyList<AttributeEventKey> EventKeys { get; }
        /// <summary>True when the provider reads the shared <see cref="RoundRoster"/> while active.</summary>
        bool UsesRoster { get; }
        /// <summary>Game events and listeners to observe while active; valid after <see cref="Configure"/>.</summary>
        IReadOnlyCollection<string> WatchedEvents { get; }

        /// <summary>
        /// Called on rebuild with every loaded task; reads canonical keys (<see cref="CompiledRule.Key"/>).
        /// Returns true when the provider is active.
        /// </summary>
        bool Configure(IReadOnlyList<ChallengeTask> tasks);

        /// <summary>Updates state from a watched event, before any value of that event is written.</summary>
        void Observe(string eventName, GameEvent gameEvent, ProviderContext context);

        /// <summary>Runs after every value of a watched event was written.</summary>
        void AfterEvent(string eventName, GameEvent gameEvent);

        /// <summary>
        /// Writes the suffixes in <paramref name="suffixes"/> for one controller. <paramref name="keys"/> holds the
        /// full key per <see cref="PlayerSuffixes"/> entry for the controller's canonical prefix.
        /// </summary>
        void WritePlayer(Dictionary<string, string> d, string[] keys, uint suffixes, CCSPlayerController? player);

        /// <summary>Writes the <see cref="EventKeys"/> entries in <paramref name="keys"/>; all belong to <paramref name="eventName"/>.</summary>
        void WriteEvent(string eventName, GameEvent gameEvent, Dictionary<string, string> d, uint keys);

        /// <summary>Round start, map end and unload.</summary>
        void Clear();

        void ForgetSlot(int slot);
    }

    /// <summary>Engine services a provider may use while observing.</summary>
    internal sealed class ProviderContext(
        Action<CCSPlayerController, string, Dictionary<string, string>> enqueueOne,
        RoundRoster roster)
    {
        /// <summary>Per-slot team and human state; current while a provider with <see cref="IAttributeProvider.UsesRoster"/> is active.</summary>
        public RoundRoster Roster { get; } = roster;

        /// <summary>Enqueues progress of a synthetic type for one player.</summary>
        public void EnqueueOne(CCSPlayerController player, string type, Dictionary<string, string> data) =>
            enqueueOne(player, type, data);
    }

    /// <summary>Key matching shared by providers, demand and load validation.</summary>
    internal static class AttributeKeys
    {
        /// <summary>Index of the suffix <paramref name="key"/> ends with after some prefix and a dot, or -1.</summary>
        public static int SuffixIndex(string key, IReadOnlyList<AttributeSuffix> suffixes)
        {
            for (int i = 0; i < suffixes.Count; i++)
            {
                string suffix = suffixes[i].Suffix;
                int dot = key.Length - suffix.Length - 1;
                if (dot > 0 && key[dot] == '.' && key.EndsWith(suffix, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>Index of the suffix when <paramref name="key"/> is <c>{prefix}.{suffix}</c>, or -1.</summary>
        public static int SuffixIndex(string key, string prefix, IReadOnlyList<AttributeSuffix> suffixes)
        {
            int length = prefix.Length;
            if (key.Length <= length + 1
                || key[length] != '.'
                || !key.StartsWith(prefix, StringComparison.Ordinal))
            {
                return -1;
            }

            ReadOnlySpan<char> rest = key.AsSpan(length + 1);
            for (int i = 0; i < suffixes.Count; i++)
            {
                if (rest.SequenceEqual(suffixes[i].Suffix))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>True when <paramref name="key"/> is one of <paramref name="keys"/> on any event.</summary>
        public static bool IsEventKey(string key, IReadOnlyList<AttributeEventKey> keys)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (string.Equals(keys[i].Key, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary><c>{prefix}.{suffix}</c> per entry of <paramref name="suffixes"/>; built at rebuild only.</summary>
        public static string[] For(string prefix, IReadOnlyList<AttributeSuffix> suffixes)
        {
            string[] keys = new string[suffixes.Count];
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = $"{prefix}.{suffixes[i].Suffix}";
            }

            return keys;
        }
    }
}
