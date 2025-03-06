using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnHostageRescuedAll(EventHostageRescuedAll @event, GameEventInfo info)
        {
            // check all players for challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
                CheckChallengeGoal(entry, "hostage_rescued_all", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "player", entry.PlayerName },
                    { "player_isbot", entry.IsBot.ToString() },
                    { "player_team", entry.Team.ToString() }
                });
            return HookResult.Continue;
        }
    }
}
