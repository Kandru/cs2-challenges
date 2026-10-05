using System.Globalization;
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
            "menu_title", "menu_page", "menu_f_all", "menu_f_progress", "menu_f_ending", "menu_f_starting",
            "menu_empty", "menu_tasks_h", "menu_by_h",
            "score_title", "score_page", "score_h_rank", "score_h_name", "score_h_cur", "score_h_tot",
            "spin_name", "spin_cur", "spin_tot", "spin_rank", "spin_l_cur", "spin_l_tot",
        ];

        private sealed record Entry(ChallengeDefinition Challenge, int Percent, string Meta);
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
            CustomHud.SetText(player, Panel, "menu_title", Context.Text(player, "hud.menu.title"));
            CustomHud.SetText(player, Panel, "menu_f_all", Context.Text(player, "hud.menu.filter.all"));
            CustomHud.SetText(player, Panel, "menu_f_progress", Context.Text(player, "hud.menu.filter.progress"));
            CustomHud.SetText(player, Panel, "menu_f_ending", Context.Text(player, "hud.menu.filter.ending"));
            CustomHud.SetText(player, Panel, "menu_f_starting", Context.Text(player, "hud.menu.filter.starting"));
            CustomHud.SetText(player, Panel, "menu_tasks_h", Context.Text(player, "hud.menu.tasks"));
            CustomHud.SetText(player, Panel, "menu_by_h", Context.Text(player, "hud.menu.solved_by"));
            CustomHud.SetText(player, Panel, "score_title", Context.Text(player, "hud.menu.scoreboard"));
            CustomHud.SetText(player, Panel, "score_h_rank", Context.Text(player, "hud.menu.scoreboard.col.rank"));
            CustomHud.SetText(player, Panel, "score_h_name", Context.Text(player, "hud.menu.scoreboard.col.name"));
            CustomHud.SetText(player, Panel, "score_h_cur", Context.Text(player, "hud.menu.scoreboard.col.solved"));
            CustomHud.SetText(player, Panel, "score_h_tot", Context.Text(player, "hud.menu.scoreboard.col.total"));
            CustomHud.SetText(player, Panel, "spin_l_cur", Context.Text(player, "hud.menu.scoreboard.you.solved"));
            CustomHud.SetText(player, Panel, "spin_l_tot", Context.Text(player, "hud.menu.scoreboard.you.total"));
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
            List<Entry> entries = BuildEntries(state, schedule);
            int pageSize = ListPageSize;
            int pages = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            state.MenuPage = Math.Clamp(state.MenuPage, 0, pages - 1);
            CustomHud.SetText(player, Panel, "menu_page", $"{state.MenuPage + 1} / {pages}");
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
                CustomHud.SetText(player, Panel, $"m{i}_meta", entry.Meta);
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

        private static List<Entry> BuildEntries(PlayerState state, RunningSchedule? schedule)
        {
            string activeKey = schedule?.Key ?? string.Empty;
            int Percent(ChallengeDefinition c) =>
                schedule != null && schedule.Challenges.Contains(c)
                    ? ChallengeProgress.GetChallengePercent(state, activeKey, c)
                    : 0;
            Entry Make(ChallengeDefinition c)
            {
                int percent = Percent(c);
                return new Entry(c, percent, $"{percent}%");
            }

            switch (state.MenuFilter)
            {
                case FilterAll:
                    return OrderByProgress(Context.ChallengeMap.Values.Select(Make));
                case FilterEnding:
                    return ScheduleEntries(s => ParseDate(s.EndDate), Percent);
                case FilterStarting:
                    return ScheduleEntries(s => ParseDate(s.StartDate), Percent);
                default:
                    if (schedule == null)
                    {
                        return [];
                    }

                    return OrderByProgress(
                        schedule.Challenges
                            .Where(c => !ChallengeProgress.IsChallengeSolved(state, activeKey, c))
                            .Select(Make));
            }
        }

        private static List<Entry> OrderByProgress(IEnumerable<Entry> entries) =>
            entries
                .OrderByDescending(e => e.Percent)
                .ThenBy(SortTitle, StringComparer.OrdinalIgnoreCase)
                .ToList();

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
            Func<ChallengeDefinition, int> percent)
        {
            DateTime now = DateTime.UtcNow;
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
            foreach ((ChallengeSchedule schedule, DateTime date) in upcoming)
            {
                string meta = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                foreach (string rawId in schedule.Challenges)
                {
                    string id = rawId.EndsWith(":*", StringComparison.Ordinal) ? rawId[..^2] : rawId;
                    if (seen.Add(id) && Context.ChallengeMap.TryGetValue(id, out ChallengeDefinition? challenge))
                    {
                        entries.Add(new Entry(challenge, percent(challenge), meta));
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
            CustomHud.SetText(player, Panel, "score_page", $"{state.ScoreboardPage + 1} / {pages}");

            int selfIndex = board.FindIndex(x => x.Player == player);
            if (selfIndex >= 0)
            {
                ScoreEntry self = board[selfIndex];
                CustomHud.SetText(player, Panel, "spin_name", player.PlayerName);
                CustomHud.SetText(player, Panel, "spin_cur", FormatSolved(self.Current, available));
                CustomHud.SetText(player, Panel, "spin_tot", self.Total.ToString());
                CustomHud.SetText(player, Panel, "spin_rank", $"#{selfIndex + 1}");
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
                    CustomHud.SetText(player, Panel, $"s{i}_rank", $"#{index + 1}");
                    CustomHud.SetText(player, Panel, $"s{i}_name", entry.Player.PlayerName);
                    CustomHud.SetText(player, Panel, $"s{i}_cur", FormatSolved(entry.Current, available));
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

        private static string FormatSolved(int solved, int available) => $"{solved} / {available}";

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
