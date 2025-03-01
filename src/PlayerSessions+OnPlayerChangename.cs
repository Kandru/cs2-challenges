using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerChangename(EventPlayerChangename @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || player == null
                || !player.IsValid) return HookResult.Continue;
            // check avenger for challenge
            CheckChallengeGoal(player, "player_changed_name", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() },
                { "new_name", @event.Newname },
                { "old_name", @event.Oldname }
            });
            return HookResult.Continue;
        }
    }
}
