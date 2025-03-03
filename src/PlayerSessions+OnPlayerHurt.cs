using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || !attacker.IsValid
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // check attacker for challenge
            if (_playerConfigs.ContainsKey(attacker.NetworkIDString))
                CheckChallengeGoal(attacker, "player_hurt", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "isteamdamage", (attacker.TeamNum == victim.TeamNum).ToString() },
                    { "isselfdamage", (attacker == victim).ToString() },
                    { "isattacker", "true" },
                    { "attacker", attacker.PlayerName },
                    { "attacker_isbot", attacker.IsBot.ToString() },
                    { "attacker_team", attacker.Team.ToString() },
                    { "isvictim", "false" },
                    { "victim", victim.PlayerName },
                    { "victim_isbot", victim.IsBot.ToString() },
                    { "victim_team", victim.Team.ToString() },
                    { "dmghealth", @event.DmgHealth.ToString() },
                    { "dmgarmor", @event.DmgArmor.ToString() },
                    { "health", @event.Health.ToString() },
                    { "armor", @event.Armor.ToString() },
                    { "hitgroup", @event.Hitgroup.ToString() },
                    { "weapon", @event.Weapon },
                });
            // check victim for challenge
            if (_playerConfigs.ContainsKey(victim.NetworkIDString))
                CheckChallengeGoal(victim, "player_hurt", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "isteamdamage", (attacker.TeamNum == victim.TeamNum).ToString() },
                    { "isselfdamage", (attacker == victim).ToString() },
                    { "isattacker", "false" },
                    { "attacker", attacker.PlayerName },
                    { "attacker_isbot", attacker.IsBot.ToString() },
                    { "attacker_team", attacker.Team.ToString() },
                    { "isvictim", "true" },
                    { "victim", victim.PlayerName },
                    { "victim_isbot", victim.IsBot.ToString() },
                    { "victim_team", victim.Team.ToString() },
                });
            return HookResult.Continue;
        }
    }
}
