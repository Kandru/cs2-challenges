using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombAbortdefuse : IExtractor
    {
        public string EventClassName => "EventBombAbortdefuse";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bomb_abortdefuse"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombAbortdefuse)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBombAbortdefuse)gameEvent;
            yield return (e.Userid, "player_bomb_abortdefuse");
        }
    }
}
