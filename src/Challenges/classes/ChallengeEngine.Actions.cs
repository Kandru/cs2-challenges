using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Utils;
using ChallengesShared.Events;

namespace Challenges.Classes
{
    public partial class ChallengeEngine
    {
        private enum ResetMode
        {
            Progress,
            Completed,
        }

        private void RunActions(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask owner,
            long now)
        {
            foreach (ChallengeAction action in owner.Actions)
            {
                switch (action.Type)
                {
                    case "task.reset_progress":
                        foreach (string id in action.Values)
                        {
                            ResetTask(player, state, schedule, challenge, owner, id, ResetMode.Progress);
                        }
                        break;
                    case "task.reset_completed":
                        foreach (string id in action.Values)
                        {
                            ResetTask(player, state, schedule, challenge, owner, id, ResetMode.Completed);
                        }
                        break;
                    case "task.mark_completed":
                        foreach (string id in action.Values)
                        {
                            MarkCompleted(player, state, schedule, challenge, owner, id, now);
                        }
                        break;
                    case "notify.player.progress.rule_broken":
                        if (action.Values.Any(id => HasProgress(state, schedule, challenge, id)))
                        {
                            Notes.NotifyRuleBroken(player, challenge, owner);
                        }
                        break;
                    case "notify.player.completed.rule_broken":
                        if (action.Values.Any(id => HasCompleted(state, schedule, challenge, id)))
                        {
                            Notes.NotifyRuleBroken(player, challenge, owner);
                        }
                        break;
                    case "server.runcommand" when action.Values.Count >= 1:
                        Server.ExecuteCommand(action.Values[0]
                            .Replace("{steamid}", player.NetworkIDString)
                            .Replace("{userid}", player.UserId?.ToString() ?? string.Empty)
                            .Replace("{index}", player.Index.ToString()));
                        break;
                    default:
                        if (GlobalConfig.Debug)
                        {
                            DebugPrint(
                                $"{player.PlayerName} {action.Type} {challenge.Id}/{owner.Id} skip unknown");
                        }

                        break;
                }
            }
        }

        private void ResetTask(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask owner,
            string taskId,
            ResetMode mode)
        {
            ChallengeTask? target = FindTask(challenge, taskId);
            if (target == null
                || ChallengeProgress.GetProgress(state, schedule.Key, challenge.Id, taskId) is not { } progress)
            {
                return;
            }

            bool complete = progress.Amount >= Math.Max(1, target.Amount);
            if (mode == ResetMode.Progress ? complete : !complete)
            {
                return;
            }

            state.Challenges[schedule.Key][challenge.Id].Remove(taskId);
            if (GlobalConfig.Debug)
            {
                string action = mode == ResetMode.Progress ? "task.reset_progress" : "task.reset_completed";
                DebugPrint($"{player.PlayerName} {action} {challenge.Id}/{owner.Id} reset {taskId}");
            }

            if (target.Visible && target.Id != owner.Id)
            {
                Notes.NotifyTaskReset(player, challenge, target);
            }
        }

        private void MarkCompleted(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            ChallengeDefinition challenge,
            ChallengeTask owner,
            string taskId,
            long now)
        {
            ChallengeTask? target = FindTask(challenge, taskId);
            if (target == null)
            {
                return;
            }

            TaskProgress progress = GetOrCreateProgress(state, schedule.Key, challenge.Id, taskId);
            progress.Amount = Math.Max(progress.Amount, Math.Max(1, target.Amount));
            progress.LastUpdate = now;
            if (GlobalConfig.Debug)
            {
                DebugPrint($"{player.PlayerName} task.mark_completed {challenge.Id}/{owner.Id} mark {taskId}");
            }
        }

        private static bool HasProgress(PlayerState state, RunningSchedule schedule, ChallengeDefinition challenge, string taskId) =>
            ChallengeProgress.GetTaskAmount(state, schedule.Key, challenge.Id, taskId) > 0;

        private static bool HasCompleted(PlayerState state, RunningSchedule schedule, ChallengeDefinition challenge, string taskId) =>
            FindTask(challenge, taskId) is { } task
            && ChallengeProgress.IsTaskComplete(state, schedule.Key, challenge.Id, task);

        private static ChallengeTask? FindTask(ChallengeDefinition challenge, string taskId) =>
            challenge.TaskById.GetValueOrDefault(taskId);

        private void TriggerProgress(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task, int amount)
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
            MergeData(eventData, task.Data);
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
            MergeData(eventData, task.Data);
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
            MergeData(eventData, challenge.Data);
            CustomEventsSender.Instance?.TriggerEvent(new PlayerCompletedChallengeEvent(userId, eventData));
        }

        private static Dictionary<string, string> TitleSource(ChallengeDefinition challenge, ChallengeTask task) =>
            task.Title.Count > 0 ? task.Title : challenge.Title;

        private static void MergeData(
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
