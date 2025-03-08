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
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "isteamdamage", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfdamage", (attacker == victim).ToString() },
                { "attackerinair", @event.InAir.ToString() },
                { "distance", @event.Distance.ToString() },
                { "noscope", @event.NoScope.ToString() },
                { "numpenetrations", @event.NumPenetrations.ToString() }
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(attacker, "attacker")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(victim, "victim")) challengeData[item.Key] = item.Value;
            // check attacker for challenge
            CheckChallengeGoal(attacker, "bullet_damage_given", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "bullet_damage_taken", challengeData);
            return HookResult.Continue;
        }
    }
}
