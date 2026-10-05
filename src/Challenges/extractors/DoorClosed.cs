using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class DoorClosed : IExtractor
    {
        public string EventClassName => "EventDoorClosed";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["door_closed"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventDoorClosed)gameEvent;
            d["entindex"] = EventData.Num(e.Entindex);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventDoorClosed)gameEvent;
            yield return (e.Userid, "door_closed");
        }
    }
}
