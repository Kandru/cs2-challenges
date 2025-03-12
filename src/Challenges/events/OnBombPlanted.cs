using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBombPlanted(EventBombPlanted @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "bomb_site", @event.Site.ToString() }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(player, "planter")) challengeData[item.Key] = item.Value;
            // check challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                // add player data
                foreach (var item in GetCCSPlayerControllerProperties(entry, "player")) challengeData[item.Key] = item.Value;
                challengeData["player_is_planter"] = player == entry ? "true" : "false";
                // check challenge
                CheckChallengeGoal(entry, "player_bomb_planted", challengeData);
            }
            return HookResult.Continue;
        }
    }
}
