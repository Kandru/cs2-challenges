using CounterStrikeSharp.API.Core;
using Challenges.Classes;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>
    /// Fullscreen challenge menu: compact challenge cards (header + 50/50 tasks|completers,
    /// 5 per page, no list scroll), a scoreboard, and a challenge detail column.
    /// </summary>
    public static class Menu
    {
        public const string Panel = "Menu";
        public const int ListSlots = 5;
        public const int ScoreSlots = 12;
        public const int DetailSlots = 5;
        public const int MaxPageSize = 5;
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

        private static readonly string[] Filters =
            [FilterAll, FilterSolved, FilterProgress, FilterEnding, FilterStarting];
        private static readonly string[] ChromeVars =
        [
            "menu_title", "list_title", "menu_page", "menu_prev", "menu_next",
            "menu_f_all", "menu_f_solved", "menu_f_progress", "menu_f_ending", "menu_f_starting",
            "menu_empty", "menu_tasks_h", "menu_by_h",
            "score_title", "score_page", "score_prev", "score_next",
            "score_f_all", "score_f_online", "score_s_solved", "score_s_lifetime",
            "score_h_rank", "score_h_name", "score_h_cur", "score_h_tot",
            "spin_name", "spin_cur", "spin_tot", "spin_rank", "spin_l_cur", "spin_l_tot",
            "detail_title", "detail_page", "detail_prev", "detail_next", "detail_back",
        ];

        private static readonly (string Var, string Key)[] ChromeLabels =
        [
            ("menu_title", "hud.menu.title"),
            ("menu_f_all", "hud.menu.filter.all"),
            ("menu_f_solved", "hud.menu.filter.solved"),
            ("menu_f_progress", "hud.menu.filter.progress"),
            ("menu_f_ending", "hud.menu.filter.ending"),
            ("menu_f_starting", "hud.menu.filter.starting"),
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

        public static string ListRowId(int index) => $"ch-mrow-{index}";
        public static string ListFillId(int index) => $"ch-mfill-{index}";
        public static string ListTaskId(int row, int task) => $"ch-mtask-{row}-{task}";
        public static string ListCompleterId(int row, int slot) => $"ch-mby-{row}-{slot}";
        public static string ScoreRowId(int index) => $"ch-srow-{index}";
        public static string DetailRowId(int index) => $"ch-drow-{index}";
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

            state.ScoreboardPage = 0;
            state.MenuFilter = FilterProgress;
            state.ScoreboardSort = ScoreboardSort.Solved;
            state.ScoreboardFilter = ScoreboardFilter.Online;
            SetSubject(state, null);
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
                    SetFilter(state, FilterAll);
                    break;
                case HudMenu.BtnFilterSolved:
                    SetFilter(state, FilterSolved);
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
                case HudMenu.BtnDetailBack:
                    ClearDetail(state);
                    break;
                case HudMenu.BtnDetailPrev:
                    state.MenuDetailPage = Math.Max(0, state.MenuDetailPage - 1);
                    break;
                case HudMenu.BtnDetailNext:
                    state.MenuDetailPage++;
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

            if (string.Equals(state.MenuDetailChallengeId, challengeId, StringComparison.Ordinal))
            {
                ClearDetail(state);
            }
            else
            {
                state.MenuDetailChallengeId = challengeId;
                state.MenuDetailPage = 0;
            }

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
            SetSubject(state, clear ? null : steamId);
            return true;
        }

        private static void ClearDetail(PlayerState state)
        {
            state.MenuDetailChallengeId = null;
            state.MenuDetailPage = 0;
        }

        private static void SetSubject(PlayerState state, string? steamId)
        {
            state.MenuSubjectSteamId = steamId;
            state.MenuPage = 0;
            ClearDetail(state);
        }

        private static bool SameSteam(string? a, string? b) =>
            !string.IsNullOrEmpty(a)
            && !string.IsNullOrEmpty(b)
            && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool IsDisabled(CCSPlayerController player, string buttonId) =>
            buttonId is (HudMenu.BtnPrev or HudMenu.BtnNext
                or HudMenu.BtnScorePrev or HudMenu.BtnScoreNext
                or HudMenu.BtnDetailPrev or HudMenu.BtnDetailNext
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
            foreach (string filter in Filters)
            {
                CustomHud.SetHasClass(player, FilterButtonId(filter), "active", false);
                CustomHud.SetHasClass(player, FilterButtonId(filter), "is-disabled", false);
            }

            CustomHud.SetHasClass(player, HudMenu.BtnScoreFilterAll, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreFilterOnline, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreSortSolved, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreSortLifetime, "active", false);
            CustomHud.SetHasClass(player, HudMenu.BtnPrev, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnNext, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScorePrev, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnScoreNext, "is-disabled", false);
            CustomHud.SetHasClass(player, EmptyLabelId, "is-off", true);

            for (int i = 0; i < ListSlots; i++)
            {
                ClearListSlot(player, i, off: false);
            }

            for (int i = 0; i < ScoreSlots; i++)
            {
                ClearScoreSlot(player, i);
            }

            ClearDetailView(player);

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

            if (OfflinePlayers.TryFind(subjectId) is { } saved)
            {
                return new MenuSubject(saved.State, saved.Name);
            }

            viewerState.MenuSubjectSteamId = null;
            return new MenuSubject(viewerState, null);
        }

        private static void SetFilter(PlayerState state, string filter)
        {
            state.MenuFilter = filter;
            state.MenuPage = 0;
        }

        private static string FilterButtonId(string filter) => filter switch
        {
            FilterAll => HudMenu.BtnFilterAll,
            FilterSolved => HudMenu.BtnFilterSolved,
            FilterEnding => HudMenu.BtnFilterEnding,
            FilterStarting => HudMenu.BtnFilterStarting,
            _ => HudMenu.BtnFilterProgress,
        };

        private static void EnsureFilterAvailable(
            CCSPlayerController player,
            PlayerState session,
            PlayerState progress,
            RunningSchedule? schedule)
        {
            string? fallback = null;
            bool activeEmpty = false;
            foreach (string filter in Filters)
            {
                int count = CountFilterEntries(progress, schedule, filter);
                CustomHud.SetHasClass(player, FilterButtonId(filter), "is-disabled", count == 0);
                if (count > 0)
                {
                    fallback ??= filter;
                }

                if (filter == session.MenuFilter)
                {
                    activeEmpty = count == 0;
                }
            }

            if (activeEmpty && fallback != null)
            {
                SetFilter(session, fallback);
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
            foreach (string varName in new[] { "menu_prev", "score_prev", "detail_prev" })
            {
                CustomHud.SetText(player, Panel, varName, prev);
            }

            foreach (string varName in new[] { "menu_next", "score_next", "detail_next" })
            {
                CustomHud.SetText(player, Panel, varName, next);
            }

            CustomHud.SetText(player, Panel, "detail_back", Context.Text(player, "hud.menu.detail.back"));

            bool sortLifetime = state.ScoreboardSort == ScoreboardSort.Lifetime;
            CustomHud.SetHasClass(player, ScoreHeadCurId, "sort-active", !sortLifetime);
            CustomHud.SetHasClass(player, ScoreHeadTotId, "sort-active", sortLifetime);
            foreach (string filter in Filters)
            {
                CustomHud.SetHasClass(player, FilterButtonId(filter), "active", filter == state.MenuFilter);
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
            int pageSize = ListPageSize;
            int pages = Math.Max(1, (entries.Count + pageSize - 1) / pageSize);
            session.MenuPage = Math.Clamp(session.MenuPage, 0, pages - 1);
            SetTitlePage(player, "menu_page", HudMenu.BtnPrev, HudMenu.BtnNext, session.MenuPage, pages);
            bool listEmpty = entries.Count == 0;
            CustomHud.SetText(
                player,
                Panel,
                "menu_empty",
                listEmpty ? Context.Text(player, "hud.menu.empty") : string.Empty);
            CustomHud.SetHasClass(player, EmptyLabelId, "is-off", !listEmpty);

            string? scheduleKey = schedule?.Key;

            for (int i = 0; i < ListSlots; i++)
            {
                bool offPage = i >= pageSize;
                int index = session.MenuPage * pageSize + i;
                if (offPage || index >= entries.Count)
                {
                    session.MenuRowChallengeIds[i] = null;
                    ClearListSlot(player, i, off: offPage);
                    continue;
                }

                Entry entry = entries[index];
                session.MenuRowChallengeIds[i] = entry.Challenge.Id;
                bool inSchedule = schedule != null && schedule.Challenges.Contains(entry.Challenge);
                bool selected = string.Equals(
                    session.MenuDetailChallengeId,
                    entry.Challenge.Id,
                    StringComparison.Ordinal);
                CustomHud.SetText(player, Panel, $"m{i}_title", Titles.For(player, entry.Challenge.Title));
                CustomHud.SetText(player, Panel, $"m{i}_when", entry.When);
                CustomHud.SetText(player, Panel, $"m{i}_meta", Context.FormatPercent(player, entry.Percent));
                CustomHud.SetStepPercent(player, ListFillId(i), entry.Percent);
                CustomHud.SetHasClass(player, ListRowId(i), "empty", false);
                CustomHud.SetHasClass(player, ListRowId(i), "is-off", false);
                CustomHud.SetHasClass(player, ListRowId(i), "is-selected", selected);
                PaintTasks(player, progress, inSchedule ? scheduleKey : null, entry.Challenge, i);
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
            CustomHud.SetHasClass(player, ListRowId(row), "is-selected", false);
            ChallengeCardPaint.Slots slots = CardSlots(row);
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
            if (string.IsNullOrEmpty(challengeId)
                || !Context.ChallengeMap.TryGetValue(challengeId, out ChallengeDefinition? challenge))
            {
                if (!string.IsNullOrEmpty(challengeId))
                {
                    ClearDetail(session);
                }

                ClearDetailView(player);
                return;
            }

            CustomHud.SetHasClass(player, ScoreColumnId, "is-off", true);
            CustomHud.SetHasClass(player, DetailColumnId, "is-off", false);
            CustomHud.SetText(player, Panel, "detail_title", Titles.For(player, challenge.Title));

            List<ChallengeTask> tasks = TaskRuleSummary.VisibleInOrder(challenge);
            int pages = Math.Max(1, (tasks.Count + DetailSlots - 1) / DetailSlots);
            session.MenuDetailPage = Math.Clamp(session.MenuDetailPage, 0, pages - 1);
            SetTitlePage(
                player,
                "detail_page",
                HudMenu.BtnDetailPrev,
                HudMenu.BtnDetailNext,
                session.MenuDetailPage,
                pages);

            string? scheduleKey = schedule != null && InSchedule(schedule, challenge.Id)
                ? schedule.Key
                : null;
            int pageStart = session.MenuDetailPage * DetailSlots;

            for (int i = 0; i < DetailSlots; i++)
            {
                int index = pageStart + i;
                if (index >= tasks.Count)
                {
                    ClearDetailSlot(player, i);
                    continue;
                }

                ChallengeTask task = tasks[index];
                int amount = Math.Max(1, task.Amount);
                int count = ChallengeProgress.GetTaskCount(progress, scheduleKey, challenge.Id, task);
                bool done = scheduleKey != null
                    && ChallengeProgress.IsTaskComplete(progress, scheduleKey, challenge.Id, task);
                CustomHud.SetText(
                    player,
                    Panel,
                    $"d{i}_title",
                    Titles.For(player, task.Title, count, amount));
                CustomHud.SetText(
                    player,
                    Panel,
                    $"d{i}_rules",
                    TaskRuleSummary.Format(player, challenge, task));
                CustomHud.SetHasClass(player, DetailRowId(i), "empty", false);
                CustomHud.SetHasClass(player, DetailRowId(i), "is-off", false);
                CustomHud.SetHasClass(player, DetailRowId(i), "is-done", done);
            }
        }

        private static void ClearDetailView(CCSPlayerController player)
        {
            CustomHud.SetHasClass(player, ScoreColumnId, "is-off", false);
            CustomHud.SetHasClass(player, DetailColumnId, "is-off", true);
            CustomHud.SetText(player, Panel, "detail_title", string.Empty);
            CustomHud.SetText(player, Panel, "detail_page", string.Empty);
            SetPageNav(player, HudMenu.BtnDetailPrev, HudMenu.BtnDetailNext, page: 0, pages: 1);
            for (int i = 0; i < DetailSlots; i++)
            {
                ClearDetailSlot(player, i);
            }
        }

        private static void ClearDetailSlot(CCSPlayerController player, int row)
        {
            CustomHud.SetText(player, Panel, $"d{row}_title", string.Empty);
            CustomHud.SetText(player, Panel, $"d{row}_rules", string.Empty);
            CustomHud.SetHasClass(player, DetailRowId(row), "empty", true);
            CustomHud.SetHasClass(player, DetailRowId(row), "is-off", false);
            CustomHud.SetHasClass(player, DetailRowId(row), "is-done", false);
        }

        private static bool InSchedule(RunningSchedule schedule, string challengeId)
        {
            foreach (ChallengeDefinition entry in schedule.Challenges)
            {
                if (string.Equals(entry.Id, challengeId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
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
            for (int i = 0; i < ScoreSlots; i++)
            {
                int index = state.ScoreboardPage * ScoreSlots + i;
                if (index >= board.Count)
                {
                    state.MenuScoreRowSteamIds[i] = null;
                    ClearScoreSlot(player, i);
                    continue;
                }

                ScoreEntry entry = board[index];
                state.MenuScoreRowSteamIds[i] = entry.SteamId;
                CustomHud.SetText(player, Panel, $"s{i}_rank", Context.FormatRank(player, index + 1));
                CustomHud.SetText(player, Panel, $"s{i}_name", entry.Name);
                CustomHud.SetText(player, Panel, $"s{i}_cur", Context.FormatCount(player, entry.Current, available));
                CustomHud.SetText(player, Panel, $"s{i}_tot", Context.FormatNumber(player, entry.Total));
                CustomHud.SetHasClass(player, ScoreRowId(i), "empty", false);
                CustomHud.SetHasClass(player, ScoreRowId(i), "is-off", false);
                CustomHud.SetHasClass(player, ScoreRowId(i), "is-self",
                    entry.Player == player || SameSteam(entry.SteamId, selfSteam));
                CustomHud.SetHasClass(player, ScoreRowId(i), "is-viewing",
                    SameSteam(entry.SteamId, viewingSteam));
                CustomHud.SetHasClass(player, ScoreRowId(i), "alt", index % 2 == 1);
            }
        }

        private static List<ScoreEntry> BuildScoreboard(
            PlayerState viewerState,
            RunningSchedule? schedule)
        {
            Dictionary<string, ScoreEntry> bySteam = new(StringComparer.OrdinalIgnoreCase);

            foreach (CCSPlayerController human in Players.GetHumans())
            {
                PlayerState? s = Context.GetState(human);
                int current = s != null && schedule != null
                    ? ChallengeProgress.CountSolvedInSchedule(s, schedule)
                    : 0;
                int total = s?.Statistics.AmountChallengesSolved ?? 0;
                string steam = human.NetworkIDString ?? human.PlayerName;
                bySteam[steam] = new ScoreEntry(human.PlayerName, steam, current, total, human);
            }

            if (viewerState.ScoreboardFilter != ScoreboardFilter.All)
            {
                return bySteam.Values.ToList();
            }

            foreach (OfflinePlayers.SavedPlayer saved in OfflinePlayers.LoadAll())
            {
                if (bySteam.ContainsKey(saved.SteamId))
                {
                    continue;
                }

                bySteam[saved.SteamId] = new ScoreEntry(
                    saved.Name,
                    saved.SteamId,
                    schedule != null
                        ? ChallengeProgress.CountSolvedInSchedule(saved.State, schedule)
                        : 0,
                    saved.State.Statistics.AmountChallengesSolved,
                    null);
            }

            return bySteam.Values.ToList();
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
            CustomHud.SetHasClass(player, ScoreRowId(index), "is-viewing", false);
            CustomHud.SetHasClass(player, ScoreRowId(index), "alt", false);
        }
    }
}
