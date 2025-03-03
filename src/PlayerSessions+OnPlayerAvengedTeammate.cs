using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerAvengedTeammate(EventPlayerAvengedTeammate @event, GameEventInfo info)
        {
            CCSPlayerController? avenger = @event.AvengerId;
            CCSPlayerController? victim = @event.AvengedPlayerId;
            if (avenger == null
                || !avenger.IsValid
                || !_playerConfigs.ContainsKey(avenger.NetworkIDString)
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isselfavenged", (avenger == victim).ToString() },
                { "avenger", avenger.PlayerName },
                { "avenger_isbot", avenger.IsBot.ToString() },
                { "avenger_team", avenger.Team.ToString() },
                { "victim", victim.PlayerName },
                { "victim_isbot", victim.IsBot.ToString() },
                { "victim_team", victim.Team.ToString() }
            };
            // check avenger for challenge
            CheckChallengeGoal(avenger, "player_has_avenged_teammate", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_got_avenged_teammate", challengeData);
            return HookResult.Continue;
        }
    }
}
