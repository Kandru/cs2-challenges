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
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "isselfavenged", (avenger == victim).ToString() },
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(avenger, "avenger")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(victim, "victim")) challengeData[item.Key] = item.Value;
            // check avenger for challenge
            CheckChallengeGoal(avenger, "player_has_avenged_teammate", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_got_avenged_teammate", challengeData);
            return HookResult.Continue;
        }
    }
}
