using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Challenges.Configs
{
    public static class MenuCommandNames
    {
        public static string? ShortestChatCommand(IEnumerable<string> entries, string prefix)
        {
            string? shortest = null;
            foreach (string entry in entries)
            {
                string? name = DisplayName(entry);
                if (name != null && (shortest == null || name.Length < shortest.Length))
                {
                    shortest = name;
                }
            }

            return shortest == null ? null : prefix + shortest;
        }

        public static string? DisplayName(string entry)
        {
            string name = entry.Trim().TrimStart('!', '/', '.').Trim();
            if (name.StartsWith("css_", StringComparison.OrdinalIgnoreCase))
            {
                name = name[4..];
            }

            return name.Length == 0 ? null : name;
        }
    }

    public class PluginConfigGui
    {
        [JsonPropertyName("show_on_round_start")] public bool ShowOnRoundStart { get; set; } = true;
        [JsonPropertyName("show_on_progress")] public bool ShowOnProgress { get; set; } = true;
        [JsonPropertyName("progress_duration")] public float ProgressDuration { get; set; } = 5f;
        [JsonPropertyName("tracker_rows")] public int TrackerRows { get; set; } = 3;
        [JsonPropertyName("menu_page_size")] public int MenuPageSize { get; set; } = 5;
        /// <summary>Accent theme name (<c>gui.theme</c>); see <c>HudTheme</c> / <c>tools/hud_themes.py</c>.</summary>
        [JsonPropertyName("theme")] public string Theme { get; set; } = "gold";
    }

    public class PluginConfigNotifications
    {
        [JsonPropertyName("notify_player_on_challenge_progress")] public bool NotifyPlayerOnChallengeProgress { get; set; } = true;
        [JsonPropertyName("notify_player_on_challenge_complete")] public bool NotifyPlayerOnChallengeComplete { get; set; } = true;
        [JsonPropertyName("notify_other_on_challenge_complete")] public bool NotifyOtherOnChallengeComplete { get; set; } = true;
        [JsonPropertyName("notification_sound_on_challenge_progress")] public string ChallengeProgressSound { get; set; } = "";
        [JsonPropertyName("notification_sound_on_challenge_complete")] public string ChallengeCompleteSound { get; set; } = "sounds/ui/xp_levelup.vsnd";
        [JsonPropertyName("notification_sound_on_action_rule_broken")] public string ChallengeRuleBrokenSound { get; set; } = "sounds/ui/xp_rankdown_02.vsnd";
    }

    public class PluginConfigDiscord
    {
        [JsonPropertyName("language")] public string Language { get; set; } = "en";
        [JsonPropertyName("webhook_on_challenge_completed")] public string WebhookChallengeCompleted { get; set; } = "";
        [JsonPropertyName("webhook_on_new_schedule")] public string WebhookNewSchedule { get; set; } = "";
    }

    public class TempDataConfig
    {
        [JsonPropertyName("current_schedule_key")] public string CurrentScheduleKey { get; set; } = "";
    }

    public class PluginConfig : BasePluginConfig
    {
        [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
        [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
        [JsonPropertyName("menu_commands")] public List<string> MenuCommands { get; set; } = ["c", "challenges"];
        [JsonPropertyName("command_prefix")] public string CommandPrefix { get; set; } = "!";
        [JsonPropertyName("gui")] public PluginConfigGui Gui { get; set; } = new();
        [JsonPropertyName("notifications")] public PluginConfigNotifications Notifications { get; set; } = new();
        [JsonPropertyName("discord")] public PluginConfigDiscord Discord { get; set; } = new();
        [JsonPropertyName("temp_data")] public TempDataConfig TempData { get; set; } = new();
    }
}
