using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombExploded : IExtractor
    {
        public string EventClassName => "EventBombExploded";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["bomb_exploded"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombExploded)gameEvent;
            var exploder = e.Userid;
            d["bomb_site"] = EventData.Num(e.Site);
            EventData.FillPlayer(d, exploder, "exploder");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "bomb_exploded");
            }
        }
    }
}
