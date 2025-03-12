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
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "isteamflash", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfflash", (attacker == victim).ToString() },
                { "blindduration", @event.BlindDuration.ToString() }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(attacker, "attacker")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(victim, "victim")) challengeData[item.Key] = item.Value;
            // check attacker for challenge
            _ = CheckChallengeGoal(attacker, "player_has_blinded", challengeData);
            // check victim for challenge
            _ = CheckChallengeGoal(victim, "player_got_blinded", challengeData);
            return HookResult.Continue;
        }
    }
}
