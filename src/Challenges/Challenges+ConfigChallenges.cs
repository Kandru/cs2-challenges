﻿using System.Text.Json.Serialization;

namespace Challenges
{
    public class ChallengesSchedule
    {
        [JsonPropertyName("title")] public Dictionary<string, string> Title { get; set; } = [];
        [JsonPropertyName("date_start")] public string StartDate { get; set; } = "2025-01-01 00:00:00";
        [JsonPropertyName("date_end")] public string EndDate { get; set; } = "2025-02-01 00:00:00";
        [JsonPropertyName("challenges")] public List<string> Challenges { get; set; } = [];
    }

    public class ChallengesBlueprintRules
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
        [JsonPropertyName("operator")] public string Operator { get; set; } = "";
        [JsonPropertyName("value")] public string Value { get; set; } = "";
    }

    public class ChallengesBlueprintActions
    {
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("values")] public List<string> Values { get; set; } = [];
    }

    public class ChallengesBlueprint
    {
        [JsonPropertyName("title")] public Dictionary<string, string> Title { get; set; } = [];
        public string Key { get; set; } = "";
        [JsonPropertyName("type")] public string Type { get; set; } = "";
        [JsonPropertyName("amount")] public int Amount { get; set; } = 0;
        [JsonPropertyName("cooldown")] public int Cooldown { get; set; } = 0;
        [JsonPropertyName("is_visible")] public bool Visible { get; set; } = true;
        [JsonPropertyName("announce_progress")] public bool AnnounceProgress { get; set; } = true;
        [JsonPropertyName("announce_completion")] public bool AnnounceCompletion { get; set; } = true;
        [JsonPropertyName("data")] public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        [JsonPropertyName("rules")] public List<ChallengesBlueprintRules> Rules { get; set; } = [];
        [JsonPropertyName("actions")] public List<ChallengesBlueprintActions> Actions { get; set; } = [];
        [JsonPropertyName("dependencies")] public List<string> Dependencies { get; set; } = [];
    }

    public class ChallengesConfig
    {
        [JsonPropertyName("schedule")] public Dictionary<string, ChallengesSchedule> Schedules { get; set; } = [];
        [JsonPropertyName("blueprints")] public Dictionary<string, ChallengesBlueprint> Blueprints { get; set; } = [];
    }

    public class RunningChallengeSchedule
    {
        public Dictionary<string, string> Title { get; set; } = [];
        public string Key { get; set; } = "";
        public string StartDate { get; set; } = "2025-01-01 00:00:00";
        public string EndDate { get; set; } = "2025-02-01 00:00:00";
        public Dictionary<string, ChallengesBlueprint> Challenges { get; set; } = [];
    }
}
