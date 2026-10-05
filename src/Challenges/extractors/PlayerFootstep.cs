using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerFootstep : IExtractor
    {
        public string EventClassName => "EventPlayerFootstep";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_footstep"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerFootstep)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerFootstep)gameEvent;
            yield return (e.Userid, "player_footstep");
        }
    }
}
