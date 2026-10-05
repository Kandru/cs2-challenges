using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class InspectWeapon : IExtractor
    {
        public string EventClassName => "EventInspectWeapon";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["inspect_weapon"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventInspectWeapon)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventInspectWeapon)gameEvent;
            yield return (e.Userid, "inspect_weapon");
        }
    }
}
