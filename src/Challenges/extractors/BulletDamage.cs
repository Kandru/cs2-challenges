using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BulletDamage : IExtractor
    {
        public string EventClassName => "EventBulletDamage";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["bullet_damage_given", "bullet_damage_taken"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBulletDamage)gameEvent;
            var attacker = e.Attacker;
            var victim = e.Victim;
            d["isteamdamage"] = attacker != null && victim != null ? EventData.Bool(attacker.TeamNum == victim.TeamNum) : "false";
            d["isselfdamage"] = EventData.Bool(attacker == victim);
            d["attackerinair"] = EventData.Bool(e.InAir);
            d["distance"] = EventData.Num(e.Distance);
            d["noscope"] = EventData.Bool(e.NoScope);
            d["numpenetrations"] = EventData.Num(e.NumPenetrations);
            EventData.FillPlayer(d, attacker, "attacker");
            EventData.FillPlayer(d, victim, "victim");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBulletDamage)gameEvent;
            yield return (e.Attacker, "bullet_damage_given");
            yield return (e.Victim, "bullet_damage_taken");
        }
    }
}
