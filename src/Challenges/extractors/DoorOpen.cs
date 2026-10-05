using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class DoorOpen : IExtractor
    {
        public string EventClassName => "EventDoorOpen";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["door_open"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventDoorOpen)gameEvent;
            d["entindex"] = EventData.Num(e.Entindex);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventDoorOpen)gameEvent;
            yield return (e.Userid, "door_open");
        }
    }
}
