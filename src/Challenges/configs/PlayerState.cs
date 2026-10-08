using System.Text.Json.Serialization;
using Challenges.Enums;

namespace Challenges.Configs
{
    public sealed class PlayerState
    {
        [JsonPropertyName("username")] public string Username { get; set; } = string.Empty;
        [JsonPropertyName("steamid")] public string SteamId { get; set; } = string.Empty;
        [JsonPropertyName("clantag")] public string? ClanTag { get; set; } = string.Empty;
        [JsonPropertyName("language")] public string Language { get; set; } = string.Empty;
        /// <summary>scheduleKey → challengeId → taskId → progress</summary>
        [JsonPropertyName("challenges")]
        public Dictionary<string, Dictionary<string, Dictionary<string, TaskProgress>>> Challenges { get; set; } = new();
        [JsonPropertyName("statistics")] public PlayerStatistics Statistics { get; set; } = new();

        [JsonIgnore] public bool Loaded;
        [JsonIgnore] public ActiveMenu ActiveMenu = ActiveMenu.None;
        [JsonIgnore] public int MenuPage;
        [JsonIgnore] public int ScoreboardPage;
        [JsonIgnore] public string MenuFilter = "progress";
        [JsonIgnore] public ScoreboardSort ScoreboardSort = ScoreboardSort.Solved;
        [JsonIgnore] public ScoreboardFilter ScoreboardFilter = ScoreboardFilter.Online;
        [JsonIgnore] public string? MenuDetailChallengeId;
        [JsonIgnore] public int MenuDetailPage;
        /// <summary>Steam id of the player whose progress the list shows; null/empty = viewer.</summary>
        [JsonIgnore] public string? MenuSubjectSteamId;
        /// <summary>Challenge ids for the currently painted list slots (click target lookup).</summary>
        [JsonIgnore] public readonly string?[] MenuRowChallengeIds = new string?[5];
        /// <summary>Steam ids for the currently painted scoreboard slots (click target lookup).</summary>
        [JsonIgnore] public readonly string?[] MenuScoreRowSteamIds = new string?[12];
        [JsonIgnore] public bool TrackerFreezeVisible;
        [JsonIgnore] public DateTime? TrackerFreezeUntil;
        [JsonIgnore] public float TrackerFreezeDuration;
        [JsonIgnore] public DateTime? TrackerProgressUntil;
        [JsonIgnore] public DateTime? TrackerFadeUntil;
        [JsonIgnore] public bool TrackerUpNextPending;
        [JsonIgnore] public bool TrackerShowingUpNext;
        [JsonIgnore] public List<TrackerProgressItem> TrackerProgressItems = [];
        [JsonIgnore] public List<TrackerProgressItem> TrackerRuleBrokenQueue = [];
        [JsonIgnore] public string? TrackerFingerprint;

        public void CopyProgressFrom(PlayerState other)
        {
            Language = other.Language;
            Challenges = other.Challenges;
            Statistics = other.Statistics;
        }
    }

    public sealed class TrackerProgressItem
    {
        public string ChallengeId { get; set; } = string.Empty;
        public string TaskId { get; set; } = string.Empty;
        public TrackerProgressKind Kind { get; set; }
    }

    public sealed class TaskProgress
    {
        [JsonPropertyName("amount")] public int Amount { get; set; }
        [JsonPropertyName("last_update")] public long LastUpdate { get; set; }
    }

    public sealed class PlayerStatistics
    {
        [JsonPropertyName("amount_challenges_solved")] public int AmountChallengesSolved { get; set; }
    }
}
