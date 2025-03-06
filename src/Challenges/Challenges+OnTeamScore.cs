using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnTeamScore(EventTeamScore @event, GameEventInfo info)
        {
            // check all players for challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
                CheckChallengeGoal(entry, "team_score", new Dictionary<string, string>
                {
                    { "isduringround", _isDuringRound.ToString() },
                    { "player", entry.PlayerName },
                    { "player_isbot", entry.IsBot.ToString() },
                    { "player_team", entry.Team.ToString() },
                    { "score", entry.Score.ToString() }
                });
            return HookResult.Continue;
        }
    }
}
