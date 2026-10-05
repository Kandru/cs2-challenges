using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges.Extractors
{
    public sealed class TeamScore : IExtractor
    {
        public string EventClassName => "EventTeamScore";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["team_score"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            // No event-level keys (old handler only merged global + per-player data).
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "team_score");
            }
        }
    }
}
