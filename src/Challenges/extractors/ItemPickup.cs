using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class ItemPickup : IExtractor
    {
        public string EventClassName => "EventItemPickup";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["item_pickup"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventItemPickup)gameEvent;
            d["item"] = e.Item.ToString();
            d["defindex"] = EventData.Num(e.Defindex);
            d["silent"] = EventData.Bool(e.Silent);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventItemPickup)gameEvent;
            yield return (e.Userid, "item_pickup");
        }
    }
}
