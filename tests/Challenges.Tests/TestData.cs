using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>Synthetic blueprints, schedules and event data, normalized and compiled like loaded YAML.</summary>
    internal static class TestData
    {
        public const string ScheduleKey = "test-schedule";

        public static ChallengeRule Rule(string key, string op, string value, int? min = null, int? max = null) =>
            new() { Key = key, Operator = op, Value = value, MinPlayers = min, MaxPlayers = max };

        public static ChallengeAction Action(string type, params string[] values) =>
            new() { Type = type, Values = [.. values] };

        public static ChallengeTask Task(
            string id,
            string type,
            int amount = 1,
            ChallengeRule[]? rules = null,
            ChallengeAction[]? actions = null,
            string[]? requires = null,
            int cooldown = 0,
            bool visible = true) =>
            new()
            {
                Id = id,
                Type = type,
                Amount = amount,
                Cooldown = cooldown,
                Visible = visible,
                Rules = rules == null ? [] : [.. rules],
                Actions = actions == null ? [] : [.. actions],
                Requires = requires == null ? [] : [.. requires],
            };

        /// <summary>A blueprint after <c>Challenges.NormalizeDefinition</c> (keys canonical, rules compiled).</summary>
        public static ChallengeDefinition Challenge(string id, params ChallengeTask[] tasks)
        {
            ChallengeDefinition def = new()
            {
                Id = id,
                Title = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["en"] = id },
                Tasks = [.. tasks],
            };
            global::Challenges.Challenges.NormalizeDefinition(def);
            return def;
        }

        public static RunningSchedule Schedule(params ChallengeDefinition[] challenges) =>
            new() { Key = ScheduleKey, ScheduleId = "test", Challenges = [.. challenges] };

        public static Dictionary<string, EventValue> Data(params (string Key, EventValue Value)[] values)
        {
            Dictionary<string, EventValue> data = new(StringComparer.Ordinal);
            foreach ((string key, EventValue value) in values)
            {
                data[key] = value;
            }

            return data;
        }

        public static string SteamId(int i) => (76561198000000000L + i).ToString();
    }
}
