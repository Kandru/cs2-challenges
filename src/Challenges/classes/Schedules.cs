using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Configs;
using Challenges.Enums;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    /// <summary>Picks the first schedule whose UTC window contains now and resolves its challenges.</summary>
    public class Schedules : ClassesBlueprint
    {
        private const string DateFormat = "yyyy-MM-dd HH:mm:ss";

        public RunningSchedule? Current { get; private set; }

        /// <summary>True when the active schedule differs from the one last announced on Discord.</summary>
        public bool NewScheduleAnnouncementPending { get; private set; }

        public Schedules(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            Compute();
        }

        private Dictionary<string, ChallengeSchedule> Loaded =>
            (Dictionary<string, ChallengeSchedule>)_globalStates[GlobalStates.Schedules];

        private Dictionary<string, ChallengeDefinition> Challenges =>
            (Dictionary<string, ChallengeDefinition>)_globalStates[GlobalStates.Challenges];

        public void ClearAnnouncement() => NewScheduleAnnouncementPending = false;

        public static bool TryParseDate(string value, out DateTime utc) =>
            DateTime.TryParseExact(
                value,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out utc)
            || DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out utc);

        /// <summary>Strips a legacy <c>:*</c> suffix from a schedule challenge id.</summary>
        public static string ChallengeId(string rawId) =>
            rawId.EndsWith(":*", StringComparison.Ordinal) ? rawId[..^2] : rawId;

        public static string BuildKey(ChallengeSchedule schedule)
        {
            string firstTitle = schedule.Title.Count > 0 ? schedule.Title.First().Value : string.Empty;
            string input = $"{firstTitle}{schedule.StartDate}{schedule.EndDate}";
            return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
        }

        private void Compute()
        {
            Current = null;
            if (Loaded.Count == 0 || Challenges.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            foreach ((string scheduleId, ChallengeSchedule schedule) in Loaded)
            {
                if (!TryParseDate(schedule.StartDate, out DateTime start)
                    || !TryParseDate(schedule.EndDate, out DateTime end)
                    || start > now
                    || end < now)
                {
                    continue;
                }

                RunningSchedule running = new()
                {
                    ScheduleId = scheduleId,
                    Key = BuildKey(schedule),
                    Title = schedule.Title,
                    StartDate = schedule.StartDate,
                    EndDate = schedule.EndDate,
                };

                HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
                foreach (string rawId in schedule.Challenges)
                {
                    string id = ChallengeId(rawId);
                    if (!seen.Add(id))
                    {
                        continue;
                    }

                    if (Challenges.TryGetValue(id, out ChallengeDefinition? definition))
                    {
                        running.Challenges.Add(definition);
                    }
                    else
                    {
                        DebugPrint($"schedule {scheduleId} references unknown challenge {id}");
                    }
                }

                Current = running;
                DebugPrint($"schedule {scheduleId} running, {running.Challenges.Count} challenges");

                if (running.Key != GlobalConfig.TempData.CurrentScheduleKey)
                {
                    GlobalConfig.TempData.CurrentScheduleKey = running.Key;
                    GlobalConfig.Update();
                    NewScheduleAnnouncementPending = true;
                }

                break;
            }
        }
    }
}
