using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return HookResult.Continue;
            // check avenger for challenge
            CheckChallengeGoal(player, "player_team", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "player", player.PlayerName },
                { "player_isbot", player.IsBot.ToString() },
                { "player_team", player.Team.ToString() },
                { "disconnect", @event.Disconnect.ToString() },
                { "silent", @event.Silent.ToString() },
                { "old_team", Enum.GetName(typeof(CsTeam), @event.Oldteam) ?? "Unknown" },
                { "new_team", Enum.GetName(typeof(CsTeam), @event.Team) ?? "Unknown" }
            });
            return HookResult.Continue;
        }
    }
}
