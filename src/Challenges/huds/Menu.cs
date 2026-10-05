using CounterStrikeSharp.API.Core;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>
    /// Fullscreen challenge menu: compact challenge cards (header + 50/50 tasks|completers,
    /// 4 per page, no list scroll) and a scoreboard ranked by schedule or lifetime solves.
    /// </summary>
    public static class Menu
    {
        public const string Panel = "Menu";
        public const int ListSlots = 4;
        public const int ScoreSlots = 12;
        public const int TaskSlots = ChallengeCardPaint.TaskSlots;
        public const int CompleterSlots = ChallengeCardPaint.CompleterSlots;
        public const int MaxPageSize = 4;
        public const string FilterAll = "all";
        public const string FilterProgress = "progress";
        public const string FilterEnding = "ending";
        public const string FilterStarting = "starting";
        private const string ScoreHeadCurId = "ch-score-h-cur";
        private const string ScoreHeadTotId = "ch-score-h-tot";

        private static readonly string[] Filters = [FilterAll, FilterProgress, FilterEnding, FilterStarting];
        private static readonly string[] ChromeVars =
        [
            "menu_title", "menu_page", "menu_prev", "menu_next",
            "menu_f_all", "menu_f_progress", "menu_f_ending", "menu_f_starting",
            "menu_empty", "menu_tasks_h", "menu_by_h",
            "score_title", "score_page", "score_prev", "score_next",
            "score_h_rank", "score_h_name", "score_h_cur", "score_h_tot",
            "spin_name", "spin_cur", "spin_tot", "spin_rank", "spin_l_cur", "spin_l_tot",
        ];

        private static readonly (string Var, string Key)[] ChromeLabels =
        [
            ("menu_title", "hud.menu.title"),
            ("menu_f_all", "hud.menu.filter.all"),
            ("menu_f_progress", "hud.menu.filter.progress"),
            ("menu_f_ending", "hud.menu.filter.ending"),
            ("menu_f_starting", "hud.menu.filter.starting"),
            ("menu_tasks_h", "hud.menu.tasks"),
            ("menu_by_h", "hud.menu.solved_by"),
            ("score_title", "hud.menu.scoreboard"),
            ("score_h_rank", "hud.menu.scoreboard.col.rank"),
            ("score_h_name", "hud.menu.scoreboard.col.name"),
            ("score_h_cur", "hud.menu.scoreboard.col.solved"),
            ("score_h_tot", "hud.menu.scoreboard.col.total"),
            ("spin_l_cur", "hud.menu.scoreboard.you.solved"),
            ("spin_l_tot", "hud.menu.scoreboard.you.total"),
        ];

        private sealed record Entry(
            ChallengeDefinition Challenge,
            int Percent,
            string When,
            bool IsActive,
            DateTime SortTime);
        private sealed record ScoreEntry(CCSPlayerController Player, int Current, int Total);

        public static string ListRowId(int index) => $"ch-mrow-{index}";
        public static string ListFillId(int index) => $"ch-mfill-{index}";
        public static string ListTaskId(int row, int task) => $"ch-mtask-{row}-{task}";
        public static string ListCompleterId(int row, int slot) => $"ch-mby-{row}-{slot}";
        public static string ScoreRowId(int index) => $"ch-srow-{index}";
        public const string PinnedRowId = "ch-spin";

        public static bool IsMenuLayout(CCSCustomHudLayout layout) => CustomHud.IsLayout(layout, Panel);

        public static int ListPageSize => Math.Clamp(Context.Config.Gui.MenuPageSize, 1, MaxPageSize);

        private static ChallengeCardPaint.Slots CardSlots(int row) => new(
            Panel,
            t => ListTaskId(row, t),
            t => $"m{row}_t{t}",
            b => ListCompleterId(row, b),
            b => $"m{row}_by{b}");

        public static bool Open(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return false;
            }

            state.MenuPage = 0;
            state.ScoreboardPage = 0;
            state.MenuFilter = FilterProgress;
            state.ScoreboardSort = ScoreboardSort.Current;
            state.ActiveMenu = ActiveMenu.Challenges;

            bool opened = HudMenu.Open(player, new HudMenuOpenOptions
            {
                InputMode = HudMenuInputMode.Mouse,
                OnButton = OnButton,
                OnClosed = closed =>
                {
                    if (Context.GetState(closed) is { } s)
                    {
                        s.ActiveMenu = ActiveMenu.None;
                    }
                },
            });

            if (!opened)
            {
                state.ActiveMenu = ActiveMenu.None;
                return false;
            }

            Paint(player);
            return true;
        }

        public static void OnButton(CCSPlayerController player, string buttonId)
        {
            if (Context.GetState(player) is not { } state)
            {
                return;
            }

            switch (buttonId)
            {
                case HudMenu.BtnClose:
                    HudMenu.Close(player);
                    return;
                case HudMenu.BtnPrev:
                    state.MenuPage = Math.Max(0, state.MenuPage - 1);
                    break;
                case HudMenu.BtnNext:
                    state.MenuPage++;
                    break;
                case HudMenu.BtnScorePrev:
                    state.ScoreboardPage = Math.Max(0, state.ScoreboardPage - 1);
                    break;
                case HudMenu.BtnScoreNext:
                    state.ScoreboardPage++;
                    break;
                case HudMenu.BtnScoreSort:
                    state.ScoreboardSort = state.ScoreboardSort == ScoreboardSort.Total
                        ? ScoreboardSort.Current
                        : ScoreboardSort.Total;
                    state.ScoreboardPage = 0;
                    break;
                case HudMenu.BtnFilterAll:
                    SetFilter(state, FilterAll);
                    break;
                case HudMenu.BtnFilterProgress:
                    SetFilter(state, FilterProgress);
                    break;
                case HudMenu.BtnFilterEnding:
                    SetFilter(state, FilterEnding);
                    break;
                case HudMenu.BtnFilterStarting:
                    SetFilter(state, FilterStarting);
                    break;
                default:
                    return;
            }

            Paint(player);
        }

        public static void WriteDefaults(CCSPlayerController player)
        {
            foreach (string name in ChromeVars)
            {
                CustomHud.SetText(player, Panel, name, string.Empty);
            }

            foreach (string filter in Filters)
            {
                CustomHud.SetHasClass(player, FilterButtonId(filter), "active", false);
            }

            for (int i = 0; i < ListSlots; i++)
            {
                ClearListSlot(player, i, off: false);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                ClearScoreSlot(player, i);
            }

            CustomHud.SetHasClass(player, PinnedRowId, "empty", true);
            CustomHud.SetHasClass(player, ScoreHeadCurId, "sort-active", false);
            CustomHud.SetHasClass(player, ScoreHeadTotId, "sort-active", false);
        }

        public static void Paint(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player)
                || !HudMenu.IsOpen(player)
                || Context.GetState(player) is not { } state)
            {
                return;
            }

            RunningSchedule? schedule = Context.Schedule;
            PaintChrome(player, state);
            PaintList(player, state, schedule);
            PaintScoreboard(player, state, schedule);
        }

        private static void SetFilter(PlayerState state, string filter)
        {
            state.MenuFilter = filter;
            state.MenuPage = 0;
        }

        private static string FilterButtonId(string filter) => filter switch
        {
            FilterAll => HudMenu.BtnFilterAll,
            FilterEnding => HudMenu.BtnFilterEnding,
            FilterStarting => HudMenu.BtnFilterStarting,
            _ => HudMenu.BtnFilterProgress,
        };

        private static void PaintChrome(CCSPlayerController player, PlayerState state)
        {
            foreach ((string varName, string key) in ChromeLabels)
            {
                CustomHud.SetText(player, Panel, varName, Context.Text(player, key));
            }

            string prev = Context.Text(player, "hud.menu.prev");
            string next = Context.Text(player, "hud.menu.next");
            CustomHud.SetText(player, Panel, "menu_prev", prev);
            CustomHud.SetText(player, Panel, "menu_next", next);
            CustomHud.SetText(player, Panel, "score_prev", prev);
            CustomHud.SetText(player, Panel, "score_next", next);

            bool sortTotal = state.ScoreboardSort == ScoreboardSort.Total;
            CustomHud.SetHasClass(player, ScoreHeadCurId, "sort-active", !sortTotal);
            CustomHud.SetHasClass(player, ScoreHeadTotId, "sort-active", sortTotal);
            foreach (string filter in Filters)
            {
                CustomHud.SetHasClass(player, FilterButtonId(filter), "active", filter == state.MenuFilter);
            }
        }

        private static void PaintList(CCSPlayerController player, PlayerState state, RunningSchedule? schedule)
        {
            List<Entry> entries = BuildEntries(player, state, schedule);
            int pageSize = ListPageSize;
            int pages = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            state.MenuPage = Math.Clamp(state.MenuPage, 0, pages - 1);
            CustomHud.SetText(player, Panel, "menu_page", Context.FormatPage(player, state.MenuPage + 1, pages));
            CustomHud.SetText(
                player,
                Panel,
                "menu_empty",
                entries.Count == 0 ? Context.Text(player, "hud.menu.empty") : string.Empty);

            string? scheduleKey = schedule?.Key;

            for (int i = 0; i < ListSlots; i++)
            {
                bool offPage = i >= pageSize;
                int index = state.MenuPage * pageSize + i;
                if (offPage || index >= entries.Count)
                {
                    ClearListSlot(player, i, off: offPage);
                    continue;
                }

                Entry entry = entries[index];
                bool inSchedule = schedule != null && schedule.Challenges.Contains(entry.Challenge);
                CustomHud.SetText(player, Panel, $"m{i}_title", Titles.For(player, entry.Challenge.Title));
                CustomHud.SetText(player, Panel, $"m{i}_when", entry.When);
                CustomHud.SetText(player, Panel, $"m{i}_meta", Context.FormatPercent(player, entry.Percent));
                CustomHud.SetStepPercent(player, ListFillId(i), entry.Percent);
                CustomHud.SetHasClass(player, ListRowId(i), "empty", false);
                CustomHud.SetHasClass(player, ListRowId(i), "is-off", false);
                PaintTasks(player, state, inSchedule ? scheduleKey : null, entry.Challenge, i);
                PaintCompleters(player, inSchedule ? schedule! : null, entry.Challenge, i);
            }
        }

        private static void ClearListSlot(CCSPlayerController player, int row, bool off)
        {
            CustomHud.SetText(player, Panel, $"m{row}_title", string.Empty);
            CustomHud.SetText(player, Panel, $"m{row}_when", string.Empty);
            CustomHud.SetText(player, Panel, $"m{row}_meta", string.Empty);
            CustomHud.SetStepPercent(player, ListFillId(row), 0);
            CustomHud.SetHasClass(player, ListRowId(row), "empty", true);
            CustomHud.SetHasClass(player, ListRowId(row), "is-off", off);
            ChallengeCardPaint.Slots slots = CardSlots(row);
            ChallengeCardPaint.ClearTasks(player, slots);
            ChallengeCardPaint.ClearCompleters(player, slots);
        }

        private static void PaintTasks(
            CCSPlayerController player,
            PlayerState state,
            string? scheduleKey,
            ChallengeDefinition challenge,
            int row) =>
            ChallengeCardPaint.PaintTasks(player, state, scheduleKey, challenge, CardSlots(row));

        private static void PaintCompleters(
            CCSPlayerController player,
            RunningSchedule? schedule,
            ChallengeDefinition challenge,
            int row) =>
            ChallengeCardPaint.PaintCompleters(player, schedule, challenge, CardSlots(row));

        private static List<Entry> BuildEntries(CCSPlayerController player, PlayerState state, RunningSchedule? schedule)
        {
            DateTime now = DateTime.UtcNow;
            string activeKey = schedule?.Key ?? string.Empty;
            int Percent(ChallengeDefinition c) =>
                schedule != null && schedule.Challenges.Contains(c)
                    ? ChallengeProgress.GetChallengePercent(state, activeKey, c)
                    : 0;
            Entry Make(ChallengeDefinition c)
            {
                ScheduleTiming.Info timing = ScheduleTiming.ForChallenge(c, schedule, Context.ScheduleMap, now);
                return new Entry(
                    c,
                    Percent(c),
                    Context.FormatWhen(player, timing, now),
                    timing.IsActive,
                    timing.Target ?? DateTime.MaxValue);
            }

            switch (state.MenuFilter)
            {
                case FilterAll:
                    return OrderEntries(Context.ChallengeMap.Values.Select(Make), byTiming: true);
                case FilterEnding:
                    return ScheduleEntries(s => ParseDate(s.EndDate), Make, now);
                case FilterStarting:
                    return ScheduleEntries(s => ParseDate(s.StartDate), Make, now);
                default:
                    if (schedule == null)
                    {
                        return [];
                    }

                    return OrderEntries(
                        schedule.Challenges
                            .Where(c => !ChallengeProgress.IsChallengeSolved(state, activeKey, c))
                            .Select(Make),
                        byTiming: false);
            }
        }

        private static List<Entry> OrderEntries(IEnumerable<Entry> entries, bool byTiming)
        {
            IOrderedEnumerable<Entry> ordered = entries.OrderByDescending(e => e.Percent);
            if (byTiming)
            {
                ordered = ordered
                    .ThenByDescending(e => e.IsActive)
                    .ThenBy(e => e.SortTime);
            }

            return ordered
                .ThenBy(SortTitle, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string SortTitle(Entry entry)
        {
            Dictionary<string, string> titles = entry.Challenge.Title;
            if (titles.TryGetValue("en", out string? en) && !string.IsNullOrEmpty(en))
            {
                return en;
            }

            return titles.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v)) ?? entry.Challenge.Id;
        }

        private static List<Entry> ScheduleEntries(
            Func<ChallengeSchedule, DateTime?> dateOf,
            Func<ChallengeDefinition, Entry> make,
            DateTime now)
        {
            List<(ChallengeSchedule Schedule, DateTime Date)> upcoming = [];
            foreach (ChallengeSchedule schedule in Context.ScheduleMap.Values)
            {
                if (dateOf(schedule) is { } date && date > now)
                {
                    upcoming.Add((schedule, date));
                }
            }

            upcoming.Sort(static (a, b) => a.Date.CompareTo(b.Date));

            List<Entry> entries = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach ((ChallengeSchedule schedule, DateTime _) in upcoming)
            {
                foreach (string rawId in schedule.Challenges)
                {
                    string id = Schedules.ChallengeId(rawId);
                    if (seen.Add(id) && Context.ChallengeMap.TryGetValue(id, out ChallengeDefinition? challenge))
                    {
                        entries.Add(make(challenge));
                    }
                }
            }

            return entries;
        }

        private static DateTime? ParseDate(string value) =>
            Schedules.TryParseDate(value, out DateTime date) ? date : null;

        private static void PaintScoreboard(CCSPlayerController player, PlayerState state, RunningSchedule? schedule)
        {
            int available = schedule?.Challenges.Count ?? 0;
            List<ScoreEntry> board = [];
            foreach (CCSPlayerController human in Players.GetHumans())
            {
                PlayerState? s = Context.GetState(human);
                int current = s != null && schedule != null
                    ? ChallengeProgress.CountSolvedInSchedule(s, schedule)
                    : 0;
                int total = s?.Statistics.AmountChallengesSolved ?? 0;
                board.Add(new ScoreEntry(human, current, total));
            }

            bool byTotal = state.ScoreboardSort == ScoreboardSort.Total;
            board.Sort((a, b) =>
            {
                int primary = byTotal ? b.Total.CompareTo(a.Total) : b.Current.CompareTo(a.Current);
                if (primary != 0)
                {
                    return primary;
                }

                int secondary = byTotal ? b.Current.CompareTo(a.Current) : b.Total.CompareTo(a.Total);
                return secondary != 0
                    ? secondary
                    : string.Compare(a.Player.PlayerName, b.Player.PlayerName, StringComparison.OrdinalIgnoreCase);
            });

            int pages = Math.Max(1, (board.Count + ScoreSlots - 1) / ScoreSlots);
            state.ScoreboardPage = Math.Clamp(state.ScoreboardPage, 0, pages - 1);
            CustomHud.SetText(player, Panel, "score_page", Context.FormatPage(player, state.ScoreboardPage + 1, pages));

            int selfIndex = board.FindIndex(x => x.Player == player);
            if (selfIndex >= 0)
            {
                ScoreEntry self = board[selfIndex];
                CustomHud.SetText(player, Panel, "spin_name", player.PlayerName);
                CustomHud.SetText(player, Panel, "spin_cur", Context.FormatCount(player, self.Current, available));
                CustomHud.SetText(player, Panel, "spin_tot", self.Total.ToString());
                CustomHud.SetText(player, Panel, "spin_rank", Context.FormatRank(player, selfIndex + 1));
                CustomHud.SetHasClass(player, PinnedRowId, "empty", false);
            }
            else
            {
                CustomHud.SetText(player, Panel, "spin_name", string.Empty);
                CustomHud.SetText(player, Panel, "spin_cur", string.Empty);
                CustomHud.SetText(player, Panel, "spin_tot", string.Empty);
                CustomHud.SetText(player, Panel, "spin_rank", string.Empty);
                CustomHud.SetHasClass(player, PinnedRowId, "empty", true);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                int index = state.ScoreboardPage * ScoreSlots + i;
                if (index < board.Count)
                {
                    ScoreEntry entry = board[index];
                    CustomHud.SetText(player, Panel, $"s{i}_rank", Context.FormatRank(player, index + 1));
                    CustomHud.SetText(player, Panel, $"s{i}_name", entry.Player.PlayerName);
                    CustomHud.SetText(player, Panel, $"s{i}_cur", Context.FormatCount(player, entry.Current, available));
                    CustomHud.SetText(player, Panel, $"s{i}_tot", entry.Total.ToString());
                    CustomHud.SetHasClass(player, ScoreRowId(i), "empty", false);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "is-off", false);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "is-self", entry.Player == player);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "alt", index % 2 == 1);
                }
                else
                {
                    ClearScoreSlot(player, i);
                }
            }
        }

        private static void ClearScoreSlot(CCSPlayerController player, int index)
        {
            CustomHud.SetText(player, Panel, $"s{index}_rank", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_name", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_cur", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_tot", string.Empty);
            CustomHud.SetHasClass(player, ScoreRowId(index), "empty", true);
            CustomHud.SetHasClass(player, ScoreRowId(index), "is-off", true);
            CustomHud.SetHasClass(player, ScoreRowId(index), "is-self", false);
            CustomHud.SetHasClass(player, ScoreRowId(index), "alt", false);
        }
    }
}
