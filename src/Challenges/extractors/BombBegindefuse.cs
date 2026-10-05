using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombBegindefuse : IExtractor
    {
        public string EventClassName => "EventBombBegindefuse";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_begindefuse"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombBegindefuse)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBombBegindefuse)gameEvent;
            yield return (e.Userid, "player_bomb_begindefuse");
        }
    }
}
