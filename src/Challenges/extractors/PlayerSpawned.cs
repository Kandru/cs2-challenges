using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerSpawned : IExtractor
    {
        public string EventClassName => "EventPlayerSpawned";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_spawned"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerSpawned)gameEvent;
            d["inrestart"] = EventData.Bool(e.Inrestart);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerSpawned)gameEvent;
            yield return (e.Userid, "player_spawned");
        }
    }
}
