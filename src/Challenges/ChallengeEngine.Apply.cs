using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class ChallengeEngine
    {
        /// <summary>
        /// Game-side part of a patch with changes, after <see cref="ProgressPipeline"/> wrote its progress into
        /// <paramref name="state"/>: tracker and menu invalidation, debug lines and the player's effects.
        /// </summary>
        private void OnPatchApplied(ProgressPatch patch, PlayerState state)
        {
            if (patch.Upserts != null || patch.Removals != null || patch.SolvedAbsolute != null)
            {
                state.TrackerContentDirty = true;
            }

            // Scoreboard columns count solved challenges; progress below a task goal cannot change them.
            if (patch.SolvedAbsolute != null || patch.Removals != null || patch.CompletesTask)
            {
                Menu.InvalidateScoreboard();
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
                try
                {
                    PlayEffects(player, state, patch);
                }
                finally
                {
                    // Other players get one message for all completions of this patch.
                    Notes.FlushCompletions(player);
                }

                if (patch.Touched is { Count: > 0 } touched && GlobalConfig.Gui.ShowOnProgress)
                {
                    Tracker.ShowProgress(player, touched);
                }

                // Repainted from the roster tick, at most 4 times per second.
                if (HudMenu.IsOpen(player))
                {
                    state.MenuDirty = true;
                }
            }
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
