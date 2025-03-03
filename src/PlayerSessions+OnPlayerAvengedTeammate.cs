using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerAvengedTeammate(EventPlayerAvengedTeammate @event, GameEventInfo info)
        {
            CCSPlayerController? avenger = @event.AvengerId;
            CCSPlayerController? victim = @event.AvengedPlayerId;
            if (avenger != null && !_playerConfigs.ContainsKey(avenger.NetworkIDString)
                && victim != null && !_playerConfigs.ContainsKey(victim.NetworkIDString)) return HookResult.Continue;
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isselfavenged", (avenger == victim).ToString() },
                { "avenger", avenger != null && avenger.IsValid ? avenger.PlayerName : "" },
                { "avenger_isbot", avenger != null && avenger.IsValid ? avenger.IsBot.ToString() : "" },
                { "avenger_team", avenger != null && avenger.IsValid ? avenger.Team.ToString() : "" },
                { "victim", victim != null && victim.IsValid ? victim.PlayerName : ""},
                { "victim_isbot", victim != null && victim.IsValid ? victim.IsBot.ToString() : ""},
                { "victim_team", victim != null && victim.IsValid ? victim.Team.ToString() : ""}
            };
            // check avenger for challenge
            CheckChallengeGoal(avenger, "player_has_avenged_teammate", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_got_avenged_teammate", challengeData);
            return HookResult.Continue;
        }
    }
}
