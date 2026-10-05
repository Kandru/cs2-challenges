using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Shared task + completer slot painting for menu and tracker challenge cards.</summary>
    internal static class ChallengeCardPaint
    {
        public const int TaskSlots = 3;
        public const int CompleterSlots = 6;

        public readonly record struct Slots(
            string Panel,
            Func<int, string> TaskId,
            Func<int, string> TaskVar,
            Func<int, string> CompleterId,
            Func<int, string> CompleterVar);

        public static void ClearTasks(CCSPlayerController player, Slots slots)
        {
            for (int t = 0; t < TaskSlots; t++)
            {
                ClearTask(player, slots, t);
            }
        }

        public static void ClearCompleters(CCSPlayerController player, Slots slots)
        {
            for (int b = 0; b < CompleterSlots; b++)
            {
                ClearCompleter(player, slots, b);
            }
        }

        public static void PaintTasks(
            CCSPlayerController player,
            PlayerState state,
            string? scheduleKey,
            ChallengeDefinition challenge,
            Slots slots)
        {
            List<ChallengeTask> visible = ChallengeProgress.VisibleTasks(challenge);
            int overflow = visible.Count > TaskSlots ? visible.Count - (TaskSlots - 1) : 0;
            int shown = overflow > 0 ? TaskSlots - 1 : visible.Count;

            for (int t = 0; t < TaskSlots; t++)
            {
                if (t == TaskSlots - 1 && overflow > 0)
                {
                    string label = Context.Text(player, "hud.menu.task.overflow")
                        .Replace("{count}", overflow.ToString());
                    CustomHud.SetText(player, slots.Panel, slots.TaskVar(t), label);
                    CustomHud.SetHasClass(player, slots.TaskId(t), "is-off", false);
                    CustomHud.SetHasClass(player, slots.TaskId(t), "is-done", false);
                    continue;
                }

                if (t >= shown)
                {
                    ClearTask(player, slots, t);
                    continue;
                }

                ChallengeTask task = visible[t];
                bool complete = scheduleKey != null
                    && ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task);
                int amount = Math.Max(1, task.Amount);
                int count = ChallengeProgress.GetTaskCount(state, scheduleKey, challenge.Id, task);
                CustomHud.SetText(player, slots.Panel, slots.TaskVar(t), Titles.For(player, task.Title, count, amount));
                CustomHud.SetHasClass(player, slots.TaskId(t), "is-off", false);
                CustomHud.SetHasClass(player, slots.TaskId(t), "is-done", complete);
            }
        }

        public static void PaintCompleters(
            CCSPlayerController player,
            RunningSchedule? schedule,
            ChallengeDefinition challenge,
            Slots slots)
        {
            List<string> names = CollectCompleters(schedule, challenge);
            if (names.Count == 0)
            {
                CustomHud.SetText(player, slots.Panel, slots.CompleterVar(0), Context.Text(player, "hud.menu.solved_by.empty"));
                CustomHud.SetHasClass(player, slots.CompleterId(0), "is-off", false);
                CustomHud.SetHasClass(player, slots.CompleterId(0), "is-empty", true);
                for (int b = 1; b < CompleterSlots; b++)
                {
                    ClearCompleter(player, slots, b);
                }

                return;
            }

            int overflow = names.Count > CompleterSlots ? names.Count - (CompleterSlots - 1) : 0;
            int shown = overflow > 0 ? CompleterSlots - 1 : names.Count;

            for (int b = 0; b < CompleterSlots; b++)
            {
                if (b == CompleterSlots - 1 && overflow > 0)
                {
                    string label = Context.Text(player, "hud.menu.task.overflow")
                        .Replace("{count}", overflow.ToString());
                    CustomHud.SetText(player, slots.Panel, slots.CompleterVar(b), label);
                    CustomHud.SetHasClass(player, slots.CompleterId(b), "is-off", false);
                    CustomHud.SetHasClass(player, slots.CompleterId(b), "is-empty", true);
                    continue;
                }

                if (b >= shown)
                {
                    ClearCompleter(player, slots, b);
                    continue;
                }

                CustomHud.SetText(player, slots.Panel, slots.CompleterVar(b), names[b]);
                CustomHud.SetHasClass(player, slots.CompleterId(b), "is-off", false);
                CustomHud.SetHasClass(player, slots.CompleterId(b), "is-empty", false);
            }
        }

        public static List<string> CollectCompleters(RunningSchedule? schedule, ChallengeDefinition challenge)
        {
            List<string> names = [];
            if (schedule == null)
            {
                return names;
            }

            foreach (CCSPlayerController human in Players.GetHumans())
            {
                if (Context.GetState(human) is { } state
                    && ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge))
                {
                    names.Add(human.PlayerName);
                }
            }

            return names;
        }

        private static void ClearTask(CCSPlayerController player, Slots slots, int task)
        {
            CustomHud.SetText(player, slots.Panel, slots.TaskVar(task), string.Empty);
            CustomHud.SetHasClass(player, slots.TaskId(task), "is-off", true);
            CustomHud.SetHasClass(player, slots.TaskId(task), "is-done", false);
        }

        private static void ClearCompleter(CCSPlayerController player, Slots slots, int slot)
        {
            CustomHud.SetText(player, slots.Panel, slots.CompleterVar(slot), string.Empty);
            CustomHud.SetHasClass(player, slots.CompleterId(slot), "is-off", true);
            CustomHud.SetHasClass(player, slots.CompleterId(slot), "is-empty", false);
        }
    }
}
