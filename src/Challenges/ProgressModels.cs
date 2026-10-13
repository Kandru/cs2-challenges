namespace Challenges
{
    internal sealed class ProgressWorkItem
    {
        public required string Source { get; init; }
        public required Dictionary<string, string> Data { get; init; }
        public required string ScheduleKey { get; init; }
        public required RunningSchedule Schedule { get; init; }
        public required bool Debug { get; init; }
        public required bool AllowBots { get; init; }
        public required long Now { get; init; }
        public required List<ProgressTarget> Targets { get; init; }
    }

    internal sealed class ProgressTarget
    {
        public required string SteamId { get; init; }
        public required int Slot { get; init; }
        public required int? UserId { get; init; }
        public required string Name { get; init; }
        public required uint Index { get; init; }
        public required List<(ChallengeDefinition Challenge, ChallengeTask Task)> Candidates { get; init; }
        public ProgressSeed? Seed { get; set; }
    }

    internal sealed class ProgressSeed
    {
        public required Dictionary<string, Dictionary<string, Dictionary<string, TaskProgress>>> Challenges { get; init; }
        public required int Solved { get; init; }
    }

    internal sealed class ProgressPatch
    {
        public required string SteamId { get; init; }
        public required int Slot { get; init; }
        public required string ScheduleKey { get; init; }
        public required RunningSchedule Schedule { get; init; }
        /// <summary>Null until the first <see cref="Upsert"/>.</summary>
        public List<ProgressUpsert>? Upserts { get; private set; }
        /// <summary>Null until the first <see cref="AddRemoval"/>.</summary>
        public List<(string ChallengeId, string TaskId)>? Removals { get; private set; }
        public int? SolvedAbsolute { get; set; }
        /// <summary>Null until the first <see cref="AddEffect"/>.</summary>
        public List<ProgressEffect>? Effects { get; private set; }
        public List<TrackerProgressItem>? Touched { get; set; }
        public List<string>? DebugLines { get; set; }
        /// <summary>Completion-only result of a target the worker could not simulate; the shadow is suspect.</summary>
        public bool Failed { get; init; }

        public bool HasChanges =>
            Upserts != null
            || Removals != null
            || SolvedAbsolute != null
            || Effects != null;

        /// <summary>
        /// Completion-only result: tells the game thread that one in-flight target finished without changes.
        /// </summary>
        public static ProgressPatch Done(
            string steamId,
            int slot,
            string scheduleKey,
            RunningSchedule schedule,
            bool failed = false) =>
            new()
            {
                SteamId = steamId,
                Slot = slot,
                ScheduleKey = scheduleKey,
                Schedule = schedule,
                Failed = failed,
            };

        public void Upsert(string challengeId, string taskId, TaskProgress progress)
        {
            List<ProgressUpsert> upserts = Upserts ??= [];
            for (int i = 0; i < upserts.Count; i++)
            {
                ProgressUpsert existing = upserts[i];
                if (existing.ChallengeId != challengeId || existing.TaskId != taskId)
                {
                    continue;
                }

                upserts[i] = new ProgressUpsert(challengeId, taskId, progress.Amount, progress.LastUpdate);
                return;
            }

            upserts.Add(new ProgressUpsert(challengeId, taskId, progress.Amount, progress.LastUpdate));
        }

        public void AddRemoval(string challengeId, string taskId) => (Removals ??= []).Add((challengeId, taskId));

        public void AddDebug(string line) => (DebugLines ??= []).Add(line);

        public void AddEffect(
            ProgressEffectKind kind,
            ChallengeDefinition challenge,
            ChallengeTask? task = null,
            int amount = 0,
            bool challengeSolvedNow = false,
            string? command = null) =>
            (Effects ??= []).Add(new ProgressEffect(kind, challenge, task, amount, challengeSolvedNow, command));
    }

    internal readonly record struct ProgressUpsert(
        string ChallengeId,
        string TaskId,
        int Amount,
        long LastUpdate);

    internal enum ProgressEffectKind
    {
        NotifyProgress,
        NotifyCompletion,
        DiscordCompletion,
        NotifyRuleBroken,
        NotifyTaskReset,
        ServerCommand,
        TriggerProgress,
        TriggerCompleted,
        TriggerChallengeSolved,
    }

    internal readonly record struct ProgressEffect(
        ProgressEffectKind Kind,
        ChallengeDefinition Challenge,
        ChallengeTask? Task,
        int Amount,
        bool ChallengeSolvedNow,
        string? Command);
}
