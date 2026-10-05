using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BreakProp : IExtractor
    {
        public string EventClassName => "EventBreakProp";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["break_prop"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBreakProp)gameEvent;
            d["entindex"] = EventData.Num(e.Entindex);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBreakProp)gameEvent;
            yield return (e.Userid, "break_prop");
        }
    }
}
