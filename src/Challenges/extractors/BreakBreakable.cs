using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BreakBreakable : IExtractor
    {
        public string EventClassName => "EventBreakBreakable";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["break_breakable"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBreakBreakable)gameEvent;
            d["entindex"] = EventData.Num(e.Entindex);
            d["material"] = e.Material.ToString();
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBreakBreakable)gameEvent;
            yield return (e.Userid, "break_breakable");
        }
    }
}
