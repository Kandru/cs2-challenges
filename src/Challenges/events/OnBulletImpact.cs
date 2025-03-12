using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBulletImpact(EventBulletImpact @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player != null && !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>();
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(player, "player")) challengeData[item.Key] = item.Value;
            // check attacker for challenge
            CheckChallengeGoal(player, "bullet_impact", challengeData);
            return HookResult.Continue;
        }
    }
}
