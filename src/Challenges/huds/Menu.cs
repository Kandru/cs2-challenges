using CounterStrikeSharp.API.Core;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>
    /// Fullscreen challenge menu: scrollable challenge cards (header + 50/50 tasks|completers),
    /// a paginated scoreboard, and a scrollable challenge detail column.
    /// </summary>
    public static class Menu
    {
        public const string Panel = "Menu";
        public const int ListSlots = 20;
        public const int ScoreSlots = 12;
        public const int DetailSlots = 20;
        public const int DetailRuleSlots = 8;
        public const string FilterAll = "all";
        public const string FilterSolved = "solved";
        public const string FilterProgress = "progress";
        public const string FilterEnding = "ending";
        public const string FilterStarting = "starting";
        private const string ScoreHeadCurId = "ch-score-h-cur";
        private const string ScoreHeadTotId = "ch-score-h-tot";
        private const string ScoreColumnId = "ch-score";
        private const string DetailColumnId = "ch-detail";
        private const string EmptyLabelId = "ch-menu-empty";
        private static readonly TimeSpan EndingSoonWindow = TimeSpan.FromDays(7);

        private static readonly FilterDef[] FilterDefs =
        [
            new(FilterAll, HudMenu.BtnFilterAll, "menu_f_all", "hud.menu.filter.all"),
            new(FilterSolved, HudMenu.BtnFilterSolved, "menu_f_solved", "hud.menu.filter.solved"),
            new(FilterProgress, HudMenu.BtnFilterProgress, "menu_f_progress", "hud.menu.filter.progress"),
            new(FilterEnding, HudMenu.BtnFilterEnding, "menu_f_ending", "hud.menu.filter.ending"),
            new(FilterStarting, HudMenu.BtnFilterStarting, "menu_f_starting", "hud.menu.filter.starting"),
        ];

        private static readonly string[] ChromeVars =
        [
            "menu_title", "list_title", "list_page",
            "menu_f_all", "menu_f_solved", "menu_f_progress", "menu_f_ending", "menu_f_starting",
            "menu_empty", "menu_tasks_h", "menu_by_h",
            "score_title", "score_page", "score_prev", "score_next",
            "score_f_all", "score_f_online", "score_s_solved", "score_s_lifetime",
            "score_h_rank", "score_h_name", "score_h_cur", "score_h_tot",
            "spin_name", "spin_cur", "spin_tot", "spin_rank", "spin_l_cur", "spin_l_tot",
            "detail_title", "detail_back",
        ];

        private static readonly (string Var, string Key)[] ChromeLabels =
        [
            ("menu_title", "hud.menu.title"),
            ("menu_tasks_h", "hud.menu.tasks"),
            ("menu_by_h", "hud.menu.solved_by"),
            ("score_title", "hud.menu.scoreboard"),
            ("score_f_all", "hud.menu.scoreboard.filter.all"),
            ("score_f_online", "hud.menu.scoreboard.filter.online"),
            ("score_s_solved", "hud.menu.scoreboard.filter.solved"),
            ("score_s_lifetime", "hud.menu.scoreboard.filter.lifetime"),
            ("score_h_rank", "hud.menu.scoreboard.col.rank"),
            ("score_h_name", "hud.menu.scoreboard.col.name"),
            ("score_h_cur", "hud.menu.scoreboard.col.solved"),
            ("score_h_tot", "hud.menu.scoreboard.col.lifetime"),
            ("spin_l_cur", "hud.menu.scoreboard.you.solved"),
            ("spin_l_tot", "hud.menu.scoreboard.you.lifetime"),
        ];

        private sealed record FilterDef(string Id, string ButtonId, string Var, string Key);

        private static string FilterFor(string buttonId)
        {
            foreach (FilterDef filter in FilterDefs)
            {
                if (filter.ButtonId == buttonId)
                {
                    return filter.Id;
                }
            }

            return FilterProgress;
        }
        private sealed record MenuSubject(PlayerState Progress, string? Name);

        private sealed record Entry(
            ChallengeDefinition Challenge,
            int Percent,
            string When,
            bool IsActive,
            DateTime SortTime);
        private sealed record ScoreEntry(
            string Name,
            string SteamId,
            int Current,
            int Total,
            CCSPlayerController? Player);

        public const string PinnedRowId = "ch-spin";

        private static readonly string[] ListRowIds = Ids("ch-mrow-", ListSlots);
        private static readonly string[] ListFillIds = Ids("ch-mfill-", ListSlots);
        private static readonly string[] ListTitleVars = Ids("m", ListSlots, "_title");
        private static readonly string[] ListWhenVars = Ids("m", ListSlots, "_when");
        private static readonly string[] ListMetaVars = Ids("m", ListSlots, "_meta");
        private static readonly string[] ScoreRowIds = Ids("ch-srow-", ScoreSlots);
        private static readonly string[] DetailRowIds = Ids("ch-drow-", DetailSlots);
        private static readonly string[] DetailTitleVars = Ids("d", DetailSlots, "_title");
        private static readonly string[][] DetailRuleIds = RuleIds("ch-drule-", DetailSlots, DetailRuleSlots);
        private static readonly string[][] DetailRuleVars = RuleIds("d", DetailSlots, DetailRuleSlots, "_r");
        private static readonly ChallengeCardPaint.Slots[] ListCardSlots = CreateListCardSlots();

        public static string ListRowId(int index) => ListRowIds[index];
        public static string ListFillId(int index) => ListFillIds[index];
        public static string ListTaskId(int row, int task) => $"ch-mtask-{row}-{task}";
        public static string ListCompleterId(int row, int slot) => $"ch-mby-{row}-{slot}";
        public static string ScoreRowId(int index) => ScoreRowIds[index];
        public static string DetailRowId(int index) => DetailRowIds[index];
        public static string DetailRuleId(int row, int slot) => DetailRuleIds[row][slot];

        public static bool IsMenuLayout(CCSCustomHudLayout layout) => CustomHud.IsLayout(layout, Panel);

        private static string[] Ids(string prefix, int count, string suffix = "")
        {
            string[] ids = new string[count];
            for (int i = 0; i < count; i++)
            {
                ids[i] = prefix + i + suffix;
            }

            return ids;
        }

        private static string[][] RuleIds(string prefix, int rows, int slots, string mid = "-")
        {
            string[][] ids = new string[rows][];
            for (int row = 0; row < rows; row++)
            {
                string[] rowIds = new string[slots];
                for (int slot = 0; slot < slots; slot++)
                {
                    rowIds[slot] = prefix + row + mid + slot;
                }

                ids[row] = rowIds;
            }

            return ids;
        }

        private static ChallengeCardPaint.Slots[] CreateListCardSlots()
        {
            ChallengeCardPaint.Slots[] slots = new ChallengeCardPaint.Slots[ListSlots];
            for (int row = 0; row < ListSlots; row++)
            {
                string[] taskIds = new string[ChallengeCardPaint.TaskSlots];
                string[] taskVars = new string[ChallengeCardPaint.TaskSlots];
                string[] byIds = new string[ChallengeCardPaint.CompleterSlots];
                string[] byVars = new string[ChallengeCardPaint.CompleterSlots];
                for (int t = 0; t < ChallengeCardPaint.TaskSlots; t++)
                {
                    taskIds[t] = $"ch-mtask-{row}-{t}";
                    taskVars[t] = $"m{row}_t{t}";
                }

                for (int b = 0; b < ChallengeCardPaint.CompleterSlots; b++)
                {
                    byIds[b] = $"ch-mby-{row}-{b}";
                    byVars[b] = $"m{row}_by{b}";
                }

                slots[row] = new ChallengeCardPaint.Slots(
                    Panel,
                    t => taskIds[t],
                    t => taskVars[t],
                    b => byIds[b],
                    b => byVars[b]);
            }

            return slots;
        }

        public static bool Open(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return false;
            }

            state.ScoreboardPage = 0;
            state.MenuListPage = 0;
            state.MenuFilter = FilterProgress;
            state.ScoreboardSort = ScoreboardSort.Solved;
            state.ScoreboardFilter = ScoreboardFilter.Online;
            state.MenuSubjectSteamId = null;
            state.MenuDetailChallengeId = null;
            // Force the first paint to clear any rows left from a previous open.
            state.MenuListPainted = ListSlots;
            state.MenuDetailPainted = DetailSlots;
            Array.Clear(state.MenuRowChallengeIds);
            Array.Clear(state.MenuScoreRowSteamIds);
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

            if (IsDisabled(player, buttonId))
            {
                return;
            }

            switch (buttonId)
            {
                case HudMenu.BtnClose:
                    HudMenu.Close(player);
                    return;
                case HudMenu.BtnScorePrev:
                    state.ScoreboardPage = Math.Max(0, state.ScoreboardPage - 1);
                    break;
                case HudMenu.BtnScoreNext:
                    state.ScoreboardPage++;
                    break;
                case HudMenu.BtnListPrev:
                    state.MenuListPage = Math.Max(0, state.MenuListPage - 1);
                    break;
                case HudMenu.BtnListNext:
                    state.MenuListPage++;
                    break;
                case HudMenu.BtnScoreFilterAll:
                    state.ScoreboardFilter = ScoreboardFilter.All;
                    state.ScoreboardPage = 0;
                    break;
                case HudMenu.BtnScoreFilterOnline:
                    state.ScoreboardFilter = ScoreboardFilter.Online;
                    state.ScoreboardPage = 0;
                    break;
                case HudMenu.BtnScoreSortSolved:
                    state.ScoreboardSort = ScoreboardSort.Solved;
                    state.ScoreboardPage = 0;
                    break;
                case HudMenu.BtnScoreSortLifetime:
                    state.ScoreboardSort = ScoreboardSort.Lifetime;
                    state.ScoreboardPage = 0;
                    break;
                case HudMenu.BtnFilterAll:
                case HudMenu.BtnFilterSolved:
                case HudMenu.BtnFilterProgress:
                case HudMenu.BtnFilterEnding:
                case HudMenu.BtnFilterStarting:
                    state.MenuFilter = FilterFor(buttonId);
                    state.MenuListPage = 0;
                    break;
                case HudMenu.BtnDetailBack:
                    state.MenuDetailChallengeId = null;
                    break;
                default:
                    if (TrySelectListRow(state, buttonId)
                        || TrySelectScoreRow(player, state, buttonId))
                    {
                        break;
                    }

                    return;
            }

            Paint(player);
        }

        private static bool TrySelectListRow(PlayerState state, string buttonId)
        {
            if (!buttonId.StartsWith("ch-mrow-", StringComparison.Ordinal)
                || !int.TryParse(buttonId.AsSpan("ch-mrow-".Length), out int row)
                || (uint)row >= (uint)ListSlots
                || state.MenuRowChallengeIds[row] is not { Length: > 0 } challengeId)
            {
                return false;
            }

            state.MenuDetailChallengeId = string.Equals(
                state.MenuDetailChallengeId,
                challengeId,
                StringComparison.Ordinal)
                ? null
                : challengeId;
            return true;
        }

        private static bool TrySelectScoreRow(CCSPlayerController player, PlayerState state, string buttonId)
        {
            if (!buttonId.StartsWith("ch-srow-", StringComparison.Ordinal)
                || !int.TryParse(buttonId.AsSpan("ch-srow-".Length), out int row)
                || (uint)row >= (uint)ScoreSlots
                || state.MenuScoreRowSteamIds[row] is not { Length: > 0 } steamId)
            {
                return false;
            }

            // Same row again, or own row → back to the viewer.
            bool clear = SameSteam(steamId, state.MenuSubjectSteamId)
                || SameSteam(steamId, player.NetworkIDString);
            state.MenuSubjectSteamId = clear ? null : steamId;
            state.MenuDetailChallengeId = null;
            state.MenuListPage = 0;
            return true;
        }

        private static bool SameSteam(string? a, string? b) =>
            !string.IsNullOrEmpty(a)
            && !string.IsNullOrEmpty(b)
            && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool IsDisabled(CCSPlayerController player, string buttonId) =>
            buttonId is (HudMenu.BtnScorePrev or HudMenu.BtnScoreNext
                or HudMenu.BtnListPrev or HudMenu.BtnListNext
                or HudMenu.BtnFilterSolved or HudMenu.BtnFilterProgress
                or HudMenu.BtnFilterEnding or HudMenu.BtnFilterStarting)
            && CustomHud.HasClass(player, buttonId, "is-disabled");

        public static void WriteDefaults(CCSPlayerController player)
        {
            foreach (string name in ChromeVars)
            {
                CustomHud.SetText(player, Panel, name, string.Empty);
            }

            HudTheme.Apply(player, Panel);
            foreach (FilterDef filter in FilterDefs)
            {
                CustomHud.SetHasClass(player, filter.ButtonId, "active", false);
                CustomHud.SetHasClass(player, filter.ButtonId, "is-disabled", false);
            }

            CustomHud.SetHasClass(player, HudMenu.BtnScoreFilterAll, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreFilterOnline, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreSortSolved, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreSortLifetime, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScorePrev, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreNext, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnListPrev, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnListNext, "is-disabled", false);
            CustomHud.SetHasClass(player, EmptyLabelId, "is-off", true);

            for (int i = 0; i < ListSlots; i++)
            {
                ClearListSlot(player, i);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                ClearScoreSlot(player, i);
            }

            ClearDetailView(player, allSlots: true);

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
            MenuSubject subject = ResolveSubject(player, state);
            EnsureFilterAvailable(player, state, subject.Progress, schedule);
            HudTheme.Apply(player, Panel);
            PaintChrome(player, state, subject);
            PaintList(player, state, subject.Progress, schedule);
            PaintScoreboard(player, state, schedule);
            PaintDetail(player, state, subject.Progress, schedule);
        }

        private static MenuSubject ResolveSubject(CCSPlayerController viewer, PlayerState viewerState)
        {
            string? subjectId = viewerState.MenuSubjectSteamId;
            if (string.IsNullOrEmpty(subjectId) || SameSteam(subjectId, viewer.NetworkIDString))
            {
                viewerState.MenuSubjectSteamId = null;
                return new MenuSubject(viewerState, null);
            }

            foreach (CCSPlayerController human in Players.GetHumans())
            {
                if (!SameSteam(human.NetworkIDString, subjectId)
                    || Context.GetState(human) is not { } live)
                {
                    continue;
                }

                return new MenuSubject(live, human.PlayerName);
            }

            if (Context.Archive?.TryGet(subjectId) is { } saved)
            {
                return new MenuSubject(saved, saved.Username.Length > 0 ? saved.Username : subjectId);
            }

            viewerState.MenuSubjectSteamId = null;
            return new MenuSubject(viewerState, null);
        }

        private static void EnsureFilterAvailable(
            CCSPlayerController player,
            PlayerState session,
            PlayerState progress,
            RunningSchedule? schedule)
        {
            string? fallback = null;
            bool activeEmpty = false;
            foreach (FilterDef filter in FilterDefs)
            {
                int count = CountFilterEntries(progress, schedule, filter.Id);
                CustomHud.SetText(player, Panel, filter.Var, $"{Context.Text(player, filter.Key)} ({count})");
                CustomHud.SetHasClass(player, filter.ButtonId, "is-disabled", count == 0);
                if (count > 0)
                {
                    fallback ??= filter.Id;
                }

                if (filter.Id == session.MenuFilter)
                {
                    activeEmpty = count == 0;
                }
            }

            if (activeEmpty && fallback != null)
            {
                session.MenuFilter = fallback;
                session.MenuListPage = 0;
            }
        }

        /// <summary>Counts matching challenges without building display strings.</summary>
        private static int CountFilterEntries(PlayerState progress, RunningSchedule? schedule, string filter)
        {
            DateTime now = DateTime.UtcNow;
            string activeKey = schedule?.Key ?? string.Empty;
            switch (filter)
            {
                case FilterAll:
                    return Context.ChallengeMap.Count;
                case FilterSolved:
                    return CountScheduleWhere(schedule, c => ChallengeProgress.IsChallengeSolved(progress, activeKey, c));
                case FilterEnding:
                    return CountScheduleWindow(s => ParseDate(s.EndDate), now, EndingSoonWindow);
                case FilterStarting:
                    return CountScheduleWindow(s => ParseDate(s.StartDate), now, maxAhead: null);
                default:
                    return CountScheduleWhere(schedule, c => !ChallengeProgress.IsChallengeSolved(progress, activeKey, c));
            }
        }

        private static int CountScheduleWhere(RunningSchedule? schedule, Func<ChallengeDefinition, bool> predicate)
        {
            if (schedule == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (predicate(challenge))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountScheduleWindow(
            Func<ChallengeSchedule, DateTime?> dateOf,
            DateTime now,
            TimeSpan? maxAhead)
        {
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            int count = 0;
            foreach (ChallengeSchedule schedule in Context.ScheduleMap.Values)
            {
                if (dateOf(schedule) is not { } date || date <= now)
                {
                    continue;
                }

                if (maxAhead is { } window && date > now + window)
                {
                    continue;
                }

                foreach (string rawId in schedule.Challenges)
                {
                    string id = Schedules.ChallengeId(rawId);
                    if (seen.Add(id) && Context.ChallengeMap.ContainsKey(id))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static void PaintChrome(CCSPlayerController player, PlayerState state, MenuSubject subject)
        {
            foreach ((string varName, string key) in ChromeLabels)
            {
                CustomHud.SetText(player, Panel, varName, Context.Text(player, key));
            }

            string listTitle = subject.Name is { Length: > 0 } name
                ? Context.Text(player, "hud.menu.viewing", ("{name}", name))
                : Context.Text(player, "hud.menu.list");
            CustomHud.SetText(player, Panel, "list_title", listTitle);

            string prev = Context.Text(player, "hud.menu.prev");
            string next = Context.Text(player, "hud.menu.next");
            CustomHud.SetText(player, Panel, "score_prev", prev);
            CustomHud.SetText(player, Panel, "score_next", next);
            CustomHud.SetText(player, Panel, "detail_back", Context.Text(player, "hud.menu.detail.back"));

            bool sortLifetime = state.ScoreboardSort == ScoreboardSort.Lifetime;
            CustomHud.SetHasClass(player, ScoreHeadCurId, "sort-active", !sortLifetime);
            CustomHud.SetHasClass(player, ScoreHeadTotId, "sort-active", sortLifetime);
            foreach (FilterDef filter in FilterDefs)
            {
                CustomHud.SetHasClass(player, filter.ButtonId, "active", filter.Id == state.MenuFilter);
            }

            CustomHud.SetHasClass(
                player,
                HudMenu.BtnScoreFilterAll,
                "active",
                state.ScoreboardFilter == ScoreboardFilter.All);
            CustomHud.SetHasClass(
                player,
                HudMenu.BtnScoreFilterOnline,
                "active",
                state.ScoreboardFilter == ScoreboardFilter.Online);
            CustomHud.SetHasClass(
                player,
                HudMenu.BtnScoreSortSolved,
                "active",
                state.ScoreboardSort == ScoreboardSort.Solved);
            CustomHud.SetHasClass(
                player,
                HudMenu.BtnScoreSortLifetime,
                "active",
                sortLifetime);
        }

        private static void PaintList(
            CCSPlayerController player,
            PlayerState session,
            PlayerState progress,
            RunningSchedule? schedule)
        {
            List<Entry> entries = BuildEntries(player, session, progress, schedule);
            bool listEmpty = entries.Count == 0;
            CustomHud.SetText(
                player,
                Panel,
                "menu_empty",
                listEmpty ? Context.Text(player, "hud.menu.empty") : string.Empty);
            CustomHud.SetHasClass(player, EmptyLabelId, "is-off", !listEmpty);
            int pages = Math.Max(1, (entries.Count + ListSlots - 1) / ListSlots);
            session.MenuListPage = Math.Clamp(session.MenuListPage, 0, pages - 1);
            SetTitlePage(
                player,
                "list_page",
                HudMenu.BtnListPrev,
                HudMenu.BtnListNext,
                session.MenuListPage,
                pages);
            if (listEmpty)
            {
                ClearUnusedListSlots(player, session, painted: 0);
                return;
            }

            string? scheduleKey = schedule?.Key;
            // Progress/Solved only contain the active schedule; other filters may include extras.
            bool scheduleOnly = session.MenuFilter is FilterProgress or FilterSolved;
            string? detailId = session.MenuDetailChallengeId;
            int pageStart = session.MenuListPage * ListSlots;
            int painted = Math.Min(ListSlots, entries.Count - pageStart);

            for (int i = 0; i < painted; i++)
            {
                Entry entry = entries[pageStart + i];
                ChallengeDefinition challenge = entry.Challenge;
                session.MenuRowChallengeIds[i] = challenge.Id;
                bool inSchedule = scheduleOnly
                    || (schedule != null && schedule.Challenges.Contains(challenge));
                string rowId = ListRowIds[i];
                CustomHud.SetText(player, Panel, ListTitleVars[i], Titles.For(player, challenge.Title));
                CustomHud.SetText(player, Panel, ListWhenVars[i], entry.When);
                CustomHud.SetText(player, Panel, ListMetaVars[i], Context.FormatPercent(player, entry.Percent));
                CustomHud.SetStepPercent(player, ListFillIds[i], entry.Percent);
                CustomHud.SetHasClass(player, rowId, "empty", false);
                CustomHud.SetHasClass(player, rowId, "is-off", false);
                CustomHud.SetHasClass(
                    player,
                    rowId,
                    "is-selected",
                    detailId != null && string.Equals(detailId, challenge.Id, StringComparison.Ordinal));
                ChallengeCardPaint.Slots card = ListCardSlots[i];
                ChallengeCardPaint.PaintTasks(
                    player,
                    progress,
                    inSchedule ? scheduleKey : null,
                    challenge,
                    card);
                ChallengeCardPaint.PaintCompleters(
                    player,
                    inSchedule ? schedule : null,
                    challenge,
                    card);
            }

            ClearUnusedListSlots(player, session, painted);
        }

        private static void ClearUnusedListSlots(CCSPlayerController player, PlayerState session, int painted)
        {
            int clearUntil = session.MenuListPainted;
            string?[] ids = session.MenuRowChallengeIds;
            for (int i = painted; i < clearUntil; i++)
            {
                ids[i] = null;
                ClearListSlot(player, i);
            }

            session.MenuListPainted = painted;
        }

        private static void ClearListSlot(CCSPlayerController player, int row)
        {
            CustomHud.SetText(player, Panel, ListTitleVars[row], string.Empty);
            CustomHud.SetText(player, Panel, ListWhenVars[row], string.Empty);
            CustomHud.SetText(player, Panel, ListMetaVars[row], string.Empty);
            CustomHud.SetStepPercent(player, ListFillIds[row], 0);
            string rowId = ListRowIds[row];
            CustomHud.SetHasClass(player, rowId, "empty", true);
            CustomHud.SetHasClass(player, rowId, "is-off", false);
            CustomHud.SetHasClass(player, rowId, "is-selected", false);
            ChallengeCardPaint.Slots slots = ListCardSlots[row];
            ChallengeCardPaint.ClearTasks(player, slots);
            ChallengeCardPaint.ClearCompleters(player, slots);
        }

        private static void PaintDetail(
            CCSPlayerController player,
            PlayerState session,
            PlayerState progress,
            RunningSchedule? schedule)
        {
            string? challengeId = session.MenuDetailChallengeId;
            if (string.IsNullOrEmpty(challengeId))
            {
                ClearDetailView(player);
                return;
            }

            if (!Context.ChallengeMap.TryGetValue(challengeId, out ChallengeDefinition? challenge))
            {
                session.MenuDetailChallengeId = null;
                ClearDetailView(player);
                return;
            }

            CustomHud.SetHasClass(player, ScoreColumnId, "is-off", true);
            CustomHud.SetHasClass(player, DetailColumnId, "is-off", false);
            CustomHud.SetText(player, Panel, "detail_title", Titles.For(player, challenge.Title));

            List<ChallengeTask> tasks = TaskRuleSummary.DetailInOrder(challenge);
            string? scheduleKey = schedule != null && InSchedule(schedule, challenge.Id)
                ? schedule.Key
                : null;
            int painted = Math.Min(tasks.Count, DetailSlots);

            for (int i = 0; i < painted; i++)
            {
                ChallengeTask task = tasks[i];
                bool broken = !task.Visible;
                string title;
                bool done = false;
                if (broken)
                {
                    title = Titles.For(player, task.Title);
                }
                else
                {
                    int amount = Math.Max(1, task.Amount);
                    int count = ChallengeProgress.GetTaskCount(progress, scheduleKey, challenge.Id, task);
                    done = scheduleKey != null
                        && ChallengeProgress.IsTaskComplete(progress, scheduleKey, challenge.Id, task);
                    title = Titles.For(player, task.Title, count, amount);
                }

                string rowId = DetailRowIds[i];
                CustomHud.SetText(player, Panel, DetailTitleVars[i], title);
                PaintDetailRules(player, i, TaskRuleSummary.Parts(player, task));
                CustomHud.SetHasClass(player, rowId, "empty", false);
                CustomHud.SetHasClass(player, rowId, "is-off", false);
                CustomHud.SetHasClass(player, rowId, "is-done", done);
                CustomHud.SetHasClass(player, rowId, "is-broken", broken);
            }

            int clearUntil = session.MenuDetailPainted;
            for (int i = painted; i < clearUntil; i++)
            {
                ClearDetailSlot(player, i);
            }

            session.MenuDetailPainted = painted;
        }

        private static void PaintDetailRules(CCSPlayerController player, int row, List<string> parts)
        {
            int overflow = parts.Count > DetailRuleSlots ? parts.Count - (DetailRuleSlots - 1) : 0;
            int shown = overflow > 0 ? DetailRuleSlots - 1 : parts.Count;

            for (int slot = 0; slot < DetailRuleSlots; slot++)
            {
                if (slot == DetailRuleSlots - 1 && overflow > 0)
                {
                    SetDetailRule(
                        player,
                        row,
                        slot,
                        string.Join(" · ", parts.GetRange(shown, parts.Count - shown)),
                        empty: false);
                    continue;
                }

                if (slot < shown)
                {
                    SetDetailRule(player, row, slot, parts[slot], empty: false);
                    continue;
                }

                // Transparent partner so an odd last chip stays half-width.
                if (shown > 0 && shown % 2 == 1 && slot == shown)
                {
                    SetDetailRule(player, row, slot, string.Empty, empty: true);
                    continue;
                }

                ClearDetailRule(player, row, slot);
            }
        }

        private static void SetDetailRule(
            CCSPlayerController player,
            int row,
            int slot,
            string text,
            bool empty)
        {
            string ruleId = DetailRuleIds[row][slot];
            CustomHud.SetText(player, Panel, DetailRuleVars[row][slot], text);
            CustomHud.SetHasClass(player, ruleId, "is-off", false);
            CustomHud.SetHasClass(player, ruleId, "is-empty", empty);
        }

        private static void ClearDetailRule(CCSPlayerController player, int row, int slot)
        {
            string ruleId = DetailRuleIds[row][slot];
            CustomHud.SetText(player, Panel, DetailRuleVars[row][slot], string.Empty);
            CustomHud.SetHasClass(player, ruleId, "is-off", true);
            CustomHud.SetHasClass(player, ruleId, "is-empty", false);
        }

        private static void ClearDetailView(CCSPlayerController player, bool allSlots = false)
        {
            CustomHud.SetHasClass(player, ScoreColumnId, "is-off", false);
            CustomHud.SetHasClass(player, DetailColumnId, "is-off", true);
            CustomHud.SetText(player, Panel, "detail_title", string.Empty);

            int clearUntil = DetailSlots;
            if (!allSlots && Context.GetState(player) is { } state)
            {
                clearUntil = state.MenuDetailPainted;
                state.MenuDetailPainted = 0;
            }

            for (int i = 0; i < clearUntil; i++)
            {
                ClearDetailSlot(player, i);
            }
        }

        private static void ClearDetailSlot(CCSPlayerController player, int row)
        {
            CustomHud.SetText(player, Panel, DetailTitleVars[row], string.Empty);
            for (int slot = 0; slot < DetailRuleSlots; slot++)
            {
                ClearDetailRule(player, row, slot);
            }

            string rowId = DetailRowIds[row];
            CustomHud.SetHasClass(player, rowId, "empty", true);
            CustomHud.SetHasClass(player, rowId, "is-off", false);
            CustomHud.SetHasClass(player, rowId, "is-done", false);
            CustomHud.SetHasClass(player, rowId, "is-broken", false);
        }

        private static bool InSchedule(RunningSchedule schedule, string challengeId)
        {
            List<ChallengeDefinition> challenges = schedule.Challenges;
            for (int i = 0; i < challenges.Count; i++)
            {
                if (string.Equals(challenges[i].Id, challengeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static List<Entry> BuildEntries(
            CCSPlayerController player,
            PlayerState session,
            PlayerState progress,
            RunningSchedule? schedule)
        {
            DateTime now = DateTime.UtcNow;
            string activeKey = schedule?.Key ?? string.Empty;
            int Percent(ChallengeDefinition c) =>
                schedule != null && schedule.Challenges.Contains(c)
                    ? ChallengeProgress.GetChallengePercent(progress, activeKey, c)
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

            switch (session.MenuFilter)
            {
                case FilterAll:
                    return OrderEntries(Context.ChallengeMap.Values.Select(Make), byTiming: true);
                case FilterSolved:
                    if (schedule == null)
                    {
                        return [];
                    }

                    return OrderEntries(
                        schedule.Challenges
                            .Where(c => ChallengeProgress.IsChallengeSolved(progress, activeKey, c))
                            .Select(Make),
                        byTiming: false);
                case FilterEnding:
                    return ScheduleEntries(s => ParseDate(s.EndDate), Make, now, EndingSoonWindow);
                case FilterStarting:
                    return ScheduleEntries(s => ParseDate(s.StartDate), Make, now, maxAhead: null);
                default:
                    if (schedule == null)
                    {
                        return [];
                    }

                    return OrderEntries(
                        schedule.Challenges
                            .Where(c => !ChallengeProgress.IsChallengeSolved(progress, activeKey, c))
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
            DateTime now,
            TimeSpan? maxAhead)
        {
            List<Entry> entries = [];
            HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
            foreach (ChallengeSchedule schedule in Context.ScheduleMap.Values)
            {
                if (dateOf(schedule) is not { } date || date <= now)
                {
                    continue;
                }

                if (maxAhead is { } window && date > now + window)
                {
                    continue;
                }

                foreach (string rawId in schedule.Challenges)
                {
                    string id = Schedules.ChallengeId(rawId);
                    if (!seen.Add(id) || !Context.ChallengeMap.TryGetValue(id, out ChallengeDefinition? challenge))
                    {
                        continue;
                    }

                    Entry entry = make(challenge);
                    entries.Add(entry with { SortTime = date });
                }
            }

            // Percent desc, then schedule time, then title A–Z.
            return entries
                .OrderByDescending(e => e.Percent)
                .ThenBy(e => e.SortTime)
                .ThenBy(SortTitle, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static DateTime? ParseDate(string value) =>
            Schedules.TryParseDate(value, out DateTime date) ? date : null;

        /// <summary>Writes <c>({page} / {pages})</c> and enables/disables title-bar prev/next.</summary>
        private static void SetTitlePage(
            CCSPlayerController player,
            string pageVar,
            string prevId,
            string nextId,
            int page,
            int pages)
        {
            CustomHud.SetText(player, Panel, pageVar, Context.FormatPage(player, page + 1, pages));
            SetPageNav(player, prevId, nextId, page, pages);
        }

        private static void SetPageNav(
            CCSPlayerController player,
            string prevId,
            string nextId,
            int page,
            int pages)
        {
            CustomHud.SetHasClass(player, prevId, "is-disabled", page <= 0 || pages <= 1);
            CustomHud.SetHasClass(player, nextId, "is-disabled", page >= pages - 1 || pages <= 1);
        }

        private static void PaintScoreboard(CCSPlayerController player, PlayerState state, RunningSchedule? schedule)
        {
            int available = schedule?.Challenges.Count ?? 0;
            List<ScoreEntry> board = BuildScoreboard(state, schedule);

            bool byLifetime = state.ScoreboardSort == ScoreboardSort.Lifetime;
            board.Sort((a, b) =>
            {
                int primary = byLifetime ? b.Total.CompareTo(a.Total) : b.Current.CompareTo(a.Current);
                if (primary != 0)
                {
                    return primary;
                }

                int secondary = byLifetime ? b.Current.CompareTo(a.Current) : b.Total.CompareTo(a.Total);
                return secondary != 0
                    ? secondary
                    : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });

            int pages = Math.Max(1, (board.Count + ScoreSlots - 1) / ScoreSlots);
            state.ScoreboardPage = Math.Clamp(state.ScoreboardPage, 0, pages - 1);
            SetTitlePage(
                player,
                "score_page",
                HudMenu.BtnScorePrev,
                HudMenu.BtnScoreNext,
                state.ScoreboardPage,
                pages);

            string? selfSteam = player.NetworkIDString;
            int selfIndex = board.FindIndex(x =>
                x.Player == player || SameSteam(x.SteamId, selfSteam));
            if (selfIndex >= 0)
            {
                ScoreEntry self = board[selfIndex];
                CustomHud.SetText(player, Panel, "spin_name", player.PlayerName);
                CustomHud.SetText(player, Panel, "spin_cur", Context.FormatCount(player, self.Current, available));
                CustomHud.SetText(player, Panel, "spin_tot", Context.FormatNumber(player, self.Total));
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

            string? viewingSteam = state.MenuSubjectSteamId;
            int pageStart = state.ScoreboardPage * ScoreSlots;
            for (int i = 0; i < ScoreSlots; i++)
            {
                int index = pageStart + i;
                if (index >= board.Count)
                {
                    state.MenuScoreRowSteamIds[i] = null;
                    ClearScoreSlot(player, i);
                    continue;
                }

                ScoreEntry entry = board[index];
                string rowId = ScoreRowIds[i];
                state.MenuScoreRowSteamIds[i] = entry.SteamId;
                CustomHud.SetText(player, Panel, $"s{i}_rank", Context.FormatRank(player, index + 1));
                CustomHud.SetText(player, Panel, $"s{i}_name", entry.Name);
                CustomHud.SetText(player, Panel, $"s{i}_cur", Context.FormatCount(player, entry.Current, available));
                CustomHud.SetText(player, Panel, $"s{i}_tot", Context.FormatNumber(player, entry.Total));
                CustomHud.SetHasClass(player, rowId, "empty", false);
                CustomHud.SetHasClass(player, rowId, "is-off", false);
                CustomHud.SetHasClass(
                    player,
                    rowId,
                    "is-self",
                    entry.Player == player || SameSteam(entry.SteamId, selfSteam));
                CustomHud.SetHasClass(player, rowId, "is-viewing", SameSteam(entry.SteamId, viewingSteam));
                CustomHud.SetHasClass(player, rowId, "alt", (index & 1) == 1);
            }
        }

        private static List<ScoreEntry> BuildScoreboard(
            PlayerState viewerState,
            RunningSchedule? schedule)
        {
            Dictionary<string, ScoreEntry> bySteam = new(StringComparer.OrdinalIgnoreCase);

            foreach (CCSPlayerController human in Players.GetHumans())
            {
                string? steam = human.NetworkIDString;
                if (string.IsNullOrEmpty(steam))
                {
                    continue;
                }

                PlayerState? s = Context.GetState(human);
                int current = s != null && schedule != null
                    ? ChallengeProgress.CountSolvedInSchedule(s, schedule)
                    : 0;
                int total = s?.Statistics.AmountChallengesSolved ?? 0;
                bySteam[steam] = new ScoreEntry(human.PlayerName, steam, current, total, human);
            }

            if (viewerState.ScoreboardFilter != ScoreboardFilter.All)
            {
                return bySteam.Values.ToList();
            }

            if (Context.Archive is not { } archive)
            {
                return bySteam.Values.ToList();
            }

            archive.EnsureOfflineScores(schedule?.Key, schedule);
            foreach (PlayerArchive.OfflineScore saved in archive.OfflineScores)
            {
                if (bySteam.ContainsKey(saved.SteamId))
                {
                    continue;
                }

                bySteam[saved.SteamId] = new ScoreEntry(saved.Name, saved.SteamId, saved.Current, saved.Total, null);
            }

            return bySteam.Values.ToList();
        }

        private static void ClearScoreSlot(CCSPlayerController player, int index)
        {
            CustomHud.SetText(player, Panel, $"s{index}_rank", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_name", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_cur", string.Empty);
            CustomHud.SetText(player, Panel, $"s{index}_tot", string.Empty);
            string rowId = ScoreRowIds[index];
            CustomHud.SetHasClass(player, rowId, "empty", true);
            CustomHud.SetHasClass(player, rowId, "is-off", true);
            CustomHud.SetHasClass(player, rowId, "is-self", false);
            CustomHud.SetHasClass(player, rowId, "is-viewing", false);
            CustomHud.SetHasClass(player, rowId, "alt", false);
        }
    }
}
