using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using System.Text.Json;
using System.Text;
using System.Globalization;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private static async Task SendDiscordWebhookMessage(string webhookUrl, string message)
        {
            using var httpClient = new HttpClient();

            var payload = new { content = message };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, httpContent);
            response.EnsureSuccessStatusCode();
        }

        private void SendDiscordMessageOnChallengeCompleted(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            if (player == null || !player.IsValid || string.IsNullOrEmpty(Config.Discord.WebhookChallengeCompleted)) return;

            string challengeTitle = GetLocalizedChallengeTitle(challenge);
            using (new WithTemporaryCulture(CultureInfo.GetCultureInfo(Config.Discord.Language)))
            {
                string message = Localizer["discord.webhook.challenge.completed"].Value
                    .Replace("{player}", player.PlayerName)
                    .Replace("{challenge}", challengeTitle);

                if (message != "discord.webhook.challenge.completed")
                    _ = SendDiscordWebhookMessage(Config.Discord.WebhookChallengeCompleted, message);
                else
                    DebugPrint($"Translation for discord.webhook.challenge.completed not found for language {Config.Discord.Language}");
            }
        }

        private void SendDiscordMessageOnNewSchedule(RunningChallengeSchedule schedule)
        {
            if (string.IsNullOrEmpty(Config.Discord.WebhookNewSchedule)) return;

            var runningChallenges = schedule.Challenges.Where(kvp => kvp.Value.Visible).ToList();
            string scheduleTitle = GetLocalizedScheduleTitle(schedule, runningChallenges.Count);

            using (new WithTemporaryCulture(CultureInfo.GetCultureInfo(Config.Discord.Language)))
            {
                string message = Localizer["discord.webhook.schedule.new"].Value
                    .Replace("{schedule}", scheduleTitle)
                    .Replace("{startDate}", FormatDate(ParseDate(schedule.StartDate)))
                    .Replace("{endDate}", FormatDate(ParseDate(schedule.EndDate)));

                var challengeOrder = GetChallengeOrder(schedule, runningChallenges);
                var groupedChallenges = GroupChallengesByCategory(challengeOrder);

                foreach (var group in groupedChallenges)
                {
                    message += $"\n- {group.Key}";
                    int index = 1;
                    foreach (var kvp in group)
                    {
                        string challengeTitle = GetLocalizedChallengeTitle(kvp.Value);
                        message += $"\n  {index}. {challengeTitle}";
                        index++;
                    }
                }

                if (message != "discord.webhook.schedule.new")
                    _ = SendDiscordWebhookMessage(Config.Discord.WebhookNewSchedule, message);
                else
                    DebugPrint($"Translation for discord.webhook.schedule.new not found for language {Config.Discord.Language}");
            }
        }

        private string GetLocalizedChallengeTitle(ChallengesBlueprint challenge)
        {
            return (challenge.Title.TryGetValue(Config.Discord.Language, out var discordTitle)
                ? discordTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value))
                .Replace("{total}", challenge.Amount.ToString("N0"))
                .Replace("{count}", challenge.Amount.ToString("N0"));
        }

        private string GetLocalizedScheduleTitle(RunningChallengeSchedule schedule, int challengeCount)
        {
            return (schedule.Title.TryGetValue(Config.Discord.Language, out var discordTitle)
                ? discordTitle
                : (schedule.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : schedule.Title.First().Value))
                .Replace("{playerName}", "Player")
                .Replace("{total}", challengeCount.ToString("N0"))
                .Replace("{count}", "0");
        }
        private DateTime? ParseDate(string date)
        {
            if (DateTime.TryParseExact(date, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }
            return null;
        }
        private string FormatDate(DateTime? date)
        {
            return string.Format(CultureInfo.GetCultureInfo(Config.Discord.Language), "{0:yyyy-MM-dd HH:mm:ss}", date);
        }

        private List<KeyValuePair<string, ChallengesBlueprint>> GetChallengeOrder(RunningChallengeSchedule schedule, List<KeyValuePair<string, ChallengesBlueprint>> runningChallenges)
        {
            var challengeOrder = new List<KeyValuePair<string, ChallengesBlueprint>>();
            var visited = new HashSet<string>();

            void VisitChallenge(string challengeId)
            {
                if (visited.Contains(challengeId)) return;

                visited.Add(challengeId);

                if (schedule.Challenges.TryGetValue(challengeId, out var challenge))
                {
                    if (challenge.Dependencies != null)
                    {
                        foreach (var dependencyId in challenge.Dependencies)
                        {
                            VisitChallenge(dependencyId);
                        }
                    }
                    challengeOrder.Add(new KeyValuePair<string, ChallengesBlueprint>(challengeId, challenge));
                }
            }

            foreach (var kvp in runningChallenges)
            {
                VisitChallenge(kvp.Key);
            }

            return challengeOrder;
        }

        private IEnumerable<IGrouping<string, KeyValuePair<string, ChallengesBlueprint>>> GroupChallengesByCategory(List<KeyValuePair<string, ChallengesBlueprint>> challengeOrder)
        {
            return challengeOrder.GroupBy(kvp => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(kvp.Key.Split(':')[0].Replace("_", " ").ToLower()));
        }
    }
}