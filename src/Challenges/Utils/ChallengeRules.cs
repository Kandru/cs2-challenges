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

        public static string FormatParams(ChallengeTask task, Dictionary<string, string> data)
        {
            int count = task.Rules.Count;
            if (count == 0)
            {
                return string.Empty;
            }

            StringBuilder sb = new();
            for (int i = 0; i < count; i++)
            {
                ChallengeRule rule = task.Rules[i];
                if (i > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(rule.Key);
                sb.Append('=');
                sb.Append(data.TryGetValue(DataLookupKey(rule), out string? value) ? value : "?");
            }

            return sb.ToString();
        }

        private static string DataLookupKey(ChallengeRule rule) =>
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
