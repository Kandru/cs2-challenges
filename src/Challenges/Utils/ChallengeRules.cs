using System.Globalization;
using System.Text;
using Challenges.Utils;

namespace Challenges
{
    /// <summary>Pure rule / skip evaluation over copied event data and progress state.</summary>
    internal static class ChallengeRules
    {
        /// <summary>
        /// True when the task must not receive progress.
        /// <paramref name="reason"/> is only set when <paramref name="explain"/> is true.
        /// </summary>
        public static bool IsSkipped(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge,
            ChallengeTask task,
            Dictionary<string, string> data,
            long now,
            bool allowBots,
            bool explain,
            out string? reason)
        {
            reason = null;

            if (ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task))
            {
                if (explain)
                {
                    reason = "done";
                }

                return true;
            }

            if (!ChallengeProgress.AreRequirementsMet(state, scheduleKey, challenge, task))
            {
                if (explain)
                {
                    reason = "locked";
                }

                return true;
            }

            if (task.Cooldown > 0
                && ChallengeProgress.GetProgress(state, scheduleKey, challenge.Id, task.Id) is { } progress
                && progress.LastUpdate + task.Cooldown > now)
            {
                if (explain)
                {
                    reason = $"cooldown {progress.LastUpdate + task.Cooldown - now}s";
                }

                return true;
            }

            return !RulesPass(task, data, allowBots, explain, out reason);
        }

        /// <summary>False when a rule fails; <paramref name="reason"/> set only when <paramref name="explain"/>.</summary>
        public static bool RulesPass(
            ChallengeTask task,
            Dictionary<string, string> data,
            bool allowBots,
            bool explain,
            out string? reason)
        {
            reason = null;
            foreach (CompiledRule rule in task.CompiledRules)
            {
                if (allowBots && rule.SkipWhenBotsAllowed)
                {
                    continue;
                }

                if (rule.RequiredType is { } requiredType
                    && !string.Equals(task.Type, requiredType, StringComparison.Ordinal))
                {
                    if (explain)
                    {
                        reason = $"{rule.Source.Key} requires {requiredType}";
                    }

                    return false;
                }

                if (!data.TryGetValue(rule.LookupKey, out string? current))
                {
                    if (explain)
                    {
                        reason = $"{rule.Source.Key} {rule.Source.Operator} {rule.Text} (missing)";
                    }

                    return false;
                }

                if (rule.Op == RuleOperator.Invalid)
                {
                    if (explain)
                    {
                        reason = $"{rule.Source.Key} unknown operator {rule.Source.Operator}";
                    }

                    return false;
                }

                if (!Matches(rule, current))
                {
                    if (explain)
                    {
                        reason = $"{rule.Source.Key} {rule.Source.Operator} {rule.Text} ({current})";
                    }

                    return false;
                }
            }

            return true;
        }

        /// <summary>Debug text <c>key=value</c> per rule; keys as written in the blueprint.</summary>
        public static string FormatParams(ChallengeTask task, Dictionary<string, string> data)
        {
            CompiledRule[] rules = task.CompiledRules;
            if (rules.Length == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            for (int i = 0; i < rules.Length; i++)
            {
                CompiledRule rule = rules[i];
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(rule.Source.Key);
                sb.Append('=');
                sb.Append(data.TryGetValue(rule.LookupKey, out string? value) ? value : "?");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Prepares every rule of <paramref name="task"/>; call after the task is normalized. Keys are resolved to
        /// their canonical spelling for the task type (<see cref="RuleKeys.Canonical"/>).
        /// </summary>
        public static CompiledRule[] Compile(ChallengeTask task)
        {
            List<ChallengeRule> rules = task.Rules;
            if (rules.Count == 0)
            {
                return [];
            }

            CompiledRule[] compiled = new CompiledRule[rules.Count];
            for (int i = 0; i < compiled.Length; i++)
            {
                ChallengeRule rule = rules[i];
                compiled[i] = new CompiledRule(
                    rule,
                    RuleKeys.Canonical(task.Type, rule.Key),
                    ParseOperator(rule.Operator));
            }

            return compiled;
        }

        /// <summary>
        /// Operator for its YAML spelling; <see cref="RuleOperator.Invalid"/> when unknown.
        /// The case labels are also the catalog's operator list (<c>tools/generate_events.py</c>).
        /// </summary>
        public static RuleOperator ParseOperator(string op)
        {
            switch (op)
            {
                case "==":
                    return RuleOperator.Equal;
                case "!=":
                    return RuleOperator.NotEqual;
                case ">":
                    return RuleOperator.Greater;
                case "<":
                    return RuleOperator.Less;
                case ">=":
                    return RuleOperator.GreaterOrEqual;
                case "<=":
                    return RuleOperator.LessOrEqual;
                case "bool==":
                    return RuleOperator.BoolEqual;
                case "bool!=":
                    return RuleOperator.BoolNotEqual;
                case "contains":
                    return RuleOperator.Contains;
                case "!contains":
                    return RuleOperator.NotContains;
                default:
                    return RuleOperator.Invalid;
            }
        }

        private static bool Matches(CompiledRule rule, string current) =>
            rule.Op switch
            {
                RuleOperator.Equal => string.Equals(current, rule.Text, StringComparison.OrdinalIgnoreCase),
                RuleOperator.NotEqual => !string.Equals(current, rule.Text, StringComparison.OrdinalIgnoreCase),
                RuleOperator.Greater => TryCompare(rule, current, out int gt) && gt > 0,
                RuleOperator.Less => TryCompare(rule, current, out int lt) && lt < 0,
                RuleOperator.GreaterOrEqual => TryCompare(rule, current, out int ge) && ge >= 0,
                RuleOperator.LessOrEqual => TryCompare(rule, current, out int le) && le <= 0,
                RuleOperator.BoolEqual => rule.Bool is bool b && bool.TryParse(current, out bool a) && a == b,
                RuleOperator.BoolNotEqual => rule.Bool is bool d && bool.TryParse(current, out bool c) && c != d,
                RuleOperator.Contains => current.Contains(rule.Text, StringComparison.OrdinalIgnoreCase),
                RuleOperator.NotContains => !current.Contains(rule.Text, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };

        /// <summary>Compares <paramref name="current"/> with the pre-parsed rule number in single precision.</summary>
        private static bool TryCompare(CompiledRule rule, string current, out int result)
        {
            if (rule.Number is not double right
                || !float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out float left))
            {
                result = 0;
                return false;
            }

            result = ((double)left).CompareTo(right);
            return true;
        }
    }
}
