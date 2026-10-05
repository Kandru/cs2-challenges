using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerJump : IExtractor
    {
        public string EventClassName => "EventPlayerJump";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_jump"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerJump)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerJump)gameEvent;
            yield return (e.Userid, "player_jump");
        }
    }
}
