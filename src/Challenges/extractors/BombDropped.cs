using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombDropped : IExtractor
    {
        public string EventClassName => "EventBombDropped";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_dropped"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombDropped)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBombDropped)gameEvent;
            yield return (e.Userid, "player_bomb_dropped");
        }
    }
}
