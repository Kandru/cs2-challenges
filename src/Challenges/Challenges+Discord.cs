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
    }
}