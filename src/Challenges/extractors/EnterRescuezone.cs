using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class EnterRescuezone : IExtractor
    {
        public string EventClassName => "EventEnterRescueZone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["enter_rescuezone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventEnterRescueZone)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventEnterRescueZone)gameEvent;
            yield return (e.Userid, "enter_rescuezone");
        }
    }
}
