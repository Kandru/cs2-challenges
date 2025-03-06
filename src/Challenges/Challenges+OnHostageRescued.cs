using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnHostageRescued(EventHostageRescued @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // check all players for challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
                CheckChallengeGoal(player, "hostage_rescued", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "player", player.PlayerName },
                    { "player_isbot", player.IsBot.ToString() },
                    { "player_team", player.Team.ToString() },
                    { "player_is_rescuer", player == entry ? "true" : "false" },
                    { "hostage", @event.Hostage.ToString() },
                    { "rescue_site", @event.Site.ToString() },
                });
            return HookResult.Continue;
        }
    }
}
