using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges.Extractors
{
    public sealed class HostageRescuedAll : IExtractor
    {
        public string EventClassName => "EventHostageRescuedAll";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["hostage_rescued_all"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            // No event-level keys (old handler only merged global + per-player data).
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "hostage_rescued_all");
            }
        }
    }
}
