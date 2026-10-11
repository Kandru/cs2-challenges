using Challenges.Configs;
using Challenges.Enums;

namespace Challenges.Utils
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
        public List<ProgressUpsert> Upserts { get; } = [];
        public List<(string ChallengeId, string TaskId)> Removals { get; } = [];
        public int? SolvedAbsolute { get; set; }
        public List<ProgressEffect> Effects { get; } = [];
        public List<TrackerProgressItem>? Touched { get; set; }
        public List<string>? DebugLines { get; set; }

        public bool HasChanges =>
            Upserts.Count > 0
            || Removals.Count > 0
            || SolvedAbsolute != null
            || Effects.Count > 0;

        public void Upsert(string challengeId, string taskId, TaskProgress progress)
        {
            for (int i = 0; i < Upserts.Count; i++)
            {
                ProgressUpsert existing = Upserts[i];
                if (existing.ChallengeId != challengeId || existing.TaskId != taskId)
                {
                    continue;
                }

                Upserts[i] = new ProgressUpsert(challengeId, taskId, progress.Amount, progress.LastUpdate);
                return;
            }

            Upserts.Add(new ProgressUpsert(challengeId, taskId, progress.Amount, progress.LastUpdate));
        }

        public void AddDebug(string line) => (DebugLines ??= []).Add(line);

        public void AddEffect(
            ProgressEffectKind kind,
            ChallengeDefinition challenge,
            ChallengeTask? task = null,
            int amount = 0,
            bool challengeSolvedNow = false,
            string? command = null) =>
            Effects.Add(new ProgressEffect(kind, challenge, task, amount, challengeSolvedNow, command));
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
