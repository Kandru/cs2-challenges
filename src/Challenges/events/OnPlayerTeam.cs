using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "disconnect", @event.Disconnect.ToString() },
                { "silent", @event.Silent.ToString() },
                { "old_team", Enum.GetName(typeof(CsTeam), @event.Oldteam) ?? "Unknown" },
                { "new_team", Enum.GetName(typeof(CsTeam), @event.Team) ?? "Unknown" }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(player, "player")) challengeData[item.Key] = item.Value;
            // check challenge
            _ = CheckChallengeGoal(player, "player_team", challengeData);
            return HookResult.Continue;
        }
    }
}
