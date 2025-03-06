using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnAddPlayerSonarIcon(EventAddPlayerSonarIcon @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // check avenger for challenge
            CheckChallengeGoal(player, "player_add_sonar_icon", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() }
            });
            return HookResult.Continue;
        }
    }
}
