using System.Globalization;
using System.Text;
using System.Text.Json;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    /// <summary>Chat, sounds and Discord webhooks.</summary>
    public class Notifications : Blueprint
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(15) };

        public Notifications(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            Schedules schedules = GetClass<Schedules>();
            if (schedules.NewScheduleAnnouncementPending && schedules.Current is { } running)
            {
                schedules.ClearAnnouncement();
                DiscordNewSchedule(running);
            }
        }

        public void NotifyProgress(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task, int amount)
        {
            if (!player.IsValid || !GlobalConfig.Notifications.NotifyPlayerOnChallengeProgress || !task.AnnounceProgress)
            {
                return;
            }

            PlaySound(player, GlobalConfig.Notifications.ChallengeProgressSound);
            string message = LocalizerExtensions.ForPlayer(Localizer, player, "challenges.progress")
                .Replace("{challenge}", TaskTitle(player, challenge, task, amount))
                .Replace("{total}", task.Amount.ToString())
                .Replace("{count}", amount.ToString());
            player.PrintToChat(message);
        }

        public void NotifyCompletion(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid || !task.AnnounceCompletion)
            {
                return;
            }

            foreach (CCSPlayerController entry in Players.GetHumans())
            {
                bool self = entry == player;
                if ((self && !GlobalConfig.Notifications.NotifyPlayerOnChallengeComplete)
                    || (!self && !GlobalConfig.Notifications.NotifyOtherOnChallengeComplete))
                {
                    continue;
                }

                if (self)
                {
                    PlaySound(player, GlobalConfig.Notifications.ChallengeCompleteSound);
                }

                string key = self ? "challenges.completed.user" : "challenges.completed.other";
                entry.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, entry, key)
                    .Replace("{challenge}", TaskTitle(entry, challenge, task, task.Amount))
                    .Replace("{player}", player.PlayerName)
                    .Replace("{total}", task.Amount.ToString())
                    .Replace("{count}", task.Amount.ToString()));
            }
        }

        public void NotifyRuleBroken(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid)
            {
                return;
            }

            PlaySound(player, GlobalConfig.Notifications.ChallengeRuleBrokenSound);
            string title = TaskTitle(player, challenge, task, 0);
            player.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, player, "challenges.rule.broken"));
            player.PrintToChat(title);
            player.PrintToCenterAlert(title);
        }

        public void NotifyTaskReset(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            if (!player.IsValid)
            {
                return;
            }

            player.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, player, "challenges.deleted")
                .Replace("{challenge}", TaskTitle(player, challenge, task, 0)));
        }

        public void DiscordChallengeCompleted(CCSPlayerController player, ChallengeDefinition challenge, ChallengeTask task)
        {
            string url = GlobalConfig.Discord.WebhookChallengeCompleted;
            if (string.IsNullOrEmpty(url) || !player.IsValid || !task.Visible || !task.AnnounceCompletion)
            {
                return;
            }

            string title = Titles.Expand(
                DiscordTitle(task.Title.Count > 0 ? task.Title : challenge.Title),
                task.Amount,
                task.Amount);
            string message = DiscordText("discord.webhook.challenge.completed")
                .Replace("{player}", player.PlayerName)
                .Replace("{challenge}", title);
            _ = SendWebhook(url, message);
        }

        public void DiscordNewSchedule(RunningSchedule schedule)
        {
            string url = GlobalConfig.Discord.WebhookNewSchedule;
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            StringBuilder message = new(DiscordText("discord.webhook.schedule.new")
                .Replace("{schedule}", Titles.Expand(DiscordTitle(schedule.Title), 0, schedule.Challenges.Count)
                    .Replace("{playerName}", "Player"))
                .Replace("{startDate}", FormatDate(schedule.StartDate))
                .Replace("{endDate}", FormatDate(schedule.EndDate)));

            int index = 1;
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                message.Append($"\n  {index}. {DiscordTitle(challenge.Title)}");
                index++;
            }

            _ = SendWebhook(url, message.ToString());
        }

        private string TaskTitle(CCSPlayerController viewer, ChallengeDefinition challenge, ChallengeTask task, int count)
        {
            string raw = Titles.Resolve(task.Title.Count > 0 ? task.Title : challenge.Title, viewer);
            return Titles.Expand(raw, count, task.Amount);
        }

        private string DiscordTitle(Dictionary<string, string> titles)
        {
            if (titles.Count == 0)
            {
                return string.Empty;
            }

            return titles.TryGetValue(GlobalConfig.Discord.Language, out string? value) && !string.IsNullOrEmpty(value)
                ? value
                : titles.Values.First();
        }

        private string DiscordText(string key)
        {
            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(GlobalConfig.Discord.Language);
            }
            catch (CultureNotFoundException)
            {
                culture = CultureInfo.InvariantCulture;
            }

            using (new WithTemporaryCulture(culture))
            {
                string text = Localizer[key].Value;
                if (text == key)
                {
                    DebugPrint($"Translation for {key} not found for language {GlobalConfig.Discord.Language}");
                }
                return text;
            }
        }

        private string FormatDate(string value) =>
            Schedules.TryParseDate(value, out DateTime date)
                ? date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + " UTC"
                : value;

        private static void PlaySound(CCSPlayerController player, string sound)
        {
            if (string.IsNullOrEmpty(sound) || !player.IsValid)
            {
                return;
            }

            if (sound.StartsWith("sounds/", StringComparison.Ordinal))
            {
                player.ExecuteClientCommand($"play {sound}");
                return;
            }

            RecipientFilter filter = [player];
            player.EmitSound(sound, filter);
        }

        private async Task SendWebhook(string webhookUrl, string message)
        {
            try
            {
                string json = JsonSerializer.Serialize(new { content = message });
                using StringContent content = new(json, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await Http.PostAsync(webhookUrl, content).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges] Discord webhook failed: {ex.Message}");
            }
        }
    }
}
