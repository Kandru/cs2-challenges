using Challenges.Attributes;
using Challenges.Extractors;
using Challenges.Utils;

namespace Challenges
{
    public partial class ChallengeEngine
    {
        /// <summary>
        /// Global preconditions shared by every task of a type. A bit names the state the tasks require;
        /// <see cref="ReadClosedGates"/> returns the bits the current game state violates.
        /// </summary>
        [Flags]
        private enum GlobalGate : byte
        {
            None = 0,
            NoWarmup = 1,
            Warmup = 2,
            DuringRound = 4,
            NotDuringRound = 8,
            WarmupBits = NoWarmup | Warmup,
            RoundBits = DuringRound | NotDuringRound,
        }

        /// <summary>One bit per <c>global.*</c> key the engine writes into event data.</summary>
        [Flags]
        private enum GlobalKeyFlags : byte
        {
            None = 0,
            IsWarmup = 1,
            IsDuringRound = 2,
            MapName = 4,
            HasHostages = 8,
        }

        /// <summary>Tasks of one challenge type in the current schedule, compiled at load.</summary>
        private sealed class TypePlan(int id, string type)
        {
            public int Id { get; } = id;
            public string Type { get; } = type;
            public List<(ChallengeDefinition Challenge, ChallengeTask Task)> Tasks { get; } = [];
            /// <summary>Preconditions every task of this type has; checked before any data is read.</summary>
            public GlobalGate Gate { get; set; }

            public bool IsClosed(GlobalGate closed) => (Gate & closed) != 0;
        }

        /// <summary>An extractor with at least one active type, its active types, and what to fill.</summary>
        private sealed class ExtractorPlan
        {
            /// <summary><paramref name="globalKeys"/> counts the <c>global.*</c> keys merged in before enqueue.</summary>
            public ExtractorPlan(IExtractor extractor, TypePlan[] types, AttributeRegistry attributes, int globalKeys)
            {
                Extractor = extractor;
                GameEvent = extractor as IGameEventExtractor;
                Types = types;
                HashSet<string> keys = RuleKeysOf(types);
                Attributes = attributes.BuildDemand(extractor, keys);
                Demand = BuildDemand(extractor, types, keys, globalKeys + (Attributes?.KeyCount ?? 0));
            }

            public IExtractor Extractor { get; }
            /// <summary><see cref="Extractor"/> of a game event; null for listeners, whose handlers fill the data.</summary>
            public IGameEventExtractor? GameEvent { get; }
            public TypePlan[] Types { get; }
            public FillDemand Demand { get; }
            /// <summary>Provider values to add after <c>Fill</c>; null when no rule of these types reads one.</summary>
            public AttributeDemand? Attributes { get; }

