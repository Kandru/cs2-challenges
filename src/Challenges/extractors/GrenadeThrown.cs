using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class GrenadeThrown : IExtractor
    {
        public string EventClassName => "EventGrenadeThrown";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["grenade_thrown"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventGrenadeThrown)gameEvent;
            d["weapon"] = e.Weapon.ToString();
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventGrenadeThrown)gameEvent;
            yield return (e.Userid, "grenade_thrown");
        }
    }
}
