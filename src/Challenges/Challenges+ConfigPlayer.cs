using System.Text.Json.Serialization;

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
        [JsonPropertyName("amount")] public int Amount { get; set; } = 0;
        [JsonPropertyName("last_update")] public long LastUpdate { get; set; } = 0;
    }

    public class PlayerConfig
    {
        [JsonPropertyName("username")] public string Username { get; set; } = "";
        [JsonPropertyName("clantag")] public string ClanTag { get; set; } = "";
        [JsonPropertyName("challenges")] public Dictionary<string, Dictionary<string, PlayerConfigChallenges>> Challenges { get; set; } = [];
        [JsonPropertyName("settings")] public PlayerConfigSettings Settings { get; set; } = new();
    }
}
