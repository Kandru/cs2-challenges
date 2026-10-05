using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class HostageHurt : IExtractor
    {
        public string EventClassName => "EventHostageHurt";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["hostage_hurt"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventHostageHurt)gameEvent;
            d["hostage"] = EventData.Num(e.Hostage);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventHostageHurt)gameEvent;
            yield return (e.Userid, "hostage_hurt");
        }
    }
}
