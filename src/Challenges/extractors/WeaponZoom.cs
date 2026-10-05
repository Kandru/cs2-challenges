using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class WeaponZoom : IExtractor
    {
        public string EventClassName => "EventWeaponZoom";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["weapon_zoom"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventWeaponZoom)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventWeaponZoom)gameEvent;
            yield return (e.Userid, "weapon_zoom");
        }
    }
}
