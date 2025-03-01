using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || !attacker.IsValid
                || attacker.IsBot
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // hide GUI for victim
            HideGui(victim);
            // check if we have an assister and add kill assists
            CCSPlayerController? assister = @event.Assister;
            if (assister != null
                && assister.IsValid
                && !assister.IsBot
                && _playerConfigs.ContainsKey(assister.NetworkIDString))
            {
                CheckChallengeGoal(assister, "kill_assist", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "isteamkill", (attacker.TeamNum == victim.TeamNum).ToString() },
                    { "isselfkill", (attacker == victim).ToString() },
                    { "attacker", attacker.PlayerName },
                    { "attacker_isbot", attacker.IsBot.ToString() },
                    { "attacker_team", attacker.Team.ToString() },
                    { "assister", assister.PlayerName },
                    { "assister_isbot", assister.IsBot.ToString() },
                    { "assister_team", assister.Team.ToString() },
                    { "victim", victim.PlayerName },
                    { "victim_isbot", victim.IsBot.ToString() },
                    { "victim_team", victim.Team.ToString() },
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
                });
            }
            // check for attacker goal
            CheckChallengeGoal(attacker, "kill", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamkill", (attacker.TeamNum == victim.TeamNum).ToString() },
                { "isselfkill", (attacker == victim).ToString() },
                { "attacker", attacker.PlayerName },
                { "attacker_isbot", attacker.IsBot.ToString() },
                { "attacker_team", attacker.Team.ToString() },
                { "victim", victim.PlayerName },
                { "victim_isbot", victim.IsBot.ToString() },
                { "victim_team", victim.Team.ToString() },
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
            });
            // check for victim goal
            CheckChallengeGoal(attacker, "death", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamkill", (attacker.TeamNum == victim.TeamNum).ToString() },
                { "isselfkill", (attacker == victim).ToString() },
                { "attacker", attacker.PlayerName },
                { "attacker_isbot", attacker.IsBot.ToString() },
                { "attacker_team", attacker.Team.ToString() },
                { "victim", victim.PlayerName },
                { "victim_isbot", victim.IsBot.ToString() },
                { "victim_team", victim.Team.ToString() },
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
            });
            return HookResult.Continue;
        }
    }
}
