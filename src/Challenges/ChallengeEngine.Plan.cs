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
        private sealed class ExtractorPlan(IExtractor extractor, TypePlan[] types)
        {
            public IExtractor Extractor { get; } = extractor;
            public TypePlan[] Types { get; } = types;
            public FillDemand Demand { get; } = BuildDemand(extractor, types);

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

        /// <summary>
        /// Union of the keys the rules of <paramref name="types"/> read, mapped onto the extractor's keys,
        /// player prefixes and derived keys. Keys owned by situations or <c>global.*</c> do not match and
        /// are ignored.
        /// </summary>
        private static FillDemand BuildDemand(IExtractor extractor, TypePlan[] types)
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

            foreach (TypePlan type in types)
            {
                foreach ((_, ChallengeTask task) in type.Tasks)
                {
                    foreach (ChallengeRule rule in task.Rules)
                    {
                        need.Add(rule.Key, extractor.Keys, extractor.PlayerPrefixes, extractor.DerivedKeys);
                    }
                }
            }

            return need;
        }

        /// <summary>Source of <see cref="_typeLayout"/>; unique across engine instances.</summary>
        private static int _nextTypeLayout;

        /// <summary>Union of all type gates; selects which global values must be read per event.</summary>
        private GlobalGate _gateUnion;
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
        /// is gated on it. Uses the same sources as <see cref="MergeGlobalData"/>.
        /// </summary>
        private GlobalGate ReadClosedGates()
        {
            GlobalGate union = _gateUnion;
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

        /// <summary>Preconditions one task's rules pin down; every such rule must pass for progress.</summary>
        private static GlobalGate TaskGate(ChallengeTask task)
        {
            GlobalGate gate = GlobalGate.None;
            foreach (ChallengeRule rule in task.Rules)
            {
                switch (rule.Key)
                {
                    case "global.iswarmup" when RequiredBool(rule) is bool warmup:
                        gate |= warmup ? GlobalGate.Warmup : GlobalGate.NoWarmup;
                        break;
                    case "global.isduringround" when RequiredBool(rule) is bool duringRound:
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
        private static bool? RequiredBool(ChallengeRule rule)
        {
            switch (rule.Operator)
            {
                case "bool==" when bool.TryParse(rule.Value, out bool value):
                    return value;
                case "bool!=" when bool.TryParse(rule.Value, out bool value):
                    return !value;
                case "==" or "!=":
                    bool equals = rule.Operator == "==";
                    if (string.Equals(rule.Value, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return equals;
                    }

                    if (string.Equals(rule.Value, "false", StringComparison.OrdinalIgnoreCase))
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
