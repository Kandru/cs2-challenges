using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombPlanted : IExtractor
    {
        public string EventClassName => "EventBombPlanted";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_planted"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombPlanted)gameEvent;
            var planter = e.Userid;
            d["bomb_site"] = EventData.Num(e.Site);
            EventData.FillPlayer(d, planter, "planter");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "player_bomb_planted");
            }
        }
    }
}
