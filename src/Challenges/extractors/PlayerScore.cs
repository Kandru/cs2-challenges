using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerScore : IExtractor
    {
        public string EventClassName => "EventPlayerScore";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_score"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerScore)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerScore)gameEvent;
            yield return (e.Userid, "player_score");
        }
    }
}
