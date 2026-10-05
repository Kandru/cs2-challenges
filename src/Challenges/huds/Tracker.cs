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

        private const string VarTitle = "tr_title";
        private const string VarCount = "tr_count";

        public static string RowId(int index) => $"ch-trow-{index}";
        public static string FillId(int index) => $"ch-tfill-{index}";
        private static string VarRowTitle(int index) => $"tr_t{index}";
        private static string VarRowValue(int index) => $"tr_v{index}";

        public static int ConfiguredRows => Math.Clamp(Context.Config.Gui.TrackerRows, 3, MaxRows);

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
            for (int i = 0; i < MaxRows; i++)
            {
                CustomHud.SetText(player, Panel, VarRowTitle(i), string.Empty);
                CustomHud.SetText(player, Panel, VarRowValue(i), string.Empty);
                CustomHud.SetStepPercent(player, FillId(i), 0);
                CustomHud.SetHasClass(player, RowId(i), "is-off", true);
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
            List<(ChallengeDefinition Challenge, int Percent)> rows = progressMode
                ? ProgressRows(state, schedule)
                : FreezeRows(state, schedule);

            string heading = Context.Text(player, "hud.tracker.title");
            string count = $"{ChallengeProgress.CountSolvedInSchedule(state, schedule)} / {schedule.Challenges.Count}";
            string[] titles = new string[rows.Count];
            StringBuilder fingerprint = new StringBuilder().Append(heading).Append('|').Append(count);
            for (int i = 0; i < rows.Count; i++)
            {
                titles[i] = RowTitle(player, state, schedule, rows[i].Challenge);
                fingerprint.Append('|').Append(titles[i]).Append(':').Append(rows[i].Percent);
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

            for (int i = 0; i < MaxRows; i++)
            {
                if (i < rows.Count)
                {
                    int percent = rows[i].Percent;
                    CustomHud.SetText(player, Panel, VarRowTitle(i), titles[i]);
                    CustomHud.SetText(player, Panel, VarRowValue(i), $"{percent}%");
                    CustomHud.SetStepPercent(player, FillId(i), percent);
                    CustomHud.SetHasClass(player, RowId(i), "is-off", false);
                }
                else
                {
                    CustomHud.SetText(player, Panel, VarRowTitle(i), string.Empty);
                    CustomHud.SetText(player, Panel, VarRowValue(i), string.Empty);
                    CustomHud.SetStepPercent(player, FillId(i), 0);
                    CustomHud.SetHasClass(player, RowId(i), "is-off", true);
                }
            }
        }

        private static List<(ChallengeDefinition, int)> FreezeRows(PlayerState state, RunningSchedule schedule)
        {
            int limit = ConfiguredRows;
            List<(ChallengeDefinition Challenge, int Percent)> rows = [];
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge))
                {
                    continue;
                }

                int percent = ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge);
                rows.Add((challenge, percent));
            }

            rows.Sort(static (a, b) => b.Percent.CompareTo(a.Percent));
            if (rows.Count > limit)
            {
                rows.RemoveRange(limit, rows.Count - limit);
            }

            return rows;
        }

        private static List<(ChallengeDefinition, int)> ProgressRows(PlayerState state, RunningSchedule schedule)
        {
            List<(ChallengeDefinition Challenge, int Percent)> rows = [];
            foreach (ChallengeDefinition challenge in schedule.Challenges)
            {
                if (!state.TrackerProgressIds.Contains(challenge.Id))
                {
                    continue;
                }

                rows.Add((challenge, ChallengeProgress.GetChallengePercent(state, schedule.Key, challenge)));
            }

            rows.Sort(static (a, b) => b.Percent.CompareTo(a.Percent));
            if (rows.Count > MaxRows)
            {
                rows.RemoveRange(MaxRows, rows.Count - MaxRows);
            }

            return rows;
        }

        /// <summary>Challenge title with {count}/{total} filled from the task the player is working on.</summary>
        public static string RowTitle(CCSPlayerController player, PlayerState state, RunningSchedule schedule, ChallengeDefinition challenge)
        {
            string title = Titles.Resolve(challenge.Title, player);
            ChallengeTask? current = ChallengeProgress.GetCurrentTask(state, schedule.Key, challenge);
            if (current == null)
            {
                return Titles.Expand(title, 0, 0);
            }

            int count = Math.Min(
                Math.Max(1, current.Amount),
                ChallengeProgress.GetTaskAmount(state, schedule.Key, challenge.Id, current.Id));
            return Titles.Expand(title, count, Math.Max(1, current.Amount));
        }
    }
}
