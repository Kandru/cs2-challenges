using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class HostageFollows : IExtractor
    {
        public string EventClassName => "EventHostageFollows";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["hostage_follows"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventHostageFollows)gameEvent;
            d["hostage"] = EventData.Num(e.Hostage);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventHostageFollows)gameEvent;
            yield return (e.Userid, "hostage_follows");
        }
    }
}
