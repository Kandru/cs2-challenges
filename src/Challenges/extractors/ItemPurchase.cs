using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class ItemPurchase : IExtractor
    {
        public string EventClassName => "EventItemPurchase";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["item_purchase"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventItemPurchase)gameEvent;
            d["weapon"] = e.Weapon.ToString();
            d["loadout"] = e.Loadout.ToString();
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventItemPurchase)gameEvent;
            yield return (e.Userid, "item_purchase");
        }
    }
}
