using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class AmmoPickup : IExtractor
    {
        public string EventClassName => "EventAmmoPickup";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_ammo_pickup"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventAmmoPickup)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventAmmoPickup)gameEvent;
            yield return (e.Userid, "player_ammo_pickup");
        }
    }
}
