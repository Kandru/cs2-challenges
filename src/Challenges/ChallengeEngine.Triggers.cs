using CounterStrikeSharp.API.Core;
using ChallengesShared.Events;

namespace Challenges
{
    public partial class ChallengeEngine
    {
        private void TriggerProgress(
            CCSPlayerController player,
            ChallengeDefinition challenge,
            ChallengeTask task,
            int amount)
        {
            TriggerTaskEvent(
                player,
                challenge,
                task,
                Titles.Expand(Titles.Resolve(Titles.TaskOrChallenge(challenge, task), player), amount, task.Amount),
                amounts: ("current_amount", amount.ToString(), "total_amount", task.Amount.ToString()),
                completed: false);
        }

        private void TriggerCompleted(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            TriggerTaskEvent(
                player,
                challenge,
                task,
                Titles.Expand(Titles.Resolve(Titles.TaskOrChallenge(challenge, task), player), task.Amount, task.Amount),
                amounts: ("amount", task.Amount.ToString(), null, null),
                completed: true);
        }

        private void TriggerTaskEvent(
            CCSPlayerController player,
            ChallengeDefinition challenge,
            ChallengeTask task,
            string title,
            (string Key1, string Value1, string? Key2, string? Value2) amounts,
            bool completed)
        {
            if (player.UserId is not int userId)
            {
                return;
            }

            Dictionary<string, string> info = new()
            {
                ["title"] = title,
                ["type"] = task.Type,
                ["challenge"] = challenge.Id,
                ["task"] = task.Id,
                ["cooldown"] = task.Cooldown.ToString(),
                [amounts.Key1] = amounts.Value1,
            };
            if (amounts.Key2 != null && amounts.Value2 != null)
            {
                info[amounts.Key2] = amounts.Value2;
            }

            Dictionary<string, Dictionary<string, string>> eventData = new() { ["info"] = info };
            MergePluginData(eventData, task.Data);
            IChallengesEvent ev = completed
                ? new PlayerCompletedChallengeEvent(userId, eventData)
                : new PlayerProgressedChallengeEvent(userId, eventData);
            CustomEventsSender.Instance?.TriggerEvent(ev);
        }

        private void TriggerChallengeSolved(CCSPlayerController player, ChallengeDefinition challenge)
        {
            if (player.UserId is not int userId)
            {
                return;
            }

            Dictionary<string, Dictionary<string, string>> eventData = new()
            {
                ["info"] = new Dictionary<string, string>
                {
                    ["title"] = Titles.Resolve(challenge.Title, player),
                    ["type"] = string.Empty,
                    ["challenge"] = challenge.Id,
                    ["task"] = string.Empty,
                    ["amount"] = ChallengeProgress.CountVisibleTasks(challenge).ToString(),
                    ["cooldown"] = "0",
                },
            };
            MergePluginData(eventData, challenge.Data);
            CustomEventsSender.Instance?.TriggerEvent(new PlayerCompletedChallengeEvent(userId, eventData));
        }

        private static void MergePluginData(
            Dictionary<string, Dictionary<string, string>> target,
            Dictionary<string, Dictionary<string, string>> source)
        {
            foreach ((string group, Dictionary<string, string> values) in source)
            {
                if (group != "info")
                {
                    target[group] = values;
                }
            }
        }
    }
}
