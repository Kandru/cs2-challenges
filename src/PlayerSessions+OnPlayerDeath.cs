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
            if (attacker == null
                || !attacker.IsValid
                || attacker.IsBot
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // hide GUI for victim
            HideGui(victim);
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamkill", (attacker.TeamNum == victim.TeamNum).ToString() },
                { "isselfkill", (attacker == victim).ToString() },
                { "attacker", attacker.PlayerName },
                { "attacker_isbot", attacker.IsBot.ToString() },
                { "attacker_team", attacker.Team.ToString() },
                { "assister", assister != null && assister.IsValid ? assister.PlayerName : "" },
                { "assister_isbot", assister != null && assister.IsValid ? assister.IsBot.ToString() : "" },
                { "assister_team", assister != null && assister.IsValid ? assister.Team.ToString() : "" },
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
            };
            // check assister for challenge
            if (assister != null
                && assister.IsValid
                && !assister.IsBot
                && _playerConfigs.ContainsKey(assister.NetworkIDString))
            {
                CheckChallengeGoal(assister, "player_kill_assist", challengeData);
            }
            // check attacker for challenge
            CheckChallengeGoal(attacker, "player_kill", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_death", challengeData);
            return HookResult.Continue;
        }
    }
}
