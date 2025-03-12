using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnWeaponZoom(EventWeaponZoom @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>();
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(player, "player")) challengeData[item.Key] = item.Value;
            // check challenge
            _ = CheckChallengeGoal(player, "weapon_zoom", challengeData);
            return HookResult.Continue;
        }
    }
}
