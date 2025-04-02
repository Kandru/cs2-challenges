using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerChat(EventPlayerChat @event, GameEventInfo info)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromUserid(@event.Userid);
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "teamonly", @event.Teamonly.ToString() },
                { "text", @event.Text }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(player, "player")) challengeData[item.Key] = item.Value;
            // check challenge
            CheckChallengeGoal(player, "player_chat", challengeData);
            return HookResult.Continue;
        }
    }
}
