using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerHurt : IExtractor
    {
        public string EventClassName => "EventPlayerHurt";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_hurt_attacker", "player_hurt_victim"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerHurt)gameEvent;
            var attacker = e.Attacker;
            var victim = e.Userid;
            d["isteamdamage"] = attacker != null && victim != null ? EventData.Bool(attacker.TeamNum == victim.TeamNum) : "false";
            d["isselfdamage"] = EventData.Bool(attacker == victim);
            d["dmghealth"] = EventData.Num(e.DmgHealth);
            d["dmgarmor"] = EventData.Num(e.DmgArmor);
            d["health"] = EventData.Num(e.Health);
            d["armor"] = EventData.Num(e.Armor);
            d["hitgroup"] = EventData.Num(e.Hitgroup);
            d["weapon"] = e.Weapon;
            EventData.FillPlayer(d, attacker, "attacker");
            EventData.FillPlayer(d, victim, "victim");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerHurt)gameEvent;
            yield return (e.Attacker, "player_hurt_attacker");
            yield return (e.Userid, "player_hurt_victim");
        }
    }
}
