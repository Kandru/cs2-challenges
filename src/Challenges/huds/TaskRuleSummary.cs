using System.Globalization;
using System.Text;
using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Compact, player-facing rule lines for the challenge detail panel.</summary>
    internal static class TaskRuleSummary
    {
        private const string Sep = " · ";

        /// <summary>Visible tasks in completion order (YAML order with <c>requires</c> respected).</summary>
        public static List<ChallengeTask> VisibleInOrder(ChallengeDefinition challenge)
        {
            List<ChallengeTask> visible = [];
            bool needsSort = false;
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                visible.Add(task);
                if (task.Requires.Count > 0)
                {
                    needsSort = true;
                }
            }

            if (!needsSort || visible.Count <= 1)
            {
                return visible;
            }

            List<ChallengeTask> ordered = new(visible.Count);
            HashSet<string> done = new(StringComparer.Ordinal);
            while (ordered.Count < visible.Count)
            {
                bool progressed = false;
                foreach (ChallengeTask task in visible)
                {
                    if (done.Contains(task.Id) || !RequirementsReady(challenge, task, done))
                    {
                        continue;
                    }

                    ordered.Add(task);
                    done.Add(task.Id);
                    progressed = true;
                }

                if (progressed)
                {
                    continue;
                }

                foreach (ChallengeTask task in visible)
                {
                    if (done.Add(task.Id))
                    {
                        ordered.Add(task);
                    }
                }

                break;
            }

            return ordered;
        }

        public static string Format(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            StringBuilder? sb = null;
            foreach (ChallengeRule rule in task.Rules)
            {
                if (rule.Key.StartsWith("global.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string? phrase = FormatRule(player, rule);
                if (string.IsNullOrEmpty(phrase))
                {
                    continue;
                }

                AppendPart(ref sb, phrase);
            }

            foreach (ChallengeTask breaker in challenge.Tasks)
            {
                if (breaker.Visible || !BreaksTask(breaker, task.Id))
                {
                    continue;
                }

                string reason = Titles.For(player, breaker.Title);
                if (string.IsNullOrEmpty(reason))
                {
                    continue;
                }

                AppendPart(ref sb, Context.Text(player, "hud.menu.resets", ("{reason}", reason)));
            }

            return sb?.ToString() ?? string.Empty;
        }

        private static void AppendPart(ref StringBuilder? sb, string part)
        {
            if (sb == null)
            {
                sb = new StringBuilder(part.Length + 32);
                sb.Append(part);
                return;
            }

            sb.Append(Sep).Append(part);
        }

        private static bool RequirementsReady(
            ChallengeDefinition challenge,
            ChallengeTask task,
            HashSet<string> done)
        {
            foreach (string requiredId in task.Requires)
            {
                if (!challenge.TaskById.TryGetValue(requiredId, out ChallengeTask? required)
                    || !required.Visible
                    || done.Contains(requiredId))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private static bool BreaksTask(ChallengeTask breaker, string taskId)
        {
            foreach (ChallengeAction action in breaker.Actions)
            {
                if (action.Type is not ("notify.player.progress.rule_broken"
                    or "notify.player.completed.rule_broken"))
                {
                    continue;
                }

                foreach (string id in action.Values)
                {
                    if (string.Equals(id, taskId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static string? FormatRule(CCSPlayerController player, ChallengeRule rule)
        {
            string op = rule.Operator;
            string value = rule.Value;
            string keyId = rule.Key.Replace('.', '_');

            if (op is "bool==" or "bool!=")
            {
                bool expected = IsTruthy(value);
                if (op == "bool!=")
                {
                    expected = !expected;
                }

                string polarityKey = $"hud.rule.{keyId}.{(expected ? "true" : "false")}";
                string polarity = Context.Text(player, polarityKey);
                if (!string.Equals(polarity, polarityKey, StringComparison.Ordinal))
                {
                    return polarity;
                }

                string label = RuleLabel(player, keyId, rule.Key);
                return expected
                    ? label
                    : Context.Text(player, "hud.rule.not", ("{rule}", label));
            }

            string baseLabel = RuleLabel(player, keyId, rule.Key);
            if (string.IsNullOrEmpty(value))
            {
                return baseLabel;
            }

            string pretty = PrettyValue(value);
            return op switch
            {
                "==" or "string==" => Context.Text(
                    player,
                    "hud.rule.equals",
                    ("{rule}", baseLabel),
                    ("{value}", pretty)),
                "!=" or "string!=" => Context.Text(
                    player,
                    "hud.rule.not_equals",
                    ("{rule}", baseLabel),
                    ("{value}", pretty)),
                ">" or ">=" or "<" or "<=" => $"{baseLabel} {op} {pretty}",
                _ => $"{baseLabel}: {pretty}",
            };
        }

        private static string RuleLabel(CCSPlayerController player, string keyId, string rawKey)
        {
            string locKey = "hud.rule." + keyId;
            string localized = Context.Text(player, locKey);
            return string.Equals(localized, locKey, StringComparison.Ordinal)
                ? HumanizeKey(rawKey)
                : localized;
        }

        private static string HumanizeKey(string key)
        {
            ReadOnlySpan<char> leaf = key.AsSpan();
            int dot = leaf.LastIndexOf('.');
            if (dot >= 0 && dot < leaf.Length - 1)
            {
                leaf = leaf[(dot + 1)..];
            }

            string text = leaf.ToString().Replace('_', ' ');
            if (text.StartsWith("is ", StringComparison.OrdinalIgnoreCase))
            {
                text = text[3..];
            }
            else if (text.StartsWith("is", StringComparison.OrdinalIgnoreCase) && text.Length > 2)
            {
                text = text[2..];
            }

            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text.ToLowerInvariant());
        }

        private static string PrettyValue(string value)
        {
            ReadOnlySpan<char> span = value.AsSpan();
            if (span.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase))
            {
                span = span["weapon_".Length..];
            }
            else if (span.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
            {
                span = span["item_".Length..];
            }

            return span.ToString().Replace('_', ' ').ToUpperInvariant();
        }

        private static bool IsTruthy(string value) =>
            value.Equals("1", StringComparison.Ordinal)
            || value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }
}
