using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class ExitBuyzone : IExtractor
    {
        public string EventClassName => "EventExitBuyzone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["exit_buyzone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventExitBuyzone)gameEvent;
            d["canbuy"] = EventData.Bool(e.Canbuy);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventExitBuyzone)gameEvent;
            yield return (e.Userid, "exit_buyzone");
        }
    }
}
