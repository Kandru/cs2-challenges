using System.Text;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Top-right challenge tracker: freeze-time overview and compact progress popups.</summary>
    public static class Tracker
    {
        public const string Panel = "Tracker";
        public const int MaxRows = 5;
        public const string TimerId = "ch-ttimer";
        public const string TimerFillId = "ch-ttimer-fill";
        public const string HintId = "ch-tr-hint";

        private const string VarTitle = "tr_title";
        private const string VarCount = "tr_count";
        private const string VarTasksHead = "tr_tasks_h";
        private const string VarHint = "tr_hint";

        private static readonly ChallengeCardPaint.Slots[] RowSlots = CreateRowSlots();

        public static string RowId(int index) => $"ch-trow-{index}";
        public static string FillId(int index) => $"ch-tfill-{index}";
        public static string TaskId(int row, int task) => $"ch-ttask-{row}-{task}";
        private static string VarRowTitle(int index) => $"tr_t{index}";
        private static string VarRowValue(int index) => $"tr_v{index}";

        public static int ConfiguredRows => Math.Clamp(Context.Config.Gui.TrackerRows, 1, MaxRows);

        /// <summary><c>mp_freezetime</c>; freeze overview is skipped when this is &lt;= 0.</summary>
        public static int FreezeTimeSeconds =>
            ConVar.Find("mp_freezetime")?.GetPrimitiveValue<int>() ?? 0;

        private static ChallengeCardPaint.Slots[] CreateRowSlots()
        {
            ChallengeCardPaint.Slots[] slots = new ChallengeCardPaint.Slots[MaxRows];
            for (int row = 0; row < MaxRows; row++)
            {
                int r = row;
                slots[r] = new ChallengeCardPaint.Slots(
                    Panel,
                    t => TaskId(r, t),
                    t => $"tr_{r}_t{t}");
            }

            return slots;
        }

        private static float ProgressSeconds => Math.Max(1f, Context.Config.Gui.ProgressDuration);

        public static void ShowFreeze(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return;
            }

            int freezeSec = FreezeTimeSeconds;
            if (freezeSec <= 0)
            {
                return;
            }

            ClearProgressState(state);
            state.TrackerRuleBrokenQueue.Clear();
            state.TrackerFreezeVisible = true;
            state.TrackerFreezeDuration = freezeSec;
            state.TrackerFreezeUntil = DateTime.UtcNow.AddSeconds(freezeSec);
            state.TrackerFingerprint = null;
            Paint(player, state);
            PaintTimer(player, state);
        }

        public static void EndFreeze(CCSPlayerController player)
        {
            if (Context.GetState(player) is { } state)
            {
                state.TrackerFreezeVisible = false;
                state.TrackerFreezeUntil = null;
                state.TrackerFreezeDuration = 0;
                if (TryShowNextRuleBroken(player, state))
                {
                    return;
                }
            }

            Refresh(player);
        }

        public static void Hide(CCSPlayerController player)
        {
            if (Context.GetState(player) is { } state)
            {
                ClearProgressState(state);
                state.TrackerRuleBrokenQueue.Clear();
                state.TrackerFreezeVisible = false;
                state.TrackerFreezeUntil = null;
                state.TrackerFreezeDuration = 0;
                state.TrackerFingerprint = null;
            }

            CustomHud.HidePanel(player, Panel);
        }

        public static void ShowProgress(CCSPlayerController player, IReadOnlyList<TrackerProgressItem> items)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state || items.Count == 0)
            {
                return;
            }

            BeginProgress(player, state, items);
        }

        public static void ShowRuleBroken(CCSPlayerController player, string challengeId, string taskId)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return;
            }

            state.TrackerRuleBrokenQueue.Add(new TrackerProgressItem
            {
                ChallengeId = challengeId,
                TaskId = taskId,
                Kind = TrackerProgressKind.RuleBroken,
            });

            if (!state.TrackerFreezeVisible && !IsShowingRuleBroken(state))
            {
                TryShowNextRuleBroken(player, state);
            }
        }

        private static bool IsShowingRuleBroken(PlayerState state) =>
            state.TrackerProgressUntil != null && HasKind(state, TrackerProgressKind.RuleBroken);

        private static bool TryShowNextRuleBroken(CCSPlayerController player, PlayerState state)
        {
            if (state.TrackerRuleBrokenQueue.Count == 0)
            {
                return false;
            }

            TrackerProgressItem next = state.TrackerRuleBrokenQueue[0];
            state.TrackerRuleBrokenQueue.RemoveAt(0);
            BeginProgress(player, state, [next]);
            return true;
        }

        private static void BeginProgress(
            CCSPlayerController player,
            PlayerState state,
            IReadOnlyList<TrackerProgressItem> items)
        {
            state.TrackerProgressItems.Clear();
            state.TrackerProgressItems.AddRange(items);
            state.TrackerShowingUpNext = false;
            state.TrackerUpNextPending = false;
            state.TrackerFadeUntil = null;
            state.TrackerProgressUntil = DateTime.UtcNow.AddSeconds(ProgressSeconds);
            state.TrackerFingerprint = null;
            Paint(player, state);
            PaintTimer(player, state);
        }

        /// <summary>Cheap enough for the roster OnTick cadence: only paints while a mode is active.</summary>
        public static void Refresh(CCSPlayerController player)
        {
            if (!Context.IsBound || Context.GetState(player) is not { } state)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            if (state.TrackerFadeUntil is { } fadeUntil)
            {
                if (now < fadeUntil)
                {
                    return;
                }

                state.TrackerFadeUntil = null;
                if (state.TrackerUpNextPending)
                {
                    state.TrackerUpNextPending = false;
                    state.TrackerShowingUpNext = true;
                    state.TrackerProgressItems.Clear();
                    state.TrackerProgressUntil = now.AddSeconds(ProgressSeconds);
                    state.TrackerFingerprint = null;
                    Paint(player, state);
                    PaintTimer(player, state);
                    CustomHud.PulseFadeIn(player, Panel);
                    return;
                }

                if (!state.TrackerFreezeVisible)
                {
                    CustomHud.FinishFadeOut(player, Panel);
                    state.TrackerFingerprint = null;
                    return;
                }

                ClearProgressState(state);
                state.TrackerFingerprint = null;
                Paint(player, state);
                PaintTimer(player, state);
                return;
            }

            if (state.TrackerProgressUntil is { } until && now >= until)
            {
                bool drainRules = HasKind(state, TrackerProgressKind.RuleBroken)
                    || state.TrackerRuleBrokenQueue.Count > 0;
                state.TrackerProgressUntil = null;

                if (state.TrackerShowingUpNext)
                {
                    state.TrackerShowingUpNext = false;
                    BeginFadeOut(player, state, upNext: false);
                    return;
                }

                if (!drainRules
                    && HasKind(state, TrackerProgressKind.ChallengeSolved)
                    && TryNextUnsolved(state, Context.Schedule) != null)
                {
                    BeginFadeOut(player, state, upNext: true);
                    return;
                }

                ClearProgressState(state);
                if (TryShowNextRuleBroken(player, state))
                {
                    return;
                }

                if (!state.TrackerFreezeVisible)
                {
                    BeginFadeOut(player, state, upNext: false);
                    return;
                }

                state.TrackerFingerprint = null;
            }

            if (!state.TrackerFreezeVisible && state.TrackerProgressUntil == null)
            {
                if (CustomHud.IsPanelVisible(player, Panel))
                {
                    CustomHud.HidePanel(player, Panel);
                }

                HideTimer(player);
                state.TrackerFingerprint = null;
                return;
            }

            Paint(player, state);
            PaintTimer(player, state);
        }

        public static void WriteDefaults(CCSPlayerController player)
        {
            CustomHud.SetText(player, Panel, VarTitle, string.Empty);
            CustomHud.SetText(player, Panel, VarCount, string.Empty);
            CustomHud.SetText(player, Panel, VarTasksHead, string.Empty);
            PaintHint(player, string.Empty);
            HudTheme.Apply(player, Panel);
            HideTimer(player);
            CustomHud.SetPercent(player, TimerFillId, 100);
            for (int i = 0; i < MaxRows; i++)
            {
                ClearRow(player, i);
            }
        }

        private static void PaintTimer(CCSPlayerController player, PlayerState state)
        {
            DateTime now = DateTime.UtcNow;
            float total;
            float remaining;

            if (state.TrackerProgressUntil is { } progressUntil && now < progressUntil)
            {
                total = ProgressSeconds;
                remaining = (float)(progressUntil - now).TotalSeconds;
            }
            else if (state.TrackerFreezeVisible
                && state.TrackerFreezeUntil is { } freezeUntil
                && state.TrackerFreezeDuration > 0
                && now < freezeUntil)
            {
                total = state.TrackerFreezeDuration;
                remaining = (float)(freezeUntil - now).TotalSeconds;
            }
            else
            {
                HideTimer(player);
                return;
            }

            int percent = (int)Math.Clamp(Math.Round(remaining / total * 100.0), 0, 100);
            CustomHud.SetHasClass(player, TimerId, "is-off", false);
            CustomHud.SetPercent(player, TimerFillId, percent);
        }

        private static void HideTimer(CCSPlayerController player)
        {
            CustomHud.SetHasClass(player, TimerId, "is-off", true);
        }

        private static void BeginFadeOut(CCSPlayerController player, PlayerState state, bool upNext)
        {
            state.TrackerUpNextPending = upNext;
            state.TrackerFadeUntil = DateTime.UtcNow.AddSeconds(CustomHud.FadeSeconds);
            if (upNext)
            {
                CustomHud.BeginFadeOut(player, Panel, collapseAfter: false);
            }
            else
            {
                CustomHud.HidePanel(player, Panel);
            }
        }

        private static void ClearProgressState(PlayerState state)
        {
            state.TrackerProgressUntil = null;
            state.TrackerProgressItems.Clear();
            state.TrackerShowingUpNext = false;
            state.TrackerUpNextPending = false;
            state.TrackerFadeUntil = null;
        }

        private static bool HasKind(PlayerState state, TrackerProgressKind kind)
        {
            List<TrackerProgressItem> items = state.TrackerProgressItems;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Highest-percent unsolved challenge, or null when none remain.</summary>
        private static ChallengeDefinition? TryNextUnsolved(PlayerState state, RunningSchedule? schedule)
        {
            if (schedule == null)
            {
                return null;
            }

            ChallengeDefinition? best = null;
            int bestPercent = -1;
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge))
                {
                    continue;
                }

                int percent = ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge);
                if (percent > bestPercent)
                {
                    bestPercent = percent;
                    best = challenge;
                }
            }

            return best;
        }

        private static void Paint(CCSPlayerController player, PlayerState state)
        {
            if (!Players.IsHumanViewer(player) || !CustomHud.EnsureSpawned(player))
            {
                return;
            }

            RunningSchedule? schedule = Context.Schedule;
            if (schedule == null || schedule.Challenges.Count == 0)
            {
                if (CustomHud.IsPanelVisible(player, Panel))
                {
                    CustomHud.HidePanel(player, Panel);
                }

                state.TrackerFingerprint = null;
                return;
            }

            bool progressMode = state.TrackerProgressUntil is { } until && DateTime.UtcNow < until;
            bool upNext = state.TrackerShowingUpNext;
            List<RowPaint> rows = progressMode
                ? BuildProgressRows(player, state, schedule, upNext)
                : BuildFreezeRows(player, state, schedule);

            string heading;
            string count;
            if (upNext)
            {
                heading = Context.Text(player, "hud.tracker.up_next");
                count = Context.FormatCount(
                    player,
                    ChallengeProgress.CountSolvedInSchedule(state, schedule),
                    schedule.Challenges.Count);
            }
            else if (progressMode && TryRuleBrokenTitle(player, state, schedule, out string ruleTitle))
            {
                heading = ruleTitle;
                count = string.Empty;
            }
            else
            {
                heading = Context.Text(player, "hud.tracker.title");
                count = Context.FormatCount(
                    player,
                    ChallengeProgress.CountSolvedInSchedule(state, schedule),
                    schedule.Challenges.Count);
            }

            string tasksHead = Context.Text(player, "hud.menu.tasks");
            string hint = OpenHint(player);
            StringBuilder fingerprint = new StringBuilder(heading.Length + count.Length + hint.Length + rows.Count * 96)
                .Append(heading).Append('|').Append(count).Append('|').Append(hint).Append('|').Append(upNext ? '1' : '0');

            foreach (RowPaint row in rows)
            {
                fingerprint.Append('|').Append(row.Title).Append(':').Append(row.Percent);
                foreach ((ChallengeTask task, bool done, bool broken, int countVal, int amount) in row.Tasks)
                {
                    fingerprint.Append('>')
                        .Append(done ? '1' : '0')
                        .Append(broken ? 'B' : '-')
                        .Append(countVal).Append('/').Append(amount)
                        .Append(':').Append(task.Id);
                }
            }

            string key = fingerprint.ToString();
            if (key == state.TrackerFingerprint && CustomHud.IsPanelVisible(player, Panel))
            {
                return;
            }

            state.TrackerFingerprint = key;
            HudTheme.Apply(player, Panel);
            CustomHud.ShowPanel(player, Panel);
            CustomHud.SetText(player, Panel, VarTitle, heading);
            CustomHud.SetText(player, Panel, VarCount, count);
            CustomHud.SetText(player, Panel, VarTasksHead, tasksHead);
            PaintHint(player, hint);

            for (int i = 0; i < MaxRows; i++)
            {
                if (i < rows.Count)
                {
                    RowPaint row = rows[i];
                    CustomHud.SetText(player, Panel, VarRowTitle(i), row.Title);
                    CustomHud.SetText(player, Panel, VarRowValue(i), Context.FormatPercent(player, row.Percent));
                    CustomHud.SetStepPercent(player, FillId(i), row.Percent);
                    CustomHud.SetHasClass(player, RowId(i), "is-off", false);
                    ChallengeCardPaint.PaintTaskList(player, RowSlots[i], row.Tasks);
                }
                else
                {
                    ClearRow(player, i);
                }
            }
        }

        private static string OpenHint(CCSPlayerController player)
        {
            string? command = MenuCommandNames.ShortestChatCommand(
                Context.Config.MenuCommands,
                Context.Config.CommandPrefix);
            return command == null
                ? string.Empty
                : Context.Text(player, "hud.tracker.open", ("{command}", command));
        }

        private static void PaintHint(CCSPlayerController player, string hint)
        {
            CustomHud.SetText(player, Panel, VarHint, hint);
            CustomHud.SetHasClass(player, HintId, "is-off", hint.Length == 0);
        }

        private static List<RowPaint> BuildFreezeRows(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule)
        {
            List<(ChallengeDefinition Challenge, int Percent)> ranked = [];
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge))
                {
                    continue;
                }

                ranked.Add((challenge, ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge)));
            }

            ranked.Sort(static (a, b) => b.Percent.CompareTo(a.Percent));
            int limit = ConfiguredRows;
            if (ranked.Count > limit)
            {
                ranked.RemoveRange(limit, ranked.Count - limit);
            }

            List<RowPaint> rows = new(ranked.Count);
            foreach ((ChallengeDefinition challenge, int percent) in ranked)
            {
                rows.Add(new RowPaint(
                    Titles.For(player, challenge.Title),
                    percent,
                    CollectIncompleteTasks(state, schedule.Key, challenge)));
            }

            return rows;
        }

        private static List<RowPaint> BuildProgressRows(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            bool upNext)
        {
            if (upNext)
            {
                ChallengeDefinition? next = TryNextUnsolved(state, schedule);
                if (next == null)
                {
                    return [];
                }

                return
                [
                    new RowPaint(
                        Titles.For(player, next.Title),
                        ChallengeProgress.GetChallengePercent(state, schedule.Key, next),
                        CollectIncompleteTasks(state, schedule.Key, next)),
                ];
            }

            Dictionary<string, TrackerProgressItem> byChallenge = new(StringComparer.Ordinal);
            foreach (TrackerProgressItem item in state.TrackerProgressItems)
            {
                if (!byChallenge.TryGetValue(item.ChallengeId, out TrackerProgressItem? existing)
                    || KindRank(item.Kind) > KindRank(existing.Kind))
                {
                    byChallenge[item.ChallengeId] = item;
                }
            }

            List<(TrackerProgressItem Item, ChallengeDefinition Challenge, int Percent)> ranked = [];
            foreach (TrackerProgressItem item in byChallenge.Values)
            {
                if (!TryResolveChallenge(schedule, item.ChallengeId, out ChallengeDefinition challenge))
                {
                    continue;
                }

                ranked.Add((item, challenge, ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge)));
            }

            ranked.Sort(static (a, b) => b.Percent.CompareTo(a.Percent));
            if (ranked.Count > MaxRows)
            {
                ranked.RemoveRange(MaxRows, ranked.Count - MaxRows);
            }

            List<RowPaint> rows = new(ranked.Count);
            foreach ((TrackerProgressItem item, ChallengeDefinition challenge, int percent) in ranked)
            {
                rows.Add(new RowPaint(
                    Titles.For(player, challenge.Title),
                    percent,
                    BuildProgressTasks(state, schedule.Key, challenge, item)));
            }

            return rows;
        }

        private static bool TryResolveChallenge(
            RunningSchedule schedule,
            string challengeId,
            out ChallengeDefinition challenge)
        {
            foreach (ChallengeDefinition entry in schedule.Challenges)
            {
                if (entry.Id == challengeId)
                {
                    challenge = entry;
                    return true;
                }
            }

            return Context.ChallengeMap.TryGetValue(challengeId, out challenge!);
        }

        private static int KindRank(TrackerProgressKind kind) => kind switch
        {
            TrackerProgressKind.ChallengeSolved => 3,
            TrackerProgressKind.TaskSolved or TrackerProgressKind.RuleBroken => 2,
            _ => 1,
        };

        private static List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> CollectIncompleteTasks(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge)
        {
            List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> tasks = [];
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible
                    || ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task))
                {
                    continue;
                }

                tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, task, done: false));
            }

            return tasks;
        }

        private static List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> BuildProgressTasks(
            PlayerState state,
            string scheduleKey,
            ChallengeDefinition challenge,
            TrackerProgressItem item)
        {
            ChallengeTask? focus = FindTask(challenge, item.TaskId);
            List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> tasks = [];

            switch (item.Kind)
            {
                case TrackerProgressKind.Progress:
                    if (focus != null)
                    {
                        tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, focus, done: false));
                    }

                    break;

                case TrackerProgressKind.RuleBroken:
                    foreach (ChallengeTask reset in CollectResetTargets(challenge, focus))
                    {
                        tasks.Add(MakeTaskEntry(
                            state,
                            scheduleKey,
                            challenge.Id,
                            reset,
                            done: false,
                            broken: true));
                    }

                    break;

                case TrackerProgressKind.TaskSolved:
                    if (focus != null)
                    {
                        tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, focus, done: true));
                    }

                    foreach (ChallengeTask task in challenge.Tasks)
                    {
                        if (!task.Visible
                            || (focus != null && task.Id == focus.Id)
                            || ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task))
                        {
                            continue;
                        }

                        tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, task, done: false));
                    }

                    break;

                case TrackerProgressKind.ChallengeSolved:
                    if (focus != null)
                    {
                        tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, focus, done: true));
                        break;
                    }

                    foreach (ChallengeTask task in challenge.Tasks)
                    {
                        if (!task.Visible)
                        {
                            continue;
                        }

                        tasks.Add(MakeTaskEntry(state, scheduleKey, challenge.Id, task, done: true));
                    }

                    break;
            }

            return tasks;
        }

        private static ChallengeTask? FindTask(ChallengeDefinition challenge, string taskId) =>
            challenge.TaskById.TryGetValue(taskId, out ChallengeTask? task) ? task : null;

        private static bool TryRuleBrokenTitle(
            CCSPlayerController player,
            PlayerState state,
            RunningSchedule schedule,
            out string title)
        {
            title = string.Empty;
            List<TrackerProgressItem> items = state.TrackerProgressItems;
            for (int i = 0; i < items.Count; i++)
            {
                TrackerProgressItem item = items[i];
                if (item.Kind != TrackerProgressKind.RuleBroken
                    || !TryResolveChallenge(schedule, item.ChallengeId, out ChallengeDefinition challenge)
                    || FindTask(challenge, item.TaskId) is not { } breaker)
                {
                    continue;
                }

                title = Titles.For(player, breaker.Title);
                return title.Length > 0;
            }

            return false;
        }

        private static List<ChallengeTask> CollectResetTargets(
            ChallengeDefinition challenge,
            ChallengeTask? breaker)
        {
            if (breaker == null)
            {
                return [];
            }

            List<ChallengeTask> resets = [];
            List<ChallengeTask> notifies = [];
            HashSet<string> seenReset = new(StringComparer.Ordinal);
            HashSet<string> seenNotify = new(StringComparer.Ordinal);

            foreach (ChallengeAction action in breaker.Actions)
            {
                bool isReset = action.Type is "task.reset_progress" or "task.reset_completed";
                bool isNotify = action.Type is "notify.player.progress.rule_broken"
                    or "notify.player.completed.rule_broken";
                if (!isReset && !isNotify)
                {
                    continue;
                }

                List<ChallengeTask> bucket = isReset ? resets : notifies;
                HashSet<string> seen = isReset ? seenReset : seenNotify;
                foreach (string id in action.Values)
                {
                    if (!seen.Add(id)
                        || FindTask(challenge, id) is not { Visible: true } target)
                    {
                        continue;
                    }

                    bucket.Add(target);
                }
            }

            return resets.Count > 0 ? resets : notifies;
        }

        private static (ChallengeTask Task, bool Done, bool Broken, int Count, int Amount) MakeTaskEntry(
            PlayerState state,
            string scheduleKey,
            string challengeId,
            ChallengeTask task,
            bool done,
            bool broken = false)
        {
            int amount = Math.Max(1, task.Amount);
            int count = ChallengeProgress.GetTaskCount(state, scheduleKey, challengeId, task);
            return (task, done, broken, count, amount);
        }

        private static void ClearRow(CCSPlayerController player, int index)
        {
            CustomHud.SetText(player, Panel, VarRowTitle(index), string.Empty);
            CustomHud.SetText(player, Panel, VarRowValue(index), string.Empty);
            CustomHud.SetStepPercent(player, FillId(index), 0);
            CustomHud.SetHasClass(player, RowId(index), "is-off", true);
            ChallengeCardPaint.ClearTasks(player, RowSlots[index]);
        }

        private sealed record RowPaint(
            string Title,
            int Percent,
            List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> Tasks);
    }
}
