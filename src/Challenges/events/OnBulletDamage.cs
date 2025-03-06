using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnBulletDamage(EventBulletDamage @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Victim;
            if (attacker != null && !_playerConfigs.ContainsKey(attacker.NetworkIDString)
                || victim != null && !_playerConfigs.ContainsKey(victim.NetworkIDString)) return HookResult.Continue;
            // hide GUI for victim
            if (victim != null && victim.IsValid) HideGui(victim);
            // create challenge data
            Dictionary<string, string> challengeData = new Dictionary<string, string>
            {
                { "isduringround", _isDuringRound.ToString() },
                { "isteamdamage", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfdamage", (attacker == victim).ToString() },
                { "attacker", attacker != null && attacker.IsValid ? attacker.PlayerName : "" },
                { "attacker_isbot", attacker != null && attacker.IsValid ? attacker.IsBot.ToString() : "" },
                { "attacker_team", attacker != null && attacker.IsValid ? attacker.Team.ToString() : "" },
                { "victim", victim != null && victim.IsValid ? victim.PlayerName : "" },
                { "victim_isbot", victim != null && victim.IsValid ? victim.IsBot.ToString() : "" },
                { "victim_team", victim != null && victim.IsValid ? victim.Team.ToString() : "" },
                { "attackerinair", @event.InAir.ToString() },
                { "distance", @event.Distance.ToString() },
                { "noscope", @event.NoScope.ToString() },
                { "numpenetrations", @event.NumPenetrations.ToString() }
            };
            // check attacker for challenge
            CheckChallengeGoal(attacker, "bullet_damage_given", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "bullet_damage_taken", challengeData);
            return HookResult.Continue;
        }
    }
}
