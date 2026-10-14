
namespace Challenges
{
    internal sealed partial class ProgressPipeline
    {
        private enum ResetMode
        {
            Progress,
            Completed,
        }

        /// <summary>Reused candidate scratch list of <see cref="SimulateTarget"/>. Worker thread only.</summary>
        private readonly List<(ChallengeDefinition Challenge, ChallengeTask Task)> _eligible = [];

        private void SimulateWorkItem(ProgressWorkItem item)
        {
            foreach (ProgressTarget target in item.Targets)
            {
                try
                {
                    SimulateTarget(item, target);
                }
                catch (Exception ex)
                {
                    Log($"progress simulate failed: {ex.Message}");
                    PostDone(item, target, failed: true);
                }
            }
        }

        private void SimulateTarget(ProgressWorkItem item, ProgressTarget target)
        {
            if (GetOrInstallShadow(target) is not { } shadow)
            {
                Log($"progress shadow missing for {target.SteamId}; skipping one event");
                PostDone(item, target, failed: true);
                return;
            }

            string scheduleKey = item.ScheduleKey;

            List<(ChallengeDefinition Challenge, ChallengeTask Task)> eligible = _eligible;
            eligible.Clear();
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in target.Candidates)
            {
                if (!ChallengeRules.IsSkipped(
                        shadow,
                        scheduleKey,
                        challenge,
                        task,
                        item.Data,
                        item.Now,
                        item.AllowBots,
                        item.Debug,
                        out string? skip))
                {
                    eligible.Add((challenge, task));
                    continue;
                }

                if (item.Debug)
                {
                    Log($"{target.Name} {item.Source} {challenge.Id}/{task.Id} skip {skip}");
                }
            }

            if (eligible.Count == 0)
            {
                PostDone(item, target);
                return;
            }

            ProgressPatch patch = new()
            {
                SteamId = target.SteamId,
                Slot = target.Slot,
                ScheduleKey = scheduleKey,
                Schedule = item.Schedule,
            };

            List<TrackerProgressItem>? touched = null;
            foreach ((ChallengeDefinition challenge, ChallengeTask task) in eligible)
            {
                if (ChallengeProgress.IsTaskComplete(shadow, scheduleKey, challenge.Id, task))
                {
                    if (item.Debug)
                    {
                        Log($"{target.Name} {item.Source} {challenge.Id}/{task.Id} skip done");
                    }

                    continue;
                }

                TrackerProgressKind kind = SimulateProgress(shadow, patch, item, target, challenge, task);
                if (!task.Visible)
                {
                    continue;
                }

                (touched ??= []).Add(new TrackerProgressItem
                {
                    ChallengeId = challenge.Id,
                    TaskId = task.Id,
                    Kind = kind,
                });
            }

            patch.Touched = touched;
            PostPatch(patch);
        }

        private void PostDone(ProgressWorkItem item, ProgressTarget target, bool failed = false) =>
            PostPatch(ProgressPatch.Done(target.SteamId, target.Slot, item.ScheduleKey, item.Schedule, failed));

        /// <summary>
        /// Installs the seed when the item carries one, else returns the kept shadow. Null when neither
        /// exists (must not happen; simulating on an empty shadow would overwrite real progress).
        /// </summary>
        private PlayerState? GetOrInstallShadow(ProgressTarget target)
        {
            if (target.Seed is { } seed)
            {
                PlayerState fresh = new()
                {
                    SteamId = target.SteamId,
                    Challenges = seed.Challenges,
                    Statistics = new PlayerStatistics { AmountChallengesSolved = seed.Solved },
                };
                _shadows[target.SteamId] = fresh;
                return fresh;
            }

            return _shadows.TryGetValue(target.SteamId, out PlayerState? existing) ? existing : null;
        }

        private TrackerProgressKind SimulateProgress(
            PlayerState shadow,
            ProgressPatch patch,
            ProgressWorkItem item,
            ProgressTarget target,
            ChallengeDefinition challenge,
            ChallengeTask task)
        {
            string scheduleKey = item.ScheduleKey;
            bool wasSolved = ChallengeProgress.IsChallengeSolved(shadow, scheduleKey, challenge);
            TaskProgress progress = ChallengeProgress.GetOrCreate(shadow, scheduleKey, challenge.Id, task.Id);
            progress.Amount++;
            progress.LastUpdate = item.Now;
            patch.Upsert(challenge.Id, task.Id, progress);

            int goal = Math.Max(1, task.Amount);
            int amount = progress.Amount;
            if (amount < goal)
            {
                AddProgressDebug(patch, item, target, challenge, task, $"+1 {amount}/{goal}");
                patch.AddEffect(ProgressEffectKind.NotifyProgress, challenge, task, amount);
                patch.AddEffect(ProgressEffectKind.TriggerProgress, challenge, task, amount);
                return TrackerProgressKind.Progress;
            }

            patch.CompletesTask = true;
            bool solvedNow = task.Visible
                && !wasSolved
                && ChallengeProgress.IsChallengeSolved(shadow, scheduleKey, challenge);
            if (solvedNow)
            {
                shadow.Statistics.AmountChallengesSolved++;
                patch.SolvedAbsolute = shadow.Statistics.AmountChallengesSolved;
            }

            AddProgressDebug(patch, item, target, challenge, task, solvedNow ? $"solved {amount}/{goal}" : $"complete {amount}/{goal}");
            patch.AddEffect(ProgressEffectKind.NotifyCompletion, challenge, task);
            patch.AddEffect(
                ProgressEffectKind.DiscordCompletion,
                challenge,
                task,
                challengeSolvedNow: solvedNow);
            SimulateActions(shadow, patch, item, target, challenge, task);
            patch.AddEffect(ProgressEffectKind.TriggerCompleted, challenge, task);

            if (!solvedNow)
            {
                return TrackerProgressKind.TaskSolved;
            }

            patch.AddEffect(ProgressEffectKind.TriggerChallengeSolved, challenge);
            return TrackerProgressKind.ChallengeSolved;
        }

