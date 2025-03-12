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

            var payload = new
            {
                content = message
            };
            var jsonPayload = JsonSerializer.Serialize(payload);
            var httpContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(webhookUrl, httpContent);
            response.EnsureSuccessStatusCode();
        }

        private void SendDiscordMessageOnChallengeCompleted(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            if (player == null
                || !player.IsValid) return;
            if (Config.Discord.WebhookChallengeCompleted == "") return;
            // get title of challenge in discord language
            string challengeTitle = (challenge.Title.TryGetValue(Config.Discord.Language, out var discordTitle)
                ? discordTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value))
                .Replace("{total}", challenge.Amount.ToString("N0"))
                .Replace("{count}", challenge.Amount.ToString("N0"));
            // create a string localizer with the language "en" and get the localized string
            using (new WithTemporaryCulture(CultureInfo.GetCultureInfo(Config.Discord.Language)))
            {
                string message = Localizer["discord.webhook.challenge.completed"].Value
                    .Replace("{player}", player.PlayerName)
                    .Replace("{challenge}", challengeTitle);
                // send message if translation was found
                if (message != "discord.webhook.challenge.completed")
                    _ = SendDiscordWebhookMessage(Config.Discord.WebhookChallengeCompleted, message);
                else
                    DebugPrint($"Translation for discord.webhook.challenge.completed not found for language {Config.Discord.Language}");
            }
        }

        private void SendDiscordMessageOnNewSchedule(RunningChallengeSchedule schedule)
        {
            if (Config.Discord.WebhookNewSchedule == "") return;
            // get title of schedule in discord language
            var runningChallenges = schedule.Challenges
                .Where(kvp => kvp.Value.Visible)
                .ToList();
            string scheduleTitle = (schedule.Title.TryGetValue(Config.Discord.Language, out var discordTitle)
                ? discordTitle
                : (schedule.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : schedule.Title.First().Value))
                .Replace("{playerName}", "Player")
                .Replace("{total}", runningChallenges.Count.ToString("N0"))
                .Replace("{count}", "0");
            // create a string localizer with the language "en" and get the localized string
            using (new WithTemporaryCulture(CultureInfo.GetCultureInfo(Config.Discord.Language)))
            {
                // create message
                string message = Localizer["discord.webhook.schedule.new"].Value
                    .Replace("{schedule}", scheduleTitle)
                    .Replace("{startDate}", string.Format(CultureInfo.GetCultureInfo(Config.Discord.Language), "{0:yyyy-MM-dd HH:mm:ss}", schedule.StartDate))
                    .Replace("{endDate}", string.Format(CultureInfo.GetCultureInfo(Config.Discord.Language), "{0:yyyy-MM-dd HH:mm:ss}", schedule.EndDate));
                var challengeOrder = new List<KeyValuePair<string, ChallengesBlueprint>>();
                var visited = new HashSet<string>();
                // visit all challenges in the schedule
                void VisitChallenge(string challengeId, int level = 0)
                {
                    if (visited.Contains(challengeId))
                        return;

                    visited.Add(challengeId);

                    if (schedule.Challenges.TryGetValue(challengeId, out var challenge))
                    {
                        if (challenge.Dependencies != null)
                        {
                            foreach (var dependencyId in challenge.Dependencies)
                            {
                                VisitChallenge(dependencyId, level + 1);
                            }
                        }
                        challengeOrder.Add(new KeyValuePair<string, ChallengesBlueprint>(challengeId, challenge));
                    }
                }
                foreach (var kvp in runningChallenges)
                {
                    VisitChallenge(kvp.Key);
                }
                // group challenges by category
                var groupedChallenges = challengeOrder
                    .GroupBy(kvp => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(kvp.Key.Split(':')[0].Replace("_", " ").ToLower()))
                    .ToList();
                // create message
                foreach (var group in groupedChallenges)
                {
                    message += $"\n- {group.Key}";
                    int index = 1;
                    foreach (var kvp in group)
                    {
                        string challengeTitle = (kvp.Value.Title.TryGetValue(Config.Discord.Language, out var discordTitle2)
                            ? discordTitle2
                            : (kvp.Value.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle2)
                                ? serverTitle2
                                : kvp.Value.Title.First().Value))
                            .Replace("{total}", kvp.Value.Amount.ToString("N0"))
                            .Replace("{count}", "0");
                        message += $"\n  {index}. {challengeTitle}";
                        index++;
                    }
                }
                // send message if translation was found
                if (message != "discord.webhook.schedule.new")
                    _ = SendDiscordWebhookMessage(Config.Discord.WebhookChallengeCompleted, message);
                else
                    DebugPrint($"Translation for discord.webhook.schedule.new not found for language {Config.Discord.Language}");
            }
        }
    }
}