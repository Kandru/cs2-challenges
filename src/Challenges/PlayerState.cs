using System.Text.Json.Serialization;

namespace Challenges
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
        /// <summary>
        /// Bumped by every progress change outside the pipeline's own patches (prune, placeholder adoption).
        /// The engine reseeds its worker shadow when this differs from the seeded version. Game thread only.
        /// </summary>
        [JsonIgnore] public int ProgressVersion;
        /// <summary>
        /// Engine cache, indexed by type id: true while the type has a task this player has not completed.
        /// Valid for <see cref="OpenTypesLayout"/> and <see cref="OpenTypesVersion"/> unless dirty. Game thread only.
        /// </summary>
        [JsonIgnore] public bool[]? OpenTypes;
        [JsonIgnore] public int OpenTypesLayout;
        [JsonIgnore] public int OpenTypesVersion;
        [JsonIgnore] public bool OpenTypesDirty;
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
        /// <summary>Steam id of the player whose progress the list shows; null/empty = viewer.</summary>
        [JsonIgnore] public string? MenuSubjectSteamId;
        [JsonIgnore] public readonly string?[] MenuScoreRowSteamIds = new string?[Menu.ScoreSlots];
        /// <summary>How many list slots the last paint filled (unused clears stop here).</summary>
        [JsonIgnore] public int MenuListPainted;
        /// <summary>Applied progress changed what the open menu shows; repainted from the roster tick.</summary>
        [JsonIgnore] public bool MenuDirty;
        /// <summary><see cref="Environment.TickCount64"/> of the last menu paint.</summary>
        [JsonIgnore] public long MenuPaintedAt;
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
        /// <summary>
        /// Tracker rows, heading and hint must be rebuilt on the next paint (progress, language, mode or schedule changed).
        /// Game thread only.
        /// </summary>
        [JsonIgnore] public bool TrackerContentDirty = true;
        /// <summary>Schedule and <see cref="ProgressVersion"/> the tracker content was last built for.</summary>
        [JsonIgnore] public RunningSchedule? TrackerSchedule;
        [JsonIgnore] public int TrackerProgressVersion;

        /// <summary>Clears tracker session fields without touching menu state.</summary>
        public void ClearTrackerUi()
        {
            TrackerFreezeVisible = false;
            TrackerFreezeUntil = null;
            TrackerFreezeDuration = 0;
            TrackerProgressUntil = null;
            TrackerFadeUntil = null;
            TrackerUpNextPending = false;
            TrackerShowingUpNext = false;
            TrackerProgressItems.Clear();
            TrackerRuleBrokenQueue.Clear();
            InvalidateTracker();
        }

        /// <summary>Forces the next tracker paint to rebuild its content.</summary>
        public void InvalidateTracker()
        {
            TrackerFingerprint = null;
            TrackerContentDirty = true;
        }

        /// <summary>Clears menu/tracker session fields so a later connect does not inherit UI state.</summary>
        public void ResetSession()
        {
            ActiveMenu = ActiveMenu.None;
            ScoreboardPage = 0;
            MenuListPage = 0;
            MenuFilter = "progress";
            ScoreboardSort = ScoreboardSort.Solved;
            ScoreboardFilter = ScoreboardFilter.Online;
            MenuSubjectSteamId = null;
            MenuListPainted = 0;
            MenuDirty = false;
            MenuPaintedAt = 0;
            Array.Clear(MenuScoreRowSteamIds);
            ClearTrackerUi();
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