            public bool AllClosed(GlobalGate closed)
            {
                if (closed == GlobalGate.None)
                {
                    return false;
                }

                foreach (TypePlan type in Types)
                {
                    if (!type.IsClosed(closed))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Canonical keys the rules of <paramref name="types"/> read.</summary>
        private static HashSet<string> RuleKeysOf(TypePlan[] types)
        {
            HashSet<string> keys = new(StringComparer.Ordinal);
            foreach (TypePlan type in types)
            {
                foreach ((_, ChallengeTask task) in type.Tasks)
                {
                    foreach (CompiledRule rule in task.CompiledRules)
                    {
                        keys.Add(rule.Key);
                    }
                }
            }

            return keys;
        }

        /// <summary>
        /// <paramref name="keys"/> mapped onto the extractor's keys, player prefixes and derived keys. Keys owned
        /// by attribute providers (player suffixes such as <c>userid.grenade</c> are not player fields) or
        /// <c>global.*</c> do not match and are ignored. <paramref name="extraKeys"/> counts keys merged in after the
        /// fill (globals, provider values).
        /// </summary>
        private static FillDemand BuildDemand(
            IExtractor extractor,
            TypePlan[] types,
            HashSet<string> keys,
            int extraKeys)
        {
            IReadOnlyList<string> challengeTypes = extractor.ChallengeTypes;
            FillDemand need = new(challengeTypes.Count, extractor.Keys.Count, extractor.PlayerPrefixes);
            for (int i = 0; i < challengeTypes.Count; i++)
            {
                foreach (TypePlan type in types)
                {
                    if (string.Equals(type.Type, challengeTypes[i], StringComparison.Ordinal))
                    {
                        need.Types[i] = true;
                    }
                }
            }

            foreach (string key in keys)
            {
                need.Add(key, extractor.Keys, extractor.PlayerPrefixes, extractor.DerivedKeys);
            }

            need.SetCapacity(extractor.DerivedKeys.Count, extraKeys);
            return need;
        }

        /// <summary>Source of <see cref="_typeLayout"/>; unique across engine instances.</summary>
        private static int _nextTypeLayout;

        /// <summary>Union of all type gates.</summary>
        private GlobalGate _gateUnion;
        /// <summary>
        /// <see cref="_gateUnion"/> plus the warmup bits when a rule reads <c>global.iswarmup</c>; selects which
        /// global values <see cref="ReadClosedGates"/> reads per event.
        /// </summary>
        private GlobalGate _gateReads;
        /// <summary>Type plans by <see cref="TypePlan.Id"/>.</summary>
        private TypePlan[] _typeList = [];
        /// <summary>Identifies the current type id assignment for <see cref="PlayerState.OpenTypes"/>.</summary>
        private int _typeLayout;
        private string _scheduleKey = string.Empty;

        /// <summary>
        /// False when every task of <paramref name="type"/> is complete for <paramref name="state"/>.
        /// Locked or cooling-down tasks count as open. Recomputed lazily after progress changed.
        /// </summary>
        private bool IsTypeOpen(PlayerState state, TypePlan type)
        {
            bool[]? open = state.OpenTypes;
            if (open == null
                || state.OpenTypesDirty
                || state.OpenTypesLayout != _typeLayout
                || state.OpenTypesVersion != state.ProgressVersion)
            {
                open = RecomputeOpenTypes(state);
            }

            return open[type.Id];
        }

        private bool[] RecomputeOpenTypes(PlayerState state)
        {
            bool[] open = state.OpenTypes is { } existing && existing.Length == _typeList.Length
                ? existing
                : new bool[_typeList.Length];
            foreach (TypePlan type in _typeList)
            {
                bool any = false;
                foreach ((ChallengeDefinition challenge, ChallengeTask task) in type.Tasks)
                {
                    if (!ChallengeProgress.IsTaskComplete(state, _scheduleKey, challenge.Id, task))
                    {
                        any = true;
                        break;
                    }
                }

                open[type.Id] = any;
            }

            state.OpenTypes = open;
            state.OpenTypesLayout = _typeLayout;
            state.OpenTypesVersion = state.ProgressVersion;
            state.OpenTypesDirty = false;
            return open;
        }

        /// <summary>
        /// Gate bits the current game state violates. Reads warmup / round state only when a loaded type
        /// is gated on it or a rule reads <c>global.iswarmup</c>; <see cref="MergeGlobalData"/> reuses the
        /// warmup bit. A bit no type is gated on closes nothing.
        /// </summary>
        private GlobalGate ReadClosedGates()
        {
            GlobalGate union = _gateReads;
            if (union == GlobalGate.None)
            {
                return GlobalGate.None;
            }

            GlobalGate closed = GlobalGate.None;
            if ((union & GlobalGate.WarmupBits) != 0)
            {
                closed |= (GetGameRules()?.WarmupPeriod ?? false) ? GlobalGate.NoWarmup : GlobalGate.Warmup;
            }

            if ((union & GlobalGate.RoundBits) != 0)
            {
                closed |= State.DuringRound ? GlobalGate.NotDuringRound : GlobalGate.DuringRound;
            }

            return closed;
        }

        private static GlobalKeyFlags GlobalKey(string key) =>
            key switch
            {
                GlobalKeys.IsWarmup => GlobalKeyFlags.IsWarmup,
                GlobalKeys.IsDuringRound => GlobalKeyFlags.IsDuringRound,
                GlobalKeys.MapName => GlobalKeyFlags.MapName,
                GlobalKeys.HasHostages => GlobalKeyFlags.HasHostages,
                _ => GlobalKeyFlags.None,
            };

        /// <summary>Preconditions one task's rules pin down; every such rule must pass for progress.</summary>
        private static GlobalGate TaskGate(ChallengeTask task)
        {
            GlobalGate gate = GlobalGate.None;
            foreach (CompiledRule rule in task.CompiledRules)
            {
                switch (rule.Key)
                {
                    case GlobalKeys.IsWarmup when RequiredBool(rule) is bool warmup:
                        gate |= warmup ? GlobalGate.Warmup : GlobalGate.NoWarmup;
                        break;
                    case GlobalKeys.IsDuringRound when RequiredBool(rule) is bool duringRound:
                        gate |= duringRound ? GlobalGate.DuringRound : GlobalGate.NotDuringRound;
                        break;
                }
            }

            return gate;
        }

        /// <summary>
        /// The only value of a <c>true</c>/<c>false</c> key that passes <paramref name="rule"/>, or null when
        /// the rule does not pin one down. Mirrors <see cref="ChallengeRules"/> operator semantics.
        /// </summary>
        private static bool? RequiredBool(CompiledRule rule)
        {
            switch (rule.Op)
            {
                case RuleOperator.BoolEqual:
                    return rule.Bool;
                case RuleOperator.BoolNotEqual:
                    return !rule.Bool;
                case RuleOperator.Equal or RuleOperator.NotEqual:
                    bool equals = rule.Op == RuleOperator.Equal;
                    if (string.Equals(rule.Text, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return equals;
                    }

                    if (string.Equals(rule.Text, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return !equals;
                    }

                    return null;
                default:
                    return null;
            }
        }
    }
}