        private void SimulateActions(
            PlayerState shadow,
            ProgressPatch patch,
            ProgressWorkItem item,
            ProgressTarget target,
            ChallengeDefinition challenge,
            ChallengeTask owner)
        {
            foreach (ChallengeAction action in owner.Actions)
            {
                switch (action.Type)
                {
                    case "task.reset_progress":
                        foreach (string id in action.Values)
                        {
                            SimulateReset(shadow, patch, item, target, challenge, owner, id, ResetMode.Progress);
                        }
                        break;
                    case "task.reset_completed":
                        foreach (string id in action.Values)
                        {
                            SimulateReset(shadow, patch, item, target, challenge, owner, id, ResetMode.Completed);
                        }
                        break;
                    case "task.mark_completed":
                        foreach (string id in action.Values)
                        {
                            SimulateMarkCompleted(shadow, patch, item, target, challenge, owner, id);
                        }
                        break;
                    case "notify.player.progress.rule_broken":
                    case "notify.player.completed.rule_broken":
                        bool completed = action.Type == "notify.player.completed.rule_broken";
                        if (action.Values.Any(id => completed
                            ? HasCompleted(shadow, item.ScheduleKey, challenge, id)
                            : HasProgress(shadow, item.ScheduleKey, challenge, id)))
                        {
                            patch.AddEffect(ProgressEffectKind.NotifyRuleBroken, challenge, owner);
                        }
                        break;
                    case "server.runcommand" when action.Values.Count >= 1:
                        patch.AddEffect(
                            ProgressEffectKind.ServerCommand,
                            challenge,
                            owner,
                            command: action.Values[0]
                                .Replace("{steamid}", target.SteamId)
                                .Replace("{userid}", target.UserId?.ToString() ?? string.Empty)
                                .Replace("{index}", target.Index.ToString()));
                        break;
                    default:
                        if (item.Debug)
                        {
                            patch.AddDebug($"{target.Name} {action.Type} {challenge.Id}/{owner.Id} skip unknown");
                        }

                        break;
                }
            }
        }

        private void SimulateReset(
            PlayerState shadow,
            ProgressPatch patch,
            ProgressWorkItem item,
            ProgressTarget target,
            ChallengeDefinition challenge,
            ChallengeTask owner,
            string taskId,
            ResetMode mode)
        {
            ChallengeTask? resetTarget = FindTask(challenge, taskId);
            if (resetTarget == null
                || ChallengeProgress.GetProgress(shadow, item.ScheduleKey, challenge.Id, taskId) is not { } progress)
            {
                return;
            }

            bool complete = progress.Amount >= Math.Max(1, resetTarget.Amount);
            if (mode == ResetMode.Progress ? complete : !complete)
            {
                return;
            }

            shadow.Challenges[item.ScheduleKey][challenge.Id].Remove(taskId);
            patch.AddRemoval(challenge.Id, taskId);
            if (item.Debug)
            {
                string action = mode == ResetMode.Progress ? "task.reset_progress" : "task.reset_completed";
                patch.AddDebug($"{target.Name} {action} {challenge.Id}/{owner.Id} reset {taskId}");
            }

            if (resetTarget.Visible && resetTarget.Id != owner.Id)
            {
                patch.AddEffect(ProgressEffectKind.NotifyTaskReset, challenge, resetTarget);
            }
        }

        private static void SimulateMarkCompleted(
            PlayerState shadow,
            ProgressPatch patch,
            ProgressWorkItem item,
            ProgressTarget target,
            ChallengeDefinition challenge,
            ChallengeTask owner,
            string taskId)
        {
            ChallengeTask? markTarget = FindTask(challenge, taskId);
            if (markTarget == null)
            {
                return;
            }

            TaskProgress progress = ChallengeProgress.GetOrCreate(shadow, item.ScheduleKey, challenge.Id, taskId);
            progress.Amount = Math.Max(progress.Amount, Math.Max(1, markTarget.Amount));
            progress.LastUpdate = item.Now;
            patch.Upsert(challenge.Id, taskId, progress);
            patch.CompletesTask = true;
            if (item.Debug)
            {
                patch.AddDebug($"{target.Name} task.mark_completed {challenge.Id}/{owner.Id} mark {taskId}");
            }
        }

        private static void AddProgressDebug(
            ProgressPatch patch,
            ProgressWorkItem item,
            ProgressTarget target,
            ChallengeDefinition challenge,
            ChallengeTask task,
            string outcome)
        {
            if (!item.Debug)
            {
                return;
            }

            string parms = ChallengeRules.FormatParams(task, item.Data);
            patch.AddDebug(parms.Length == 0
                ? $"{target.Name} {item.Source} {challenge.Id}/{task.Id} {outcome}"
                : $"{target.Name} {item.Source} {challenge.Id}/{task.Id} {outcome} {parms}");
        }

        private static bool HasProgress(PlayerState state, string scheduleKey, ChallengeDefinition challenge, string taskId) =>
            ChallengeProgress.GetTaskAmount(state, scheduleKey, challenge.Id, taskId) > 0;

        private static bool HasCompleted(PlayerState state, string scheduleKey, ChallengeDefinition challenge, string taskId) =>
            FindTask(challenge, taskId) is { } task
            && ChallengeProgress.IsTaskComplete(state, scheduleKey, challenge.Id, task);

        private static ChallengeTask? FindTask(ChallengeDefinition challenge, string taskId) =>
            challenge.TaskById.GetValueOrDefault(taskId);
    }
}
