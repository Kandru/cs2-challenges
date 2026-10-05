using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerGivenC4 : IExtractor
    {
        public string EventClassName => "EventPlayerGivenC4";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_givenc4"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerGivenC4)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerGivenC4)gameEvent;
            yield return (e.Userid, "player_givenc4");
        }
    }
}
