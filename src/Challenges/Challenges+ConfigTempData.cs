using System.Text.Json.Serialization;

namespace Challenges
{
    public class TempDataScheduleConfig
    {
        [JsonPropertyName("key")] public string Key { get; set; } = "";
    }

    public class TempDataConfig
    {
        [JsonPropertyName("current_schedule")] public TempDataScheduleConfig CurrentSchedule { get; set; } = new();
    }
}
