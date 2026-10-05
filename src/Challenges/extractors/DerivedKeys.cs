using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Utils;

namespace Challenges.Extractors
{
    /// <summary>Derived rule keys that are not 1:1 CSS fields (team/self flags, enum names).</summary>
    public static class DerivedKeys
    {
        public static void Apply(string kind, GameEvent gameEvent, Dictionary<string, string> d)
        {
            switch (kind)
            {
                case "team_self_kill":
                    WriteTeamSelf((EventPlayerDeath)gameEvent, d, "isteamkill", "isselfkill",
                        e => e.Attacker, e => e.Userid);
                    break;
                case "team_self_damage" when gameEvent is EventPlayerHurt hurt:
                    WriteTeamSelf(hurt, d, "isteamdamage", "isselfdamage",
                        e => e.Attacker, e => e.Userid);
                    break;
                case "team_self_damage" when gameEvent is EventBulletDamage bullet:
                    WriteTeamSelf(bullet, d, "isteamdamage", "isselfdamage",
                        e => e.Attacker, e => e.Victim);
                    break;
                case "team_self_flash":
                    WriteTeamSelf((EventPlayerBlind)gameEvent, d, "isteamflash", "isselfflash",
                        e => e.Attacker, e => e.Userid);
                    break;
                case "self_avenged":
                {
                    var e = (EventPlayerAvengedTeammate)gameEvent;
                    d["isselfavenged"] = EventData.Bool(e.AvengerId == e.AvengedPlayerId);
                    break;
                }
                case "player_team_names":
                {
                    var e = (EventPlayerTeam)gameEvent;
                    d["old_team"] = Enum.GetName(typeof(CsTeam), e.Oldteam) ?? "Unknown";
                    d["new_team"] = Enum.GetName(typeof(CsTeam), e.Team) ?? "Unknown";
                    break;
                }
            }
        }

        private static void WriteTeamSelf<T>(
            T gameEvent,
            Dictionary<string, string> d,
            string teamKey,
            string selfKey,
            Func<T, CCSPlayerController?> attackerOf,
            Func<T, CCSPlayerController?> victimOf)
        {
            CCSPlayerController? attacker = attackerOf(gameEvent);
            CCSPlayerController? victim = victimOf(gameEvent);
            d[teamKey] = attacker != null && victim != null
                ? EventData.Bool(attacker.TeamNum == victim.TeamNum)
                : "false";
            d[selfKey] = EventData.Bool(attacker == victim);
        }
    }
}
