using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class ChallengeEngine
    {
        private void ApplyPatch(ProgressPatch patch)
        {
            if (!patch.HasChanges)
            {
                if (patch.Failed)
                {
                    DiscardShadow(patch.SteamId);
                }

                FinishPending(patch.SteamId);
                return;
            }

            PlayerState state = Archive.GetOrCreate(patch.SteamId);
            string scheduleKey = patch.ScheduleKey;

            if (patch.Removals != null)
            {
                foreach ((string challengeId, string taskId) in patch.Removals)
                {
                    if (state.Challenges.TryGetValue(scheduleKey, out var challenges)
                        && challenges.TryGetValue(challengeId, out var tasks)
                        && tasks.Remove(taskId))
                    {
                        state.Dirty = true;
                    }
                }
            }

            if (patch.Upserts != null)
            {
                foreach (ProgressUpsert upsert in patch.Upserts)
                {
                    TaskProgress progress = ChallengeProgress.GetOrCreate(
                        state, scheduleKey, upsert.ChallengeId, upsert.TaskId);
                    progress.Amount = upsert.Amount;
                    progress.LastUpdate = upsert.LastUpdate;
                    state.Dirty = true;
                }
            }

            if (patch.SolvedAbsolute is int solved)
            {
                state.Statistics.AmountChallengesSolved = solved;
                state.Dirty = true;
            }

            if (patch.DebugLines is { Count: > 0 })
            {
                foreach (string line in patch.DebugLines)
                {
                    DebugPrint(line);
                }
            }

            if (SteamIds.GetPlayerIfSteam(patch.Slot, patch.SteamId) is { } player)
            {
                PlayEffects(player, state, patch);
                if (patch.Touched is { Count: > 0 } touched && GlobalConfig.Gui.ShowOnProgress)
                {
                    Tracker.ShowProgress(player, touched);
                }

                if (HudMenu.IsOpen(player))
                {
                    Menu.Paint(player);
                }
            }

            FinishPending(patch.SteamId);
        }

        private void PlayEffects(CCSPlayerController player, PlayerState state, ProgressPatch patch)
        {
            if (patch.Effects == null)
            {
                return;
            }

            foreach (ProgressEffect effect in patch.Effects)
            {
                switch (effect.Kind)
                {
                    case ProgressEffectKind.NotifyProgress when effect.Task != null:
                        Notes.NotifyProgress(player, effect.Challenge, effect.Task, effect.Amount);
                        break;
                    case ProgressEffectKind.NotifyCompletion when effect.Task != null:
                        Notes.NotifyCompletion(player, effect.Challenge, effect.Task);
                        break;
                    case ProgressEffectKind.DiscordCompletion when effect.Task != null:
                        Notes.DiscordCompletion(
                            player,
                            state,
                            patch.Schedule,
                            effect.Challenge,
                            effect.Task,
                            effect.ChallengeSolvedNow);
                        break;
                    case ProgressEffectKind.NotifyRuleBroken when effect.Task != null:
                        Notes.NotifyRuleBroken(player, effect.Challenge, effect.Task);
                        break;
                    case ProgressEffectKind.NotifyTaskReset when effect.Task != null:
                        Notes.NotifyTaskReset(player, effect.Challenge, effect.Task);
                        break;
                    case ProgressEffectKind.ServerCommand when effect.Command is { Length: > 0 }:
                        Server.ExecuteCommand(effect.Command);
                        break;
                    case ProgressEffectKind.TriggerProgress when effect.Task != null:
                        TriggerProgress(player, effect.Challenge, effect.Task, effect.Amount);
                        break;
                    case ProgressEffectKind.TriggerCompleted when effect.Task != null:
                        TriggerCompleted(player, effect.Challenge, effect.Task);
                        break;
                    case ProgressEffectKind.TriggerChallengeSolved:
                        TriggerChallengeSolved(player, effect.Challenge);
                        break;
                }
            }
        }
    }
}
