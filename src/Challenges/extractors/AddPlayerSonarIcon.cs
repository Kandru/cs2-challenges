using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class AddPlayerSonarIcon : IExtractor
    {
        public string EventClassName => "EventAddPlayerSonarIcon";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_add_sonar_icon"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventAddPlayerSonarIcon)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventAddPlayerSonarIcon)gameEvent;
            yield return (e.Userid, "player_add_sonar_icon");
        }
    }
}
