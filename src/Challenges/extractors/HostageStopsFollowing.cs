using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class HostageStopsFollowing : IExtractor
    {
        public string EventClassName => "EventHostageStopsFollowing";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["hostage_stops_following"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventHostageStopsFollowing)gameEvent;
            d["hostage"] = EventData.Num(e.Hostage);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventHostageStopsFollowing)gameEvent;
            yield return (e.Userid, "hostage_stops_following");
        }
    }
}
