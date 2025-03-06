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
            // check avenger for challenge
            CheckChallengeGoal(player, "player_bomb_planted", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() },
                { "bomb_site", @event.Site.ToString() }
            });
            return HookResult.Continue;
        }
    }
}
