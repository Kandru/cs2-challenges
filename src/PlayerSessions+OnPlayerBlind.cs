using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker == null
                || !attacker.IsValid
                || attacker.IsBot
                || !_playerConfigs.ContainsKey(attacker.NetworkIDString)
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamflash", (attacker.TeamNum == victim.TeamNum).ToString() },
                { "isselfflash", (attacker == victim).ToString() },
                { "attacker", attacker.PlayerName },
                { "attacker_isbot", attacker.IsBot.ToString() },
                { "attacker_team", attacker.Team.ToString() },
                { "victim", victim.PlayerName },
                { "victim_isbot", victim.IsBot.ToString() },
                { "victim_team", victim.Team.ToString() },
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
