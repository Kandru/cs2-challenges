using Challenges.Configs;

namespace Challenges.Utils
{
    /// <summary>Read-only progress math on top of <see cref="PlayerState.Challenges"/>.</summary>
    public static class ChallengeProgress
    {
        public static TaskProgress? GetProgress(PlayerState state, string scheduleKey, string challengeId, string taskId) =>
            state.Challenges.TryGetValue(scheduleKey, out var challenges)
            && challenges.TryGetValue(challengeId, out var tasks)
            && tasks.TryGetValue(taskId, out TaskProgress? progress)
                ? progress
                : null;

        public static int GetTaskAmount(PlayerState state, string scheduleKey, string challengeId, string taskId) =>
            GetProgress(state, scheduleKey, challengeId, taskId)?.Amount ?? 0;

        public static bool IsTaskComplete(PlayerState state, string scheduleKey, string challengeId, ChallengeTask task) =>
            GetTaskAmount(state, scheduleKey, challengeId, task.Id) >= Math.Max(1, task.Amount);

        public static int CountVisibleTasks(ChallengeDefinition challenge)
        {
            int count = 0;
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (task.Visible)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Visible tasks in blueprint order. Allocation is intentional for HUD painting.</summary>
        public static List<ChallengeTask> VisibleTasks(ChallengeDefinition challenge)
        {
            List<ChallengeTask> list = [];
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (task.Visible)
                {
                    list.Add(task);
                }
            }

            return list;
        }

        /// <summary>Solved when it has at least one visible task and every visible task is complete.</summary>
        public static bool IsChallengeSolved(PlayerState state, string scheduleKey, ChallengeDefinition challenge)
        {
            bool any = false;
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                any = true;
                if (!IsTaskComplete(state, scheduleKey, challenge.Id, task))
                {
                    return false;
                }
            }

            return any;
        }

        /// <summary>Visible task progress over visible task amounts, 0-100.</summary>
        public static int GetChallengePercent(PlayerState state, string scheduleKey, ChallengeDefinition challenge)
        {
            long done = 0;
            long total = 0;
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                int amount = Math.Max(1, task.Amount);
                total += amount;
                done += Math.Min(amount, GetTaskAmount(state, scheduleKey, challenge.Id, task.Id));
            }

            return total == 0 ? 0 : (int)Math.Clamp(done * 100 / total, 0, 100);
        }

        public static int CountSolvedInSchedule(PlayerState state, RunningSchedule schedule)
        {
            int count = 0;
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (IsChallengeSolved(state, schedule.Key, challenge))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// First incomplete visible task whose <c>requires</c> are already complete, or the last visible
        /// task when every visible task is done.
        /// </summary>
        public static ChallengeTask? GetCurrentTask(PlayerState state, string scheduleKey, ChallengeDefinition challenge)
        {
            ChallengeTask? last = null;
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                last = task;
                if (!IsTaskComplete(state, scheduleKey, challenge.Id, task)
                    && AreRequirementsMet(state, scheduleKey, challenge, task))
                {
                    return task;
                }
            }

            return last;
        }

        public static bool AreRequirementsMet(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge,
            ChallengeTask task)
        {
            foreach (string requiredId in task.Requires)
            {
                if (!challenge.TaskById.TryGetValue(requiredId, out ChallengeTask? required)
                    || !IsTaskComplete(state, scheduleKey, challenge.Id, required))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Clamped progress count for a task (0 when <paramref name="scheduleKey"/> is null).</summary>
        public static int GetTaskCount(PlayerState state, string? scheduleKey, string challengeId, ChallengeTask task)
        {
            int amount = Math.Max(1, task.Amount);
            if (scheduleKey is null)
            {
                return 0;
            }

            return Math.Min(amount, GetTaskAmount(state, scheduleKey, challengeId, task.Id));
        }
    }
}
