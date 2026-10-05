using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerPing : IExtractor
    {
        public string EventClassName => "EventPlayerPing";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_ping"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerPing)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerPing)gameEvent;
            yield return (e.Userid, "player_ping");
        }
    }
}
