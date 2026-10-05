using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace Challenges.Configs
{
    public sealed class ChallengeDefinition
    {
        [YamlMember(Alias = "title")]
        public Dictionary<string, string> Title { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        [YamlMember(Alias = "data", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new();

        [YamlMember(Alias = "tasks")]
        public List<ChallengeTask> Tasks { get; set; } = [];

        [JsonIgnore]
        [YamlIgnore]
        public string Id { get; set; } = "";

        [JsonIgnore]
        [YamlIgnore]
        public Dictionary<string, ChallengeTask> TaskById { get; } = new(StringComparer.Ordinal);

        public void BuildTaskIndex()
        {
            TaskById.Clear();
            foreach (ChallengeTask task in Tasks)
            {
                TaskById.TryAdd(task.Id, task);
            }
        }
    }

    public sealed class ChallengeTask
    {
        [YamlMember(Alias = "id")] public string Id { get; set; } = "";
        [YamlMember(Alias = "title")]
        public Dictionary<string, string> Title { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [YamlMember(Alias = "type")] public string Type { get; set; } = "";
        [YamlMember(Alias = "amount")] public int Amount { get; set; } = 1;
        [YamlMember(Alias = "cooldown")] public int Cooldown { get; set; }
        [YamlMember(Alias = "visible")] public bool Visible { get; set; } = true;
        [YamlMember(Alias = "announce_progress")] public bool AnnounceProgress { get; set; } = true;
        [YamlMember(Alias = "announce_completion")] public bool AnnounceCompletion { get; set; } = true;
        [YamlMember(Alias = "data", DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public Dictionary<string, Dictionary<string, string>> Data { get; set; } = new();
        [YamlMember(Alias = "rules")] public List<ChallengeRule> Rules { get; set; } = [];
        [YamlMember(Alias = "actions")] public List<ChallengeAction> Actions { get; set; } = [];
        [YamlMember(Alias = "requires")] public List<string> Requires { get; set; } = [];
    }

    public sealed class ChallengeRule
    {
        [YamlMember(Alias = "key")] public string Key { get; set; } = "";
        [YamlMember(Alias = "operator")] public string Operator { get; set; } = "==";
        [YamlMember(Alias = "value")] public string Value { get; set; } = "";
    }

    public sealed class ChallengeAction
    {
        [YamlMember(Alias = "type")] public string Type { get; set; } = "";
        [YamlMember(Alias = "values")] public List<string> Values { get; set; } = [];
    }

    public sealed class ChallengeSchedule
    {
        [YamlMember(Alias = "title")]
        public Dictionary<string, string> Title { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        [YamlMember(Alias = "date_start")] public string StartDate { get; set; } = "2025-01-01 00:00:00";
        [YamlMember(Alias = "date_end")] public string EndDate { get; set; } = "2025-02-01 00:00:00";
        [YamlMember(Alias = "challenges")] public List<string> Challenges { get; set; } = [];
    }

    public sealed class RunningSchedule
    {
        public string Key { get; set; } = "";
        public string ScheduleId { get; set; } = "";
        public Dictionary<string, string> Title { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string StartDate { get; set; } = "";
        public string EndDate { get; set; } = "";
        public List<ChallengeDefinition> Challenges { get; set; } = [];
    }
}
