using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnHostageRescuedAll(EventHostageRescuedAll @event, GameEventInfo info)
        {
            // build challenge data
            var challengeData = new Dictionary<string, string>();
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // check all players for challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                // add player data
                foreach (var item in GetCCSPlayerControllerProperties(entry, "player")) challengeData[item.Key] = item.Value;
                // check challenge
                CheckChallengeGoal(entry, "hostage_rescued_all", challengeData);
            }
            return HookResult.Continue;
        }
    }
}
