using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombDefused : IExtractor
    {
        public string EventClassName => "EventBombDefused";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["bomb_defused"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombDefused)gameEvent;
            var defuser = e.Userid;
            d["bomb_site"] = EventData.Num(e.Site);
            EventData.FillPlayer(d, defuser, "defuser");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "bomb_defused");
            }
        }
    }
}
