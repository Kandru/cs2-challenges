using System.Text;
using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Top-right challenge tracker: freeze-time overview and the short progress mode.</summary>
    public static class Tracker
    {
        public const string Panel = "Tracker";
        public const int MaxRows = 5;
        public const int TaskSlots = ChallengeCardPaint.TaskSlots;
        public const int CompleterSlots = ChallengeCardPaint.CompleterSlots;

        private const string VarTitle = "tr_title";
        private const string VarCount = "tr_count";
        private const string VarTasksHead = "tr_tasks_h";
        private const string VarByHead = "tr_by_h";

        public static string RowId(int index) => $"ch-trow-{index}";
        public static string FillId(int index) => $"ch-tfill-{index}";
        public static string TaskId(int row, int task) => $"ch-ttask-{row}-{task}";
        public static string CompleterId(int row, int slot) => $"ch-tby-{row}-{slot}";
        private static string VarRowTitle(int index) => $"tr_t{index}";
        private static string VarRowValue(int index) => $"tr_v{index}";

        public static int ConfiguredRows => Math.Clamp(Context.Config.Gui.TrackerRows, 3, MaxRows);

        private static ChallengeCardPaint.Slots CardSlots(int row) => new(
            Panel,
            t => TaskId(row, t),
            t => $"tr_{row}_t{t}",
            b => CompleterId(row, b),
            b => $"tr_{row}_by{b}");

        public static void ShowFreeze(CCSPlayerController player)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state)
            {
                return;
            }

            state.TrackerFreezeVisible = true;
            Paint(player, state);
        }

        public static void EndFreeze(CCSPlayerController player)
        {
            if (Context.GetState(player) is { } state)
            {
                state.TrackerFreezeVisible = false;
            }

            Refresh(player);
        }

        public static void Hide(CCSPlayerController player)
        {
            if (Context.GetState(player) is { } state)
            {
                state.TrackerFreezeVisible = false;
                state.TrackerProgressUntil = null;
                state.TrackerProgressIds.Clear();
                state.TrackerFingerprint = null;
            }

            CustomHud.HidePanel(player, Panel);
        }

        public static void ShowProgress(CCSPlayerController player, IReadOnlyCollection<string> challengeIds)
        {
            if (!Players.IsHumanViewer(player) || Context.GetState(player) is not { } state || challengeIds.Count == 0)
            {
                return;
            }

            state.TrackerProgressIds.Clear();
            foreach (string id in challengeIds)
            {
                state.TrackerProgressIds.Add(id);
            }

            state.TrackerProgressUntil = DateTime.UtcNow.AddSeconds(Math.Max(1f, Context.Config.Gui.ProgressDuration));
            Paint(player, state);
        }

        /// <summary>Cheap enough for the staggered OnTick: only paints while a mode is active.</summary>
        public static void Refresh(CCSPlayerController player)
        {
            if (!Context.IsBound || Context.GetState(player) is not { } state)
            {
                return;
            }

            bool progressActive = state.TrackerProgressUntil is { } until && DateTime.UtcNow < until;
            if (!progressActive && state.TrackerProgressUntil != null)
            {
                state.TrackerProgressUntil = null;
                state.TrackerProgressIds.Clear();
            }

            if (!state.TrackerFreezeVisible && !progressActive)
            {
                if (CustomHud.IsPanelVisible(player, Panel))
                {
                    CustomHud.HidePanel(player, Panel);
                }

                state.TrackerFingerprint = null;
                return;
            }

            Paint(player, state);
        }

        public static void WriteDefaults(CCSPlayerController player)
        {
            CustomHud.SetText(player, Panel, VarTitle, string.Empty);
            CustomHud.SetText(player, Panel, VarCount, string.Empty);
            CustomHud.SetText(player, Panel, VarTasksHead, string.Empty);
            CustomHud.SetText(player, Panel, VarByHead, string.Empty);
            for (int i = 0; i < MaxRows; i++)
            {
                ClearRow(player, i);
            }
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
            int limit = progressMode ? MaxRows : ConfiguredRows;
            List<(ChallengeDefinition Challenge, int Percent)> ranked = progressMode
                ? RankedRows(state, schedule, c => state.TrackerProgressIds.Contains(c.Id), limit)
                : RankedRows(state, schedule, c => !ChallengeProgress.IsChallengeSolved(state, schedule.Key, c), limit);

            string heading = Context.Text(player, "hud.tracker.title");
            string count = Context.FormatCount(
                player,
                ChallengeProgress.CountSolvedInSchedule(state, schedule),
                schedule.Challenges.Count);
            string tasksHead = Context.Text(player, "hud.menu.tasks");
            string byHead = Context.Text(player, "hud.menu.solved_by");
            StringBuilder fingerprint = new StringBuilder(heading.Length + count.Length + ranked.Count * 96)
                .Append(heading).Append('|').Append(count);

            foreach ((ChallengeDefinition challenge, int percent) in ranked)
            {
                string title = Titles.For(player, challenge.Title);
                fingerprint.Append('|').Append(title).Append(':').Append(percent);
                foreach (ChallengeTask task in ChallengeProgress.VisibleTasks(challenge))
                {
                    bool done = ChallengeProgress.IsTaskComplete(state, schedule.Key, challenge.Id, task);
                    int amount = Math.Max(1, task.Amount);
                    int taskCount = ChallengeProgress.GetTaskCount(state, schedule.Key, challenge.Id, task);
                    fingerprint.Append('>').Append(done ? '1' : '0').Append(taskCount).Append('/').Append(amount);
                }

                foreach (string name in ChallengeCardPaint.CollectCompleters(schedule, challenge))
                {
                    fingerprint.Append('@').Append(name);
                }
            }

            string key = fingerprint.ToString();
            if (key == state.TrackerFingerprint && CustomHud.IsPanelVisible(player, Panel))
            {
                return;
            }

            state.TrackerFingerprint = key;
            CustomHud.ShowPanel(player, Panel);
            CustomHud.SetText(player, Panel, VarTitle, heading);
            CustomHud.SetText(player, Panel, VarCount, count);
            CustomHud.SetText(player, Panel, VarTasksHead, tasksHead);
            CustomHud.SetText(player, Panel, VarByHead, byHead);

            for (int i = 0; i < MaxRows; i++)
            {
                if (i < ranked.Count)
                {
                    (ChallengeDefinition challenge, int percent) = ranked[i];
                    CustomHud.SetText(player, Panel, VarRowTitle(i), Titles.For(player, challenge.Title));
                    CustomHud.SetText(player, Panel, VarRowValue(i), Context.FormatPercent(player, percent));
                    CustomHud.SetStepPercent(player, FillId(i), percent);
                    CustomHud.SetHasClass(player, RowId(i), "is-off", false);
                    ChallengeCardPaint.Slots slots = CardSlots(i);
                    ChallengeCardPaint.PaintTasks(player, state, schedule.Key, challenge, slots);
                    ChallengeCardPaint.PaintCompleters(player, schedule, challenge, slots);
                }
                else
                {
                    ClearRow(player, i);
                }
            }
        }

        private static void ClearRow(CCSPlayerController player, int index)
        {
            CustomHud.SetText(player, Panel, VarRowTitle(index), string.Empty);
            CustomHud.SetText(player, Panel, VarRowValue(index), string.Empty);
            CustomHud.SetStepPercent(player, FillId(index), 0);
            CustomHud.SetHasClass(player, RowId(index), "is-off", true);
            ChallengeCardPaint.Slots slots = CardSlots(index);
            ChallengeCardPaint.ClearTasks(player, slots);
            ChallengeCardPaint.ClearCompleters(player, slots);
        }

        /// <summary>Unsolved/matching challenges, highest completion percent first, truncated to <paramref name="limit"/>.</summary>
        private static List<(ChallengeDefinition Challenge, int Percent)> RankedRows(
            PlayerState state,
            RunningSchedule schedule,
            Func<ChallengeDefinition, bool> include,
            int limit)
        {
            List<(ChallengeDefinition Challenge, int Percent)> rows = [];
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (!include(challenge))
                {
                    continue;
                }

                rows.Add((challenge, ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge)));
            }

            rows.Sort(static (a, b) => b.Percent.CompareTo(a.Percent));
            if (rows.Count > limit)
            {
                rows.RemoveRange(limit, rows.Count - limit);
            }

            return rows;
        }
    }
}
