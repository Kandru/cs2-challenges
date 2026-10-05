using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class GrenadeBounce : IExtractor
    {
        public string EventClassName => "EventGrenadeBounce";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["grenade_bounce"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventGrenadeBounce)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventGrenadeBounce)gameEvent;
            yield return (e.Userid, "grenade_bounce");
        }
    }
}
