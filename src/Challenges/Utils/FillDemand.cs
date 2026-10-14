using System.Numerics;

namespace Challenges.Utils
{
    /// <summary>One bit per player property suffix written by <c>EventData.FillPlayer</c>.</summary>
    [Flags]
    public enum PlayerFields : ushort
    {
        None = 0,
        Name = 1 << 0,
        IsBot = 1 << 1,
        Team = 1 << 2,
        Alive = 1 << 3,
        Ping = 1 << 4,
        Money = 1 << 5,
        Score = 1 << 6,
        StatsKills = 1 << 7,
        StatsAssists = 1 << 8,
        StatsDeaths = 1 << 9,
        StatsDamage = 1 << 10,
        Health = 1 << 11,
        Armor = 1 << 12,
        HasDefusor = 1 << 13,
        HasHelmet = 1 << 14,
        Stats = StatsKills | StatsAssists | StatsDeaths | StatsDamage,
        All = (1 << 15) - 1,
    }

    /// <summary>A player property suffix, its bit, and its catalog value type.</summary>
    public readonly record struct PlayerSuffix(string Suffix, PlayerFields Field, string Type);

    /// <summary>
    /// The player property suffix table and cached <c>{prefix}.{suffix}</c> key strings.
    /// Single source for the runtime and for <c>tools/generate_events.py</c> (catalog and docs).
    /// </summary>
    public static class PlayerKeys
    {
        /// <summary>Bit index i of <see cref="PlayerFields"/> is entry i.</summary>
        public static readonly PlayerSuffix[] Suffixes =
        [
            new("name", PlayerFields.Name, "string"),
            new("isbot", PlayerFields.IsBot, "bool"),
            new("team", PlayerFields.Team, "string"),
            new("alive", PlayerFields.Alive, "bool"),
            new("ping", PlayerFields.Ping, "int"),
            new("money", PlayerFields.Money, "int"),
            new("score", PlayerFields.Score, "int"),
            new("stats.kills", PlayerFields.StatsKills, "int"),
            new("stats.assists", PlayerFields.StatsAssists, "int"),
            new("stats.deaths", PlayerFields.StatsDeaths, "int"),
            new("stats.damage", PlayerFields.StatsDamage, "int"),
            new("health", PlayerFields.Health, "int"),
            new("armor", PlayerFields.Armor, "int"),
            new("hasdefusor", PlayerFields.HasDefusor, "bool"),
            new("hashelmet", PlayerFields.HasHelmet, "bool"),
        ];

        private static readonly Dictionary<string, string[]> ByPrefix = new(StringComparer.Ordinal);
        private static readonly Lock ByPrefixLock = new();

        public static int Count => Suffixes.Length;

        /// <summary>Key strings for <paramref name="prefix"/>, indexed like <see cref="Suffixes"/>. Built once per prefix.</summary>
        public static string[] For(string prefix)
        {
            lock (ByPrefixLock)
            {
                if (ByPrefix.TryGetValue(prefix, out string[]? keys))
                {
                    return keys;
                }

                keys = new string[Suffixes.Length];
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i] = $"{prefix}.{Suffixes[i].Suffix}";
                }

                ByPrefix[prefix] = keys;
                return keys;
            }
        }

        /// <summary>Bit for <c>{prefix}.{suffix}</c> when <paramref name="key"/> has that form.</summary>
        public static bool TryMatch(string key, string prefix, out PlayerFields field)
        {
            field = PlayerFields.None;
            int length = prefix.Length;
            if (key.Length <= length + 1
                || key[length] != '.'
                || !key.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            ReadOnlySpan<char> suffix = key.AsSpan(length + 1);
            foreach (PlayerSuffix entry in Suffixes)
            {
                if (suffix.SequenceEqual(entry.Suffix))
                {
                    field = entry.Field;
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// What one extractor must produce for the loaded rules. Built at load; read per event on the game
    /// thread. Arrays are indexed like the extractor's <c>ChallengeTypes</c>, <c>Keys</c> and
    /// <c>PlayerPrefixes</c>.
    /// </summary>
    public sealed class FillDemand
    {
        public FillDemand(int types, int keys, IReadOnlyList<string> prefixes)
        {
            Types = new bool[types];
            Keys = new bool[keys];
            Prefixes = new PlayerFields[prefixes.Count];
            PrefixKeys = new string[prefixes.Count][];
            for (int i = 0; i < prefixes.Count; i++)
            {
                PrefixKeys[i] = PlayerKeys.For(prefixes[i]);
            }
        }

        /// <summary>Challenge types with loaded tasks; targets are only listed for these.</summary>
        public bool[] Types { get; }
        /// <summary>Event keys some loaded rule reads.</summary>
        public bool[] Keys { get; }
        /// <summary>Player fields some loaded rule reads, per player prefix.</summary>
        public PlayerFields[] Prefixes { get; }
        /// <summary>Cached key strings per player prefix.</summary>
        public string[][] PrefixKeys { get; }
        /// <summary>True when a loaded rule reads a derived key (team/self flags, team names).</summary>
        public bool Derived { get; set; }
        /// <summary>Upper bound of the keys one event's data holds; sizes its dictionary up front.</summary>
        public int Capacity { get; private set; }

        /// <summary>
        /// Sets <see cref="Capacity"/> once every rule is added: requested keys, player fields and derived
        /// keys, plus <paramref name="extra"/> keys merged in later (globals, provider values).
        /// </summary>
        public void SetCapacity(int derivedKeys, int extra)
        {
            int count = extra;
            foreach (bool key in Keys)
            {
                if (key)
                {
                    count++;
                }
            }

            foreach (PlayerFields fields in Prefixes)
            {
                count += BitOperations.PopCount((uint)fields);
            }

            Capacity = Derived ? count + derivedKeys : count;
        }

        /// <summary>Marks what <paramref name="key"/> needs. Keys the extractor never writes are ignored.</summary>
        public void Add(
            string key,
            IReadOnlyList<string> eventKeys,
            IReadOnlyList<string> prefixes,
            IReadOnlyList<string> derivedKeys)
        {
            for (int i = 0; i < eventKeys.Count; i++)
            {
                if (string.Equals(eventKeys[i], key, StringComparison.Ordinal))
                {
                    Keys[i] = true;
                    return;
                }
            }

            for (int i = 0; i < derivedKeys.Count; i++)
            {
                if (string.Equals(derivedKeys[i], key, StringComparison.Ordinal))
                {
                    Derived = true;
                    return;
                }
            }

            for (int i = 0; i < prefixes.Count; i++)
            {
                if (PlayerKeys.TryMatch(key, prefixes[i], out PlayerFields field))
                {
                    Prefixes[i] |= field;
                }
            }
        }
    }
}
