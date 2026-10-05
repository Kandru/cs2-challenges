using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerRadio : IExtractor
    {
        public string EventClassName => "EventPlayerRadio";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_radio"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerRadio)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerRadio)gameEvent;
            yield return (e.Userid, "player_radio");
        }
    }
}
