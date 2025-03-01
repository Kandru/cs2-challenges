﻿using System.Text.Json.Serialization;

namespace Challenges
{
    public class PlayerConfigSettingsChallenges
    {
        [JsonPropertyName("show_always")] public bool ShowAlways { get; set; } = true;
    }

    public class PlayerConfigSettings
    {
        [JsonPropertyName("challenges")] public PlayerConfigSettingsChallenges Challenges { get; set; } = new();
    }

    public class PlayerConfigChallenges
    {
        [JsonPropertyName("schedule_key")] public string ScheduleKey { get; set; } = "";
        [JsonPropertyName("amount")] public int Amount { get; set; } = 0;
    }

    public class PlayerConfig
    {
        [JsonPropertyName("username")] public string Username { get; set; } = "";
        [JsonPropertyName("clantag")] public string ClanTag { get; set; } = "";
        [JsonPropertyName("challenges")] public Dictionary<string, PlayerConfigChallenges> Challenges { get; set; } = [];
        [JsonPropertyName("settings")] public PlayerConfigSettings Settings { get; set; } = new();
    }
}
