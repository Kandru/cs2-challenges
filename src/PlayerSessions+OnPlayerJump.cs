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
                { "isduringround", _isDuringRound.ToString() }
            });
            return HookResult.Continue;
        }
    }
}
