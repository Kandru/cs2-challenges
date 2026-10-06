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
            Func<int, string>? CompleterId = null,
            Func<int, string>? CompleterVar = null);

        public static void ClearTasks(CCSPlayerController player, Slots slots)
        {
            for (int t = 0; t < TaskSlots; t++)
            {
                ClearTask(player, slots, t);
            }
        }

        public static void ClearCompleters(CCSPlayerController player, Slots slots)
        {
            if (slots.CompleterId == null || slots.CompleterVar == null)
            {
                return;
            }

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
            List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> list = [];
            foreach (ChallengeTask task in challenge.Tasks)
            {
                if (!task.Visible)
                {
                    continue;
                }

                bool complete = scheduleKey != null
                    && ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task);
                int amount = Math.Max(1, task.Amount);
                int count = scheduleKey != null
                    ? ChallengeProgress.GetTaskCount(state, scheduleKey, challenge.Id, task)
                    : 0;
                list.Add((task, complete, false, count, amount));
            }

            PaintTaskList(player, slots, list);
        }

        public static void PaintTaskList(
            CCSPlayerController player,
            Slots slots,
            List<(ChallengeTask Task, bool Done, bool Broken, int Count, int Amount)> tasks)
        {
            int overflow = tasks.Count > TaskSlots ? tasks.Count - (TaskSlots - 1) : 0;
            int shown = overflow > 0 ? TaskSlots - 1 : tasks.Count;

            for (int t = 0; t < TaskSlots; t++)
            {
                if (t == TaskSlots - 1 && overflow > 0)
                {
                    CustomHud.SetText(player, slots.Panel, slots.TaskVar(t), Context.FormatOverflow(player, overflow));
                    CustomHud.SetHasClass(player, slots.TaskId(t), "is-off", false);
                    CustomHud.SetHasClass(player, slots.TaskId(t), "is-done", false);
                    CustomHud.SetHasClass(player, slots.TaskId(t), "is-broken", false);
                    continue;
                }

                if (t >= shown)
                {
                    ClearTask(player, slots, t);
                    continue;
                }

                (ChallengeTask task, bool complete, bool broken, int count, int amount) = tasks[t];
                CustomHud.SetText(player, slots.Panel, slots.TaskVar(t), Titles.For(player, task.Title, count, amount));
                CustomHud.SetHasClass(player, slots.TaskId(t), "is-off", false);
                CustomHud.SetHasClass(player, slots.TaskId(t), "is-done", complete);
                CustomHud.SetHasClass(player, slots.TaskId(t), "is-broken", broken);
            }
        }

        public static void PaintCompleters(
            CCSPlayerController viewer,
            RunningSchedule? schedule,
            ChallengeDefinition challenge,
            Slots slots)
        {
            if (slots.CompleterId == null || slots.CompleterVar == null)
            {
                return;
            }

            Func<int, string> completerId = slots.CompleterId;
            Func<int, string> completerVar = slots.CompleterVar;
            List<(string Text, bool IsYou)> names = CollectCompleters(viewer, schedule, challenge);
            if (names.Count == 0)
            {
                CustomHud.SetText(viewer, slots.Panel, completerVar(0), Context.Text(viewer, "hud.menu.solved_by.empty"));
                CustomHud.SetHasClass(viewer, completerId(0), "is-off", false);
                CustomHud.SetHasClass(viewer, completerId(0), "is-empty", true);
                CustomHud.SetHasClass(viewer, completerId(0), "is-you", false);
                for (int b = 1; b < CompleterSlots; b++)
                {
                    ClearCompleter(viewer, slots.Panel, completerId, completerVar, b);
                }

                return;
            }

            int overflow = names.Count > CompleterSlots ? names.Count - (CompleterSlots - 1) : 0;
            int shown = overflow > 0 ? CompleterSlots - 1 : names.Count;

            for (int b = 0; b < CompleterSlots; b++)
            {
                if (b == CompleterSlots - 1 && overflow > 0)
                {
                    CustomHud.SetText(viewer, slots.Panel, completerVar(b), Context.FormatOverflow(viewer, overflow));
                    CustomHud.SetHasClass(viewer, completerId(b), "is-off", false);
                    CustomHud.SetHasClass(viewer, completerId(b), "is-empty", true);
                    CustomHud.SetHasClass(viewer, completerId(b), "is-you", false);
                    continue;
                }

                if (b >= shown)
                {
                    ClearCompleter(viewer, slots.Panel, completerId, completerVar, b);
                    continue;
                }

                (string text, bool isYou) = names[b];
                CustomHud.SetText(viewer, slots.Panel, completerVar(b), text);
                CustomHud.SetHasClass(viewer, completerId(b), "is-off", false);
                CustomHud.SetHasClass(viewer, completerId(b), "is-empty", false);
                CustomHud.SetHasClass(viewer, completerId(b), "is-you", isYou);
            }
        }

        private static List<(string Text, bool IsYou)> CollectCompleters(
            CCSPlayerController viewer,
            RunningSchedule? schedule,
            ChallengeDefinition challenge)
        {
            List<(string Text, bool IsYou)> names = [];
            if (schedule == null)
            {
                return names;
            }

            string? youLabel = null;
            foreach (CCSPlayerController human in Players.GetHumans())
            {
                if (Context.GetState(human) is not { } state
                    || !ChallengeProgress.IsChallengeSolved(state, schedule.Key, challenge))
                {
                    continue;
                }

                if (human == viewer)
                {
                    youLabel ??= Context.Text(viewer, "hud.menu.you");
                    names.Insert(0, (youLabel, true));
                }
                else
                {
                    names.Add((human.PlayerName, false));
                }
            }

            return names;
        }

        private static void ClearTask(CCSPlayerController player, Slots slots, int task)
        {
            CustomHud.SetText(player, slots.Panel, slots.TaskVar(task), string.Empty);
            CustomHud.SetHasClass(player, slots.TaskId(task), "is-off", true);
            CustomHud.SetHasClass(player, slots.TaskId(task), "is-done", false);
            CustomHud.SetHasClass(player, slots.TaskId(task), "is-broken", false);
        }

        private static void ClearCompleter(
            CCSPlayerController player,
            Slots slots,
            int slot)
        {
            if (slots.CompleterId == null || slots.CompleterVar == null)
            {
                return;
            }

            ClearCompleter(player, slots.Panel, slots.CompleterId, slots.CompleterVar, slot);
        }

        private static void ClearCompleter(
            CCSPlayerController player,
            string panel,
            Func<int, string> completerId,
            Func<int, string> completerVar,
            int slot)
        {
            CustomHud.SetText(player, panel, completerVar(slot), string.Empty);
            CustomHud.SetHasClass(player, completerId(slot), "is-off", true);
            CustomHud.SetHasClass(player, completerId(slot), "is-empty", false);
            CustomHud.SetHasClass(player, completerId(slot), "is-you", false);
        }
    }
}
