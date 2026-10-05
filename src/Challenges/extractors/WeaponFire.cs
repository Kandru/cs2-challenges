using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class WeaponFire : IExtractor
    {
        public string EventClassName => "EventWeaponFire";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["weapon_fire"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventWeaponFire)gameEvent;
            d["silenced"] = EventData.Bool(e.Silenced);
            d["weapon"] = e.Weapon.ToString();
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventWeaponFire)gameEvent;
            yield return (e.Userid, "weapon_fire");
        }
    }
}
