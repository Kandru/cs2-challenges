using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBombBeep(EventBombBeep @event, GameEventInfo info)
        {
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "entindex", @event.Entindex.ToString() }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // check challenge
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                // add player data
                foreach (var item in GetCCSPlayerControllerProperties(entry, "player")) challengeData[item.Key] = item.Value;
                CheckChallengeGoal(entry, "bomb_beep", challengeData);
            }
            return HookResult.Continue;
        }
    }
}
