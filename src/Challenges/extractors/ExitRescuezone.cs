using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class ExitRescuezone : IExtractor
    {
        public string EventClassName => "EventExitRescueZone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["exit_rescuezone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventExitRescueZone)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventExitRescueZone)gameEvent;
            yield return (e.Userid, "exit_rescuezone");
        }
    }
}
