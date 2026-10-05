using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace Challenges.Configs
{
    public class PluginConfigGui
    {
        [JsonPropertyName("show_on_round_start")] public bool ShowOnRoundStart { get; set; } = true;
        [JsonPropertyName("show_on_progress")] public bool ShowOnProgress { get; set; } = true;
        [JsonPropertyName("progress_duration")] public float ProgressDuration { get; set; } = 5f;
        [JsonPropertyName("tracker_rows")] public int TrackerRows { get; set; } = 4;
        [JsonPropertyName("menu_page_size")] public int MenuPageSize { get; set; } = 8;
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
        [JsonPropertyName("allow_bots")] public bool AllowBots { get; set; } = false;
        [JsonPropertyName("gui")] public PluginConfigGui Gui { get; set; } = new();
        [JsonPropertyName("notifications")] public PluginConfigNotifications Notifications { get; set; } = new();
        [JsonPropertyName("discord")] public PluginConfigDiscord Discord { get; set; } = new();
        [JsonPropertyName("temp_data")] public TempDataConfig TempData { get; set; } = new();
    }
}
