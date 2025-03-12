using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? assister = @event.Assister;
            CCSPlayerController? victim = @event.Userid;
            if (attacker != null && !_playerConfigs.ContainsKey(attacker.NetworkIDString)
                && assister != null && !_playerConfigs.ContainsKey(assister.NetworkIDString)
                && victim != null && !_playerConfigs.ContainsKey(victim.NetworkIDString)) return HookResult.Continue;
            // hide GUI for victim
            if (victim != null && victim.IsValid) HideGui(victim);
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "isteamkill", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfkill", (attacker == victim).ToString() },
                { "assistedflash", @event.Assistedflash.ToString() },
                { "attackerblind", @event.Attackerblind.ToString() },
                { "attackerinair", @event.Attackerinair.ToString() },
                { "distance", @event.Distance.ToString() },
                { "dmgarmor", @event.DmgArmor.ToString() },
                { "dmghealth", @event.DmgHealth.ToString() },
                { "dominated", (@event.Dominated > 0).ToString() },
                { "headshot", @event.Headshot.ToString() },
                { "hitgroup", @event.Hitgroup.ToString() },
                { "noscope", @event.Noscope.ToString() },
                { "penetrated", (@event.Penetrated > 0).ToString() },
                { "revenge", (@event.Revenge > 0).ToString() },
                { "thrusmoke", @event.Thrusmoke.ToString() },
                { "weapon", @event.Weapon },
                { "weaponitemid", @event.WeaponItemid }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(attacker, "attacker")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(assister, "assister")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(victim, "victim")) challengeData[item.Key] = item.Value;
            // check assister for challenge
            _ = CheckChallengeGoal(assister, "player_kill_assist", challengeData);
            // check attacker for challenge
            _ = CheckChallengeGoal(attacker, "player_kill", challengeData);
            // check victim for challenge
            _ = CheckChallengeGoal(victim, "player_death", challengeData);
            return HookResult.Continue;
        }
    }
}
