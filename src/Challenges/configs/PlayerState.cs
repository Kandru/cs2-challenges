using System.Text.Json.Serialization;
using Challenges.Enums;
using Challenges.Huds;

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

        [JsonIgnore] public bool Dirty;
        /// <summary>Cheap progress probe for placeholder adoption (not a full weight walk).</summary>
        [JsonIgnore]
        public bool HasProgress =>
            Statistics.AmountChallengesSolved > 0 || Challenges.Count > 0;

        [JsonIgnore] public ActiveMenu ActiveMenu = ActiveMenu.None;
        [JsonIgnore] public int ScoreboardPage;
        [JsonIgnore] public int MenuListPage;
        [JsonIgnore] public string MenuFilter = "progress";
        [JsonIgnore] public ScoreboardSort ScoreboardSort = ScoreboardSort.Solved;
        [JsonIgnore] public ScoreboardFilter ScoreboardFilter = ScoreboardFilter.Online;
        [JsonIgnore] public string? MenuDetailChallengeId;
        /// <summary>Steam id of the player whose progress the list shows; null/empty = viewer.</summary>
        [JsonIgnore] public string? MenuSubjectSteamId;
        [JsonIgnore] public readonly string?[] MenuRowChallengeIds = new string?[Menu.ListSlots];
        [JsonIgnore] public readonly string?[] MenuScoreRowSteamIds = new string?[Menu.ScoreSlots];
        /// <summary>How many list / detail slots the last paint filled (unused clears stop here).</summary>
        [JsonIgnore] public int MenuListPainted;
        [JsonIgnore] public int MenuDetailPainted;
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

        /// <summary>Clears menu/tracker session fields so a later connect does not inherit UI state.</summary>
        public void ResetSession()
        {
            ActiveMenu = ActiveMenu.None;
            ScoreboardPage = 0;
            MenuListPage = 0;
            MenuFilter = "progress";
            ScoreboardSort = ScoreboardSort.Solved;
            ScoreboardFilter = ScoreboardFilter.Online;
            MenuDetailChallengeId = null;
            MenuSubjectSteamId = null;
            MenuListPainted = 0;
            MenuDetailPainted = 0;
            Array.Clear(MenuRowChallengeIds);
            Array.Clear(MenuScoreRowSteamIds);
            TrackerFreezeVisible = false;
            TrackerFreezeUntil = null;
            TrackerFreezeDuration = 0;
            TrackerProgressUntil = null;
            TrackerFadeUntil = null;
            TrackerUpNextPending = false;
            TrackerShowingUpNext = false;
            TrackerProgressItems.Clear();
            TrackerRuleBrokenQueue.Clear();
            TrackerFingerprint = null;
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
