using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombBeginplant : IExtractor
    {
        public string EventClassName => "EventBombBeginplant";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_beginplant"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombBeginplant)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBombBeginplant)gameEvent;
            yield return (e.Userid, "player_bomb_beginplant");
        }
    }
}
