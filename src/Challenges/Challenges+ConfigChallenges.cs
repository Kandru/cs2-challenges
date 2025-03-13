using YamlDotNet.Serialization;

namespace Challenges
{
    public class ChallengesSchedule
    {
        [YamlMember(Alias = "title")] public Dictionary<string, string> Title { get; set; } = [];
        [YamlMember(Alias = "date_start")] public string StartDate { get; set; } = "2025-01-01 00:00:00";
        [YamlMember(Alias = "date_end")] public string EndDate { get; set; } = "2025-02-01 00:00:00";
        [YamlMember(Alias = "challenges")] public List<string> Challenges { get; set; } = [];
    }

    public class ChallengesBlueprintRules
    {
        [YamlMember(Alias = "key")] public string Key { get; set; } = "";
        [YamlMember(Alias = "operator")] public string Operator { get; set; } = "";
        [YamlMember(Alias = "value")] public string Value { get; set; } = "";
    }

    public class ChallengesBlueprintActions
    {
        [YamlMember(Alias = "type")] public string Type { get; set; } = "";
        [YamlMember(Alias = "values")] public List<string> Values { get; set; } = [];
    }

    public class ChallengesBlueprint
    {
        [YamlMember(Alias = "title")] public Dictionary<string, string> Title { get; set; } = [];
        public string Key { get; set; } = "";
        [YamlMember(Alias = "type")] public string Type { get; set; } = "";
        [YamlMember(Alias = "amount")] public int Amount { get; set; } = 0;
        [YamlMember(Alias = "cooldown")] public int Cooldown { get; set; } = 0;
        [YamlMember(Alias = "is_visible")] public bool Visible { get; set; } = true;
        [YamlMember(Alias = "announce_progress")] public bool AnnounceProgress { get; set; } = true;
        [YamlMember(Alias = "announce_completion")] public bool AnnounceCompletion { get; set; } = true;
        [YamlMember(Alias = "data", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)] public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new Dictionary<string, Dictionary<string, string>>();
        [YamlMember(Alias = "rules", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)] public List<ChallengesBlueprintRules> Rules { get; set; } = [];
        [YamlMember(Alias = "actions", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)] public List<ChallengesBlueprintActions> Actions { get; set; } = [];
        [YamlMember(Alias = "dependencies", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)] public List<string> Dependencies { get; set; } = [];
    }

    public class ChallengesConfig
    {
        [YamlMember(Alias = "schedule")] public Dictionary<string, ChallengesSchedule> Schedules { get; set; } = [];
        [YamlMember(Alias = "blueprints")] public Dictionary<string, ChallengesBlueprint> Blueprints { get; set; } = [];
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
