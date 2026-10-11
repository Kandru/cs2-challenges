using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Utils;
using ChallengesShared.Events;

namespace Challenges.Classes
{
    public partial class ChallengeEngine
    {
        private void TriggerProgress(
            CCSPlayerController player,
            ChallengeDefinition challenge,
            ChallengeTask task,
            int amount)
        {
            if (player.UserId is not int userId)
            {
                return;
            }

            Dictionary<string, Dictionary<string, string>> eventData = new()
            {
                ["info"] = new Dictionary<string, string>
                {
                    ["title"] = Titles.Expand(Titles.Resolve(TitleSource(challenge, task), player), amount, task.Amount),
                    ["type"] = task.Type,
                    ["challenge"] = challenge.Id,
                    ["task"] = task.Id,
                    ["current_amount"] = amount.ToString(),
                    ["total_amount"] = task.Amount.ToString(),
                    ["cooldown"] = task.Cooldown.ToString(),
                },
            };
            MergePluginData(eventData, task.Data);
            CustomEventsSender.Instance?.TriggerEvent(new PlayerProgressedChallengeEvent(userId, eventData));
        }

        private void TriggerCompleted(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (player.UserId is not int userId)
            {
                return;
            }

            Dictionary<string, Dictionary<string, string>> eventData = new()
            {
                ["info"] = new Dictionary<string, string>
                {
                    ["title"] = Titles.Expand(Titles.Resolve(TitleSource(challenge, task), player), task.Amount, task.Amount),
                    ["type"] = task.Type,
                    ["challenge"] = challenge.Id,
                    ["task"] = task.Id,
                    ["amount"] = task.Amount.ToString(),
                    ["cooldown"] = task.Cooldown.ToString(),
                },
            };
            MergePluginData(eventData, task.Data);
            CustomEventsSender.Instance?.TriggerEvent(new PlayerCompletedChallengeEvent(userId, eventData));
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

        private static Dictionary<string, string> TitleSource(ChallengeDefinition challenge, ChallengeTask task) =>
            task.Title.Count > 0 ? task.Title : challenge.Title;

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
