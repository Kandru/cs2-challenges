using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerAvengedTeammate(EventPlayerAvengedTeammate @event, GameEventInfo info)
        {
            CCSPlayerController? avenger = @event.AvengerId;
            CCSPlayerController? victim = @event.AvengedPlayerId;
            if (avenger == null
                || !avenger.IsValid
                || avenger.IsBot
                || !_playerConfigs.ContainsKey(avenger.NetworkIDString)
                || victim == null
                || !victim.IsValid) return HookResult.Continue;
            // check for challenge
            CheckChallengeGoal(avenger, "player_avenged_teammate", new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamflash", (avenger.TeamNum == victim.TeamNum).ToString() },
                { "isselfflash", (avenger == victim).ToString() },
                { "avenger", avenger.PlayerName },
                { "avenger_isbot", avenger.IsBot.ToString() },
                { "avenger_team", avenger.Team.ToString() },
                { "victim", victim.PlayerName },
                { "victim_isbot", victim.IsBot.ToString() },
                { "victim_team", victim.Team.ToString() },
            });
            return HookResult.Continue;
        }
    }
}
