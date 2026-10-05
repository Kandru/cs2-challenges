using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges.Extractors
{
    public sealed class BuytimeEnded : IExtractor
    {
        public string EventClassName => "EventBuytimeEnded";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["buytime_ended"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            // No event-level keys (old handler only merged global + per-player data).
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "buytime_ended");
            }
        }
    }
}
