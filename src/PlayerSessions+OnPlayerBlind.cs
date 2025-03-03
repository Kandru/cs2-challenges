using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker != null && !_playerConfigs.ContainsKey(attacker.NetworkIDString)
                && victim != null && !_playerConfigs.ContainsKey(victim.NetworkIDString)) return HookResult.Continue;
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamflash", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfflash", (attacker == victim).ToString() },
                { "attacker", attacker != null && attacker.IsValid ? attacker.PlayerName : "" },
                { "attacker_isbot", attacker != null && attacker.IsValid ? attacker.IsBot.ToString() : "" },
                { "attacker_team", attacker != null && attacker.IsValid ? attacker.Team.ToString() : "" },
                { "victim", victim != null && victim.IsValid ? victim.PlayerName : "" },
                { "victim_isbot", victim != null && victim.IsValid ? victim.IsBot.ToString() : "" },
                { "victim_team", victim != null && victim.IsValid ? victim.Team.ToString() : "" },
                { "blindduration", @event.BlindDuration.ToString() }
            };
            // check attacker for challenge
            CheckChallengeGoal(attacker, "player_has_blinded", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_got_blinded", challengeData);
            return HookResult.Continue;
        }
    }
}
