using System.Globalization;
using CounterStrikeSharp.API.Core;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Fullscreen challenge menu: list (2/3) with filters, scoreboard (1/3) with a pinned self row.</summary>
    public static class Menu
    {
        public const string Panel = "Menu";
        public const int ListSlots = 10;
        public const int ScoreSlots = 8;
        public const string FilterAll = "all";
        public const string FilterProgress = "progress";
        public const string FilterEnding = "ending";
        public const string FilterStarting = "starting";

        private static readonly string[] Filters = [FilterAll, FilterProgress, FilterEnding, FilterStarting];

        private sealed record Entry(ChallengeDefinition Challenge, int Percent, string Meta);

        public static string ListRowId(int index) => $"ch-mrow-{index}";
        public static string ListFillId(int index) => $"ch-mfill-{index}";
        public static string ScoreRowId(int index) => $"ch-srow-{index}";
        public const string PinnedRowId = "ch-spin";

        public static bool IsMenuLayout(CCSCustomHudLayout layout) => CustomHud.IsLayout(layout, Panel);

        public static int ListPageSize => Math.Clamp(Context.Config.Gui.MenuPageSize, 4, ListSlots);

        public static bool Open(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return false;
            }

            state.MenuPage = 0;
            state.ScoreboardPage = 0;
            state.MenuFilter = FilterProgress;
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
            foreach (string name in new[]
                     {
                         "menu_title", "menu_page", "menu_f_all", "menu_f_progress", "menu_f_ending", "menu_f_starting",
                         "menu_empty", "score_title", "score_page", "spin_name", "spin_val", "spin_rank",
                     })
            {
                CustomHud.SetText(player, Panel, name, string.Empty);
            }

            foreach (string filter in Filters)
            {
                CustomHud.SetHasClass(player, FilterButtonId(filter), "active", false);
            }

            for (int i = 0; i < ListSlots; i++)
            {
                CustomHud.SetText(player, Panel, $"m{i}_title", string.Empty);
                CustomHud.SetText(player, Panel, $"m{i}_meta", string.Empty);
                CustomHud.SetStepPercent(player, ListFillId(i), 0);
                CustomHud.SetHasClass(player, ListRowId(i), "empty", true);
                CustomHud.SetHasClass(player, ListRowId(i), "is-off", false);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                CustomHud.SetText(player, Panel, $"s{i}_name", string.Empty);
                CustomHud.SetText(player, Panel, $"s{i}_val", string.Empty);
                CustomHud.SetHasClass(player, ScoreRowId(i), "empty", true);
                CustomHud.SetHasClass(player, ScoreRowId(i), "is-self", false);
            }

            CustomHud.SetHasClass(player, PinnedRowId, "empty", true);
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
            CustomHud.SetText(player, Panel, "score_title", Context.Text(player, "hud.menu.scoreboard"));
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
            CustomHud.SetText(player, Panel, "menu_page", $"{state.MenuPage + 1} / {pages}");
            CustomHud.SetText(
                player,
                Panel,
                "menu_empty",
                entries.Count == 0 ? Context.Text(player, "hud.menu.empty") : string.Empty);

            for (int i = 0; i < ListSlots; i++)
            {
                CustomHud.SetHasClass(player, ListRowId(i), "is-off", i >= pageSize);
                int index = state.MenuPage * pageSize + i;
                if (i < pageSize && index < entries.Count)
                {
                    Entry entry = entries[index];
                    string title = schedule != null && schedule.Challenges.Contains(entry.Challenge)
                        ? Tracker.RowTitle(player, state, schedule, entry.Challenge)
                        : Titles.Expand(Titles.Resolve(entry.Challenge.Title, player), 0, 0);
                    CustomHud.SetText(player, Panel, $"m{i}_title", title);
                    CustomHud.SetText(player, Panel, $"m{i}_meta", entry.Meta);
                    CustomHud.SetStepPercent(player, ListFillId(i), entry.Percent);
                    CustomHud.SetHasClass(player, ListRowId(i), "empty", false);
                }
                else
                {
                    CustomHud.SetText(player, Panel, $"m{i}_title", string.Empty);
                    CustomHud.SetText(player, Panel, $"m{i}_meta", string.Empty);
                    CustomHud.SetStepPercent(player, ListFillId(i), 0);
                    CustomHud.SetHasClass(player, ListRowId(i), "empty", true);
                }
            }
        }

        private static List<Entry> BuildEntries(CCSPlayerController player, PlayerState state, RunningSchedule? schedule)
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
                    return Context.ChallengeMap.Values
                        .OrderBy(c => c.Id, StringComparer.OrdinalIgnoreCase)
                        .Select(Make)
                        .ToList();
                case FilterEnding:
                    return ScheduleEntries(
                        Context.ScheduleMap.Values
                            .Select(s => (Schedule: s, Date: ParseDate(s.EndDate)))
                            .Where(x => x.Date.HasValue && x.Date.Value > DateTime.UtcNow)
                            .OrderBy(x => x.Date!.Value),
                        Percent);
                case FilterStarting:
                    return ScheduleEntries(
                        Context.ScheduleMap.Values
                            .Select(s => (Schedule: s, Date: ParseDate(s.StartDate)))
                            .Where(x => x.Date.HasValue && x.Date.Value > DateTime.UtcNow)
                            .OrderBy(x => x.Date!.Value),
                        Percent);
                default:
                    if (schedule == null)
                    {
                        return [];
                    }

                    return schedule.Challenges
                        .Where(c => !ChallengeProgress.IsChallengeSolved(state, activeKey, c))
                        .Select(Make)
                        .OrderByDescending(e => e.Percent)
                        .ToList();
            }
        }

        private static List<Entry> ScheduleEntries(
            IEnumerable<(ChallengeSchedule Schedule, DateTime? Date)> ordered,
            Func<ChallengeDefinition, int> percent)
        {
            List<Entry> entries = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach ((ChallengeSchedule schedule, DateTime? date) in ordered)
            {
                string meta = date!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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
            List<(CCSPlayerController Player, int Solved)> board = Players.GetHumans()
                .Select(p => (Player: p, Solved: SolvedCount(p, schedule)))
                .OrderByDescending(x => x.Solved)
                .ThenBy(x => x.Player.PlayerName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            int pages = Math.Max(1, (board.Count + ScoreSlots - 1) / ScoreSlots);
            state.ScoreboardPage = Math.Clamp(state.ScoreboardPage, 0, pages - 1);
            CustomHud.SetText(player, Panel, "score_page", $"{state.ScoreboardPage + 1} / {pages}");

            int selfIndex = board.FindIndex(x => x.Player == player);
            if (selfIndex >= 0)
            {
                CustomHud.SetText(player, Panel, "spin_name", player.PlayerName);
                CustomHud.SetText(player, Panel, "spin_val", board[selfIndex].Solved.ToString());
                CustomHud.SetText(player, Panel, "spin_rank", $"#{selfIndex + 1}");
                CustomHud.SetHasClass(player, PinnedRowId, "empty", false);
            }
            else
            {
                CustomHud.SetText(player, Panel, "spin_name", string.Empty);
                CustomHud.SetText(player, Panel, "spin_val", string.Empty);
                CustomHud.SetText(player, Panel, "spin_rank", string.Empty);
                CustomHud.SetHasClass(player, PinnedRowId, "empty", true);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                int index = state.ScoreboardPage * ScoreSlots + i;
                if (index < board.Count)
                {
                    CustomHud.SetText(player, Panel, $"s{i}_name", board[index].Player.PlayerName);
                    CustomHud.SetText(player, Panel, $"s{i}_val", board[index].Solved.ToString());
                    CustomHud.SetHasClass(player, ScoreRowId(i), "empty", false);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "is-self", board[index].Player == player);
                }
                else
                {
                    CustomHud.SetText(player, Panel, $"s{i}_name", string.Empty);
                    CustomHud.SetText(player, Panel, $"s{i}_val", string.Empty);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "empty", true);
                    CustomHud.SetHasClass(player, ScoreRowId(i), "is-self", false);
                }
            }
        }

        private static int SolvedCount(CCSPlayerController player, RunningSchedule? schedule)
        {
            if (Context.GetState(player) is not { } state)
            {
                return 0;
            }

            return schedule != null
                ? ChallengeProgress.CountSolvedInSchedule(state, schedule)
                : state.Statistics.AmountChallengesSolved;
        }
    }
}
