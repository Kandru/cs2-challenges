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
                CheckChallengeGoal(entry, "hostage_rescued", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "rescuer", player.PlayerName },
                    { "rescuer_isbot", player.IsBot.ToString() },
                    { "rescuer_team", player.Team.ToString() },
                    { "player", entry.PlayerName },
                    { "player_isbot", entry.IsBot.ToString() },
                    { "player_team", entry.Team.ToString() },
                    { "player_is_rescuer", player == entry ? "true" : "false" },
                    { "hostage", @event.Hostage.ToString() },
                    { "rescue_site", @event.Site.ToString() },
                });
            return HookResult.Continue;
        }
    }
}
