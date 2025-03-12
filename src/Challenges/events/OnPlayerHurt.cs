using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            CCSPlayerController? attacker = @event.Attacker;
            CCSPlayerController? victim = @event.Userid;
            if (attacker != null && !_playerConfigs.ContainsKey(attacker.NetworkIDString)
                && victim != null && !_playerConfigs.ContainsKey(victim.NetworkIDString)) return HookResult.Continue;
            // build challenge data
            var challengeData = new Dictionary<string, string>{
                { "isteamdamage", attacker != null && victim != null ? (attacker.TeamNum == victim.TeamNum).ToString() : "false" },
                { "isselfdamage", (attacker == victim).ToString() },
                { "dmghealth", @event.DmgHealth.ToString() },
                { "dmgarmor", @event.DmgArmor.ToString() },
                { "health", @event.Health.ToString() },
                { "armor", @event.Armor.ToString() },
                { "hitgroup", @event.Hitgroup.ToString() },
                { "weapon", @event.Weapon },
            };
            // merge global data
            foreach (var item in GetGlobalEventData()) challengeData[item.Key] = item.Value;
            // add player data
            foreach (var item in GetCCSPlayerControllerProperties(attacker, "attacker")) challengeData[item.Key] = item.Value;
            foreach (var item in GetCCSPlayerControllerProperties(victim, "victim")) challengeData[item.Key] = item.Value;
            // check attacker for challenge
            CheckChallengeGoal(attacker, "player_hurt_attacker", challengeData);
            // check victim for challenge
            CheckChallengeGoal(victim, "player_hurt_victim", challengeData);
            return HookResult.Continue;
        }
    }
}
