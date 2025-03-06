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
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamkill", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfkill", (attacker == victim).ToString() },
                { "attacker", attacker != null && attacker.IsValid ? attacker.PlayerName : "" },
                { "attacker_isbot", attacker != null && attacker.IsValid ? attacker.IsBot.ToString() : "" },
                { "attacker_team", attacker != null && attacker.IsValid ? attacker.Team.ToString() : "" },
                { "assister", assister != null && assister.IsValid ? assister.PlayerName : "" },
                { "assister_isbot", assister != null && assister.IsValid ? assister.IsBot.ToString() : "" },
                { "assister_team", assister != null && assister.IsValid ? assister.Team.ToString() : "" },
                { "victim", victim != null && victim.IsValid ? victim.PlayerName : "" },
                { "victim_isbot", victim != null && victim.IsValid ? victim.IsBot.ToString() : "" },
                { "victim_team", victim != null && victim.IsValid ? victim.Team.ToString() : "" },
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
            // check assister for challenge
            CheckChallengeGoal(assister, "player_kill_assist", challengeData);
            // check attacker for challenge
            CheckChallengeGoal(attacker, "player_kill", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_death", challengeData);
            return HookResult.Continue;
        }
    }
}
