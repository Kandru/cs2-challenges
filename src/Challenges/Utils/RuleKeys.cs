using Challenges.Extractors;

namespace Challenges.Utils
{
    /// <summary>
    /// Resolves rule keys to the canonical keys the data sources of a challenge type write. Runs at load;
    /// sources therefore write each value under one key and one prefix per controller.
    /// </summary>
    internal static class RuleKeys
    {
        private static readonly Dictionary<string, IKeySource[]> SourcesByType = BuildIndex();

        /// <summary>
        /// Canonical spelling of <paramref name="key"/> for <paramref name="type"/>: a key alias maps to its
        /// key, an alias prefix to the canonical prefix of the same controller (any suffix, so situation keys
        /// such as <c>victim.grenade</c> follow too). Unknown types and keys are returned unchanged.
        /// </summary>
        public static string Canonical(string type, string key)
        {
            if (!SourcesByType.TryGetValue(type, out IKeySource[]? sources))
            {
                return key;
            }

            foreach (IKeySource source in sources)
            {
                if (TryCanonical(source, key, out string canonical))
                {
                    return canonical;
                }
            }

            return key;
        }

        /// <summary>True when <paramref name="source"/> knows <paramref name="key"/> as a key, key alias or alias prefix.</summary>
        private static bool TryCanonical(IKeySource source, string key, out string canonical)
        {
            canonical = key;
            if (Contains(source.Keys, key))
            {
                return true;
            }

            if (source.KeyAliases.TryGetValue(key, out string? aliased))
            {
                canonical = aliased;
                return true;
            }

            int dot = key.IndexOf('.');
            if (dot <= 0 || !source.PrefixAliases.TryGetValue(key[..dot], out string? prefix))
            {
                return false;
            }

            canonical = string.Concat(prefix, key.AsSpan(dot));
            return true;
        }

        private static bool Contains(IReadOnlyList<string> keys, string key)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (string.Equals(keys[i], key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Every extractor by challenge type, plus the synthetic types that have no extractor.</summary>
        private static Dictionary<string, IKeySource[]> BuildIndex()
        {
            Dictionary<string, List<IKeySource>> lists = new(StringComparer.Ordinal);
            foreach (IExtractor extractor in Registry.All)
            {
                foreach (string type in extractor.ChallengeTypes)
                {
                    Add(lists, type, extractor);
                }
            }

            Add(lists, SituationMemory.TradeType, SituationMemory.TradeKeys);

            Dictionary<string, IKeySource[]> index = new(lists.Count, StringComparer.Ordinal);
            foreach ((string type, List<IKeySource> list) in lists)
            {
                index[type] = [.. list];
            }

            return index;
        }

        private static void Add(Dictionary<string, List<IKeySource>> lists, string type, IKeySource source)
        {
            if (!lists.TryGetValue(type, out List<IKeySource>? list))
            {
                lists[type] = list = [];
            }

            list.Add(source);
        }
    }

    /// <summary>Hand-written key layout of a synthetic challenge type (no extractor).</summary>
    internal sealed class SyntheticKeySource(
        IReadOnlyList<string> keys,
        IReadOnlyList<string> playerPrefixes,
        IReadOnlyDictionary<string, string> keyAliases,
        IReadOnlyDictionary<string, string> prefixAliases) : IKeySource
    {
        public IReadOnlyList<string> Keys { get; } = keys;
        public IReadOnlyList<string> PlayerPrefixes { get; } = playerPrefixes;
        public IReadOnlyDictionary<string, string> KeyAliases { get; } = keyAliases;
        public IReadOnlyDictionary<string, string> PrefixAliases { get; } = prefixAliases;
    }
}
