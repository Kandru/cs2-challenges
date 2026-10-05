using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class HostageRescued : IExtractor
    {
        public string EventClassName => "EventHostageRescued";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["hostage_rescued"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventHostageRescued)gameEvent;
            var rescuer = e.Userid;
            d["hostage"] = EventData.Num(e.Hostage);
            d["rescue_site"] = EventData.Num(e.Site);
            EventData.FillPlayer(d, rescuer, "rescuer");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "hostage_rescued");
            }
        }
    }
}
