using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerAvengedTeammate : IExtractor
    {
        public string EventClassName => "EventPlayerAvengedTeammate";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_has_avenged_teammate", "player_got_avenged_teammate"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerAvengedTeammate)gameEvent;
            var avenger = e.AvengerId;
            var victim = e.AvengedPlayerId;
            d["isselfavenged"] = EventData.Bool(avenger == victim);
            EventData.FillPlayer(d, avenger, "avenger");
            EventData.FillPlayer(d, victim, "victim");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerAvengedTeammate)gameEvent;
            yield return (e.AvengerId, "player_has_avenged_teammate");
            yield return (e.AvengedPlayerId, "player_got_avenged_teammate");
        }
    }
}
