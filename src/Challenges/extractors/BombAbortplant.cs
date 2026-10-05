using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombAbortplant : IExtractor
    {
        public string EventClassName => "EventBombAbortplant";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_abortplant"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombAbortplant)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBombAbortplant)gameEvent;
            yield return (e.Userid, "player_bomb_abortplant");
        }
    }
}
