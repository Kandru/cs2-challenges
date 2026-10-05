using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerDeath : IExtractor
    {
        public string EventClassName => "EventPlayerDeath";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_kill", "player_kill_assist", "player_death"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerDeath)gameEvent;
            var attacker = e.Attacker;
            var assister = e.Assister;
            var victim = e.Userid;
            d["isteamkill"] = attacker != null && victim != null ? EventData.Bool(attacker.TeamNum == victim.TeamNum) : "false";
            d["isselfkill"] = EventData.Bool(attacker == victim);
            d["assistedflash"] = EventData.Bool(e.Assistedflash);
            d["attackerblind"] = EventData.Bool(e.Attackerblind);
            d["attackerinair"] = EventData.Bool(e.Attackerinair);
            d["distance"] = EventData.Num(e.Distance);
            d["dmgarmor"] = EventData.Num(e.DmgArmor);
            d["dmghealth"] = EventData.Num(e.DmgHealth);
            d["dominated"] = EventData.Bool(e.Dominated > 0);
            d["headshot"] = EventData.Bool(e.Headshot);
            d["hitgroup"] = EventData.Num(e.Hitgroup);
            d["noscope"] = EventData.Bool(e.Noscope);
            d["penetrated"] = EventData.Bool(e.Penetrated > 0);
            d["revenge"] = EventData.Bool(e.Revenge > 0);
            d["thrusmoke"] = EventData.Bool(e.Thrusmoke);
            d["weapon"] = e.Weapon;
            d["weaponitemid"] = e.WeaponItemid;
            EventData.FillPlayer(d, attacker, "attacker");
            EventData.FillPlayer(d, assister, "assister");
            EventData.FillPlayer(d, victim, "victim");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerDeath)gameEvent;
            yield return (e.Assister, "player_kill_assist");
            yield return (e.Attacker, "player_kill");
            yield return (e.Userid, "player_death");
        }
    }
}
