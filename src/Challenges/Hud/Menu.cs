using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>
    /// Fullscreen challenge menu: paginated challenge cards (4/page, header + 50/50 tasks|completers)
    /// and a paginated scoreboard.
    /// </summary>
    public static class Menu
    {
        public const string Panel = "Menu";
        public const int ListSlots = 4;
        public const int ScoreSlots = 12;
        public const string FilterAll = "all";
        public const string FilterSolved = "solved";
        public const string FilterProgress = "progress";
        public const string FilterEnding = "ending";
        public const string FilterStarting = "starting";
        private const string ScoreHeadCurId = "ch-score-h-cur";
        private const string ScoreHeadTotId = "ch-score-h-tot";
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

        private static string FilterFor(string buttonId) => buttonId switch
        {
            HudMenu.BtnFilterAll => FilterAll,
            HudMenu.BtnFilterSolved => FilterSolved,
            HudMenu.BtnFilterProgress => FilterProgress,
            HudMenu.BtnFilterEnding => FilterEnding,
            HudMenu.BtnFilterStarting => FilterStarting,
            _ => FilterProgress,
        };

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
        private static readonly string[] ScoreRankVars = Ids("s", ScoreSlots, "_rank");
        private static readonly string[] ScoreNameVars = Ids("s", ScoreSlots, "_name");
        private static readonly string[] ScoreCurVars = Ids("s", ScoreSlots, "_cur");
        private static readonly string[] ScoreTotVars = Ids("s", ScoreSlots, "_tot");
        private static readonly ChallengeCardPaint.Slots[] ListCardSlots = CreateListCardSlots();

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

        private static ChallengeCardPaint.Slots[] CreateListCardSlots()
        {
            ChallengeCardPaint.Slots[] slots = new ChallengeCardPaint.Slots[ListSlots];
            for (int row = 0; row < ListSlots; row++)
            {
                string[] taskIds = Ids($"ch-mtask-{row}-", ChallengeCardPaint.TaskSlots);
                string[] taskVars = Ids($"m{row}_t", ChallengeCardPaint.TaskSlots);
                string[] byIds = Ids($"ch-mby-{row}-", ChallengeCardPaint.CompleterSlots);
                string[] byVars = Ids($"m{row}_by", ChallengeCardPaint.CompleterSlots);
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
            // Force the first paint to clear any rows left from a previous open.
            state.MenuListPainted = ListSlots;
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
                default:
                    if (TrySelectScoreRow(player, state, buttonId))
                    {
                        break;
                    }

                    return;
            }

            Paint(player);
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
            state.MenuListPage = 0;
            return true;
        }

        private static bool SameSteam(string? a, string? b) =>
            !string.IsNullOrEmpty(a)
            && !string.IsNullOrEmpty(b)
            && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private static bool IsDisabled(CCSPlayerController player, string buttonId)
        {
            if (buttonId is not (HudMenu.BtnScorePrev or HudMenu.BtnScoreNext
                or HudMenu.BtnListPrev or HudMenu.BtnListNext
                or HudMenu.BtnFilterSolved or HudMenu.BtnFilterProgress
                or HudMenu.BtnFilterEnding or HudMenu.BtnFilterStarting))
            {
                return false;
            }

            return CustomHud.HasClass(player, buttonId, "is-disabled");
        }

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
            TimeSpan? maxAhead) =>
            VisitScheduleWindow(dateOf, now, maxAhead, onChallenge: null);

        /// <summary>Unique challenges in schedules whose date falls in (now, now+maxAhead].</summary>
        private static int VisitScheduleWindow(
            Func<ChallengeSchedule, DateTime?> dateOf,
            DateTime now,
            TimeSpan? maxAhead,
            Action<ChallengeDefinition, DateTime>? onChallenge)
        {
            int count = 0;
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

                    count++;
                    onChallenge?.Invoke(challenge, date);
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
            int pageStart = session.MenuListPage * ListSlots;
            int painted = Math.Min(ListSlots, entries.Count - pageStart);

            for (int i = 0; i < painted; i++)
            {
                Entry entry = entries[pageStart + i];
                ChallengeDefinition challenge = entry.Challenge;
                bool inSchedule = scheduleOnly
                    || (schedule != null && schedule.Challenges.Contains(challenge));
                string rowId = ListRowIds[i];
                CustomHud.SetText(player, Panel, ListTitleVars[i], Titles.For(player, challenge.Title));
                CustomHud.SetText(player, Panel, ListWhenVars[i], entry.When);
                CustomHud.SetText(player, Panel, ListMetaVars[i], Context.FormatPercent(player, entry.Percent));
                CustomHud.SetStepPercent(player, ListFillIds[i], entry.Percent);
                CustomHud.SetHasClass(player, rowId, "empty", false);
                CustomHud.SetHasClass(player, rowId, "is-off", false);
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
            if (painted < clearUntil)
            {
                for (int i = painted; i < clearUntil; i++)
                {
                    ClearListSlot(player, i);
                }
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
            ChallengeCardPaint.Slots slots = ListCardSlots[row];
            ChallengeCardPaint.ClearTasks(player, slots);
            ChallengeCardPaint.ClearCompleters(player, slots);
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
            VisitScheduleWindow(dateOf, now, maxAhead, (challenge, date) =>
            {
                entries.Add(make(challenge) with { SortTime = date });
            });

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
                    for (int j = i; j < ScoreSlots; j++)
                    {
                        state.MenuScoreRowSteamIds[j] = null;
                        ClearScoreSlot(player, j);
                    }

                    break;
                }

                ScoreEntry entry = board[index];
                string rowId = ScoreRowIds[i];
                state.MenuScoreRowSteamIds[i] = entry.SteamId;
                CustomHud.SetText(player, Panel, ScoreRankVars[i], Context.FormatRank(player, index + 1));
                CustomHud.SetText(player, Panel, ScoreNameVars[i], entry.Name);
                CustomHud.SetText(player, Panel, ScoreCurVars[i], Context.FormatCount(player, entry.Current, available));
                CustomHud.SetText(player, Panel, ScoreTotVars[i], Context.FormatNumber(player, entry.Total));
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

            if (viewerState.ScoreboardFilter != ScoreboardFilter.All
                || Context.Archive is not { } archive)
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
            CustomHud.SetText(player, Panel, ScoreRankVars[index], string.Empty);
            CustomHud.SetText(player, Panel, ScoreNameVars[index], string.Empty);
            CustomHud.SetText(player, Panel, ScoreCurVars[index], string.Empty);
            CustomHud.SetText(player, Panel, ScoreTotVars[index], string.Empty);
            string rowId = ScoreRowIds[index];
            CustomHud.SetHasClass(player, rowId, "empty", true);
            CustomHud.SetHasClass(player, rowId, "is-off", true);
            CustomHud.SetHasClass(player, rowId, "is-self", false);
            CustomHud.SetHasClass(player, rowId, "is-viewing", false);
            CustomHud.SetHasClass(player, rowId, "alt", false);
        }
    }
}
