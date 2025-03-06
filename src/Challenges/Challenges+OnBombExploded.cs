using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBombExploded(EventBombExploded @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // check all players for challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
                CheckChallengeGoal(player, "bomb_exploded", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "exploder", player.PlayerName },
                    { "expoder_isbot", player.IsBot.ToString() },
                    { "exploder_team", player.Team.ToString() },
                    { "player", entry.PlayerName },
                    { "player_isbot", entry.IsBot.ToString() },
                    { "player_team", entry.Team.ToString() },
                    { "player_is_exploder", player == entry ? "true" : "false" },
                    { "bomb_site", @event.Site.ToString() }
                });
            return HookResult.Continue;
        }
    }
}
