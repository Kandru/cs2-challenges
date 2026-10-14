using System.Globalization;
using Challenges.Utils;

namespace Challenges.Bench
{
    /// <summary>
    /// <c>ChallengeRules.RulesPass</c> as it was before compiled rules (phase 2 and earlier): string event data,
    /// operator and key strings read per check, rule values parsed per check. Kept verbatim for comparison.
    /// </summary>
    internal static class LegacyRules
    {
        public static bool RulesPass(
            ChallengeTask task,
            Dictionary<string, string> data,
            bool allowBots,
            bool explain,
            out string? reason)
        {
            reason = null;
            foreach (ChallengeRule rule in task.Rules)
            {
                if (allowBots
                    && rule.Operator == "bool=="
                    && rule.Key.EndsWith(".isbot", StringComparison.Ordinal)
                    && rule.Value.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(rule.Key, "isace", StringComparison.Ordinal)
                    && !string.Equals(task.Type, "player_kill", StringComparison.Ordinal))
                {
                    if (explain)
                    {
                        reason = "isace requires player_kill";
                    }

                    return false;
                }

                string lookupKey = DataLookupKey(rule);
                if (!data.TryGetValue(lookupKey, out string? current))
                {
                    if (explain)
                    {
                        reason = $"{rule.Key} {rule.Operator} {rule.Value} (missing)";
                    }

                    return false;
                }

                if (!TryEvaluate(rule, current, out bool matched))
                {
                    if (explain)
                    {
                        reason = $"{rule.Key} unknown operator {rule.Operator}";
                    }

                    return false;
                }

                if (!matched)
                {
                    if (explain)
                    {
                        reason = $"{rule.Key} {rule.Operator} {rule.Value} ({current})";
                    }

                    return false;
                }
            }

            return true;
        }

        public static string DataLookupKey(ChallengeRule rule) =>
            string.Equals(rule.Key, "isace", StringComparison.Ordinal)
                ? AceBounds.DataKey(rule)
                : rule.Key;

        private static bool TryEvaluate(ChallengeRule rule, string current, out bool matched)
        {
            string target = rule.Value;
            switch (rule.Operator)
            {
                case "==":
                    matched = string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                    return true;
                case "!=":
                    matched = !string.Equals(current, target, StringComparison.OrdinalIgnoreCase);
                    return true;
                case ">":
                    matched = TryCompare(current, target, out int gt) && gt > 0;
                    return true;
                case "<":
                    matched = TryCompare(current, target, out int lt) && lt < 0;
                    return true;
                case ">=":
                    matched = TryCompare(current, target, out int ge) && ge >= 0;
                    return true;
                case "<=":
                    matched = TryCompare(current, target, out int le) && le <= 0;
                    return true;
                case "bool==":
                    matched = bool.TryParse(current, out bool a) && bool.TryParse(target, out bool b) && a == b;
                    return true;
                case "bool!=":
                    matched = bool.TryParse(current, out bool c) && bool.TryParse(target, out bool d) && c != d;
                    return true;
                case "contains":
                    matched = current.Contains(target, StringComparison.OrdinalIgnoreCase);
                    return true;
                case "!contains":
                    matched = !current.Contains(target, StringComparison.OrdinalIgnoreCase);
                    return true;
                default:
                    matched = false;
                    return false;
            }
        }

        private static bool TryCompare(string current, string target, out int result)
        {
            if (!float.TryParse(current, NumberStyles.Float, CultureInfo.InvariantCulture, out float left)
                || !float.TryParse(target, NumberStyles.Float, CultureInfo.InvariantCulture, out float right))
            {
                result = 0;
                return false;
            }

            result = left.CompareTo(right);
            return true;
        }
    }
}
