using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerFalldamage(EventPlayerFalldamage @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // check avenger for challenge
            CheckChallengeGoal(player, "player_falldamage", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() },
                { "damage", @event.Damage.ToString() }
            });
            return HookResult.Continue;
        }
    }
}
