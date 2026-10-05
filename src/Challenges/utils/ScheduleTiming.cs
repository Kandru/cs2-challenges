using Challenges.Classes;
using Challenges.Configs;

namespace Challenges.Utils
{
    /// <summary>
    /// Resolves whether a challenge is in the running schedule and which UTC instant
    /// drives its relative "when" phrase (end while active, next start otherwise).
    /// </summary>
    public static class ScheduleTiming
    {
        public readonly record struct Info(bool IsActive, DateTime? Target);

        public static Info ForChallenge(
            ChallengeDefinition challenge,
            RunningSchedule? current,
            IReadOnlyDictionary<string, ChallengeSchedule> schedules,
            DateTime now)
        {
            if (current != null
                && current.Challenges.Contains(challenge)
                && Schedules.TryParseDate(current.EndDate, out DateTime end)
                && end > now)
            {
                return new Info(true, end);
            }

            DateTime? nextStart = null;
            foreach (ChallengeSchedule schedule in schedules.Values)
            {
                if (!Contains(schedule, challenge.Id)
                    || !Schedules.TryParseDate(schedule.StartDate, out DateTime start)
                    || start <= now)
                {
                    continue;
                }

                if (nextStart is null || start < nextStart)
                {
                    nextStart = start;
                }
            }

            return new Info(false, nextStart);
        }

        private static bool Contains(ChallengeSchedule schedule, string challengeId)
        {
            foreach (string rawId in schedule.Challenges)
            {
                if (string.Equals(Schedules.ChallengeId(rawId), challengeId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
