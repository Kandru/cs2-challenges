using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class EnterBuyzone : IExtractor
    {
        public string EventClassName => "EventEnterBuyzone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["enter_buyzone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventEnterBuyzone)gameEvent;
            d["canbuy"] = EventData.Bool(e.Canbuy);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventEnterBuyzone)gameEvent;
            yield return (e.Userid, "enter_buyzone");
        }
    }
}
