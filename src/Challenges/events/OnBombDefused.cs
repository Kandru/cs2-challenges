using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBombDefused(EventBombDefused @event, GameEventInfo info)
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
            foreach (var item in GetCCSPlayerControllerProperties(player, "defuser")) challengeData[item.Key] = item.Value;
            // check challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                // add player data
                foreach (var item in GetCCSPlayerControllerProperties(entry, "player")) challengeData[item.Key] = item.Value;
                challengeData["player_is_defuser"] = player == entry ? "true" : "false";
                CheckChallengeGoal(player, "bomb_defused", challengeData);
            }
            return HookResult.Continue;
        }
    }
}
