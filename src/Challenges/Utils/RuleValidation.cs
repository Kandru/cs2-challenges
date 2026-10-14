using Challenges.Attributes;
using Challenges.Extractors;

namespace Challenges.Utils
{
    /// <summary>
    /// Load-time checks of compiled rules: unknown challenge types, keys no data source of the type writes,
    /// unknown operators and keys limited to another type. Only reports; nothing is rejected.
    /// </summary>
    internal static class RuleValidation
    {
        /// <summary>Largest edit distance for a "did you mean" suggestion.</summary>
        private const int MaxSuggestionDistance = 3;

        private static readonly Dictionary<string, KnownKeys> KnownByType = new(StringComparer.Ordinal);
        private static readonly Lock KnownLock = new();
        private static string[]? _sortedTypes;

        /// <summary>One message per problem of <paramref name="def"/>; call after its rules are compiled.</summary>
        public static List<string> Check(ChallengeDefinition def)
        {
            List<string> problems = [];
            foreach (ChallengeTask task in def.Tasks)
            {
                string where = $"blueprint {def.Id}, task {task.Id}";
                KnownKeys? known = Known(task.Type);
                if (known == null)
                {
                    // Marker tasks (e.g. type: marker) get no events; other tasks set or reset them via actions.
                    if (IsActionTarget(def, task.Id))
                    {
                        continue;
                    }

                    problems.Add($"{where}: unknown type {task.Type}{Suggest(task.Type, SortedTypes())}");
                }

                foreach (CompiledRule rule in task.CompiledRules)
                {
                    string key = rule.Source.Key;
                    if (rule.Op == RuleOperator.Invalid)
                    {
                        problems.Add($"{where}: unknown operator {rule.Source.Operator} for key {key}");
                    }

                    if (known == null)
                    {
                        continue;
                    }

                    if (!known.Contains(key))
                    {
                        problems.Add($"{where}: unknown key {key} for type {task.Type}{Suggest(key, known.Sorted)}");
                        continue;
                    }

                    if (rule.RequiredType is { } required
                        && !string.Equals(required, task.Type, StringComparison.Ordinal))
                    {
                        problems.Add($"{where}: key {key} only works for type {required}");
                    }
                }
            }

            return problems;
        }

        /// <summary>True when a <c>task.*</c> action of <paramref name="def"/> names <paramref name="taskId"/>.</summary>
        private static bool IsActionTarget(ChallengeDefinition def, string taskId)
        {
            foreach (ChallengeTask task in def.Tasks)
            {
                foreach (ChallengeAction action in task.Actions)
                {
                    if (!action.Type.StartsWith("task.", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    foreach (string value in action.Values)
                    {
                        if (string.Equals(value.Trim(), taskId, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>Keys <paramref name="type"/> can deliver, aliases included; null for an unknown type.</summary>
        private static KnownKeys? Known(string type)
        {
            lock (KnownLock)
            {
                if (KnownByType.TryGetValue(type, out KnownKeys? cached))
                {
                    return cached;
                }

                if (!RuleKeys.TryGetSources(type, out IKeySource[] sources))
                {
                    return null;
                }

                KnownKeys known = Build(sources);
                KnownByType[type] = known;
                return known;
            }
        }

        private static KnownKeys Build(IKeySource[] sources)
        {
            HashSet<string> keys = new(GlobalKeys.All, StringComparer.Ordinal);
            foreach (IKeySource source in sources)
            {
                IExtractor? extractor = source as IExtractor;
                keys.UnionWith(source.Keys);
                keys.UnionWith(source.KeyAliases.Keys);
                if (extractor != null)
                {
                    keys.UnionWith(extractor.DerivedKeys);
                    foreach (AttributeEventKey eventKey in AttributeRegistry.AllEventKeys)
                    {
                        if (string.Equals(eventKey.EventClassName, extractor.EventClassName, StringComparison.Ordinal))
                        {
                            keys.Add(eventKey.Key);
                        }
                    }
                }

                foreach (string prefix in source.PlayerPrefixes)
                {
                    AddPlayerKeys(keys, prefix, extractor != null);
                }

                foreach (string prefix in source.PrefixAliases.Keys)
                {
                    AddPlayerKeys(keys, prefix, extractor != null);
                }
            }

            string[] sorted = [.. keys];
            Array.Sort(sorted, StringComparer.Ordinal);
            return new KnownKeys(keys, sorted);
        }

        /// <summary>Player data suffixes, plus the provider suffixes for extractor prefixes (not synthetic types).</summary>
        private static void AddPlayerKeys(HashSet<string> keys, string prefix, bool providers)
        {
            foreach (string key in PlayerKeys.For(prefix))
            {
                keys.Add(key);
            }

            if (!providers)
            {
                return;
            }

            foreach (AttributeSuffix suffix in AttributeRegistry.AllPlayerSuffixes)
            {
                keys.Add($"{prefix}.{suffix.Suffix}");
            }
        }

        private static string[] SortedTypes()
        {
            lock (KnownLock)
            {
                if (_sortedTypes == null)
                {
                    string[] types = [.. RuleKeys.Types];
                    Array.Sort(types, StringComparer.Ordinal);
                    _sortedTypes = types;
                }

                return _sortedTypes;
            }
        }

        /// <summary><c>; did you mean x?</c> for the closest candidate within <see cref="MaxSuggestionDistance"/>, else empty.</summary>
        private static string Suggest(string value, string[] candidates)
        {
            string? best = null;
            int bestDistance = MaxSuggestionDistance + 1;
            foreach (string candidate in candidates)
            {
                if (Math.Abs(candidate.Length - value.Length) >= bestDistance)
                {
                    continue;
                }

                int distance = Levenshtein(value, candidate, bestDistance);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best == null ? string.Empty : $"; did you mean {best}?";
        }

        /// <summary>Edit distance of <paramref name="a"/> and <paramref name="b"/>; any value of at least <paramref name="limit"/> means too far.</summary>
        private static int Levenshtein(string a, string b, int limit)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++)
            {
                previous[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                int rowMin = current[0];
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + cost);
                    rowMin = Math.Min(rowMin, current[j]);
                }

                if (rowMin >= limit)
                {
                    return limit;
                }

                (previous, current) = (current, previous);
            }

            return previous[b.Length];
        }

        private sealed class KnownKeys(HashSet<string> keys, string[] sorted)
        {
            /// <summary>Ordinal order, so the suggestion for a tie is stable.</summary>
            public string[] Sorted { get; } = sorted;

            public bool Contains(string key) => keys.Contains(key);
        }
    }
}
