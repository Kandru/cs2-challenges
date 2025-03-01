using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {

        private HookResult OnPlayerJump(EventPlayerJump @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // jump challenge ahead
            CheckChallengeGoal(player, "player_jump", new Dictionary<string, string> {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() }
            });
            return HookResult.Continue;
        }
    }
}
