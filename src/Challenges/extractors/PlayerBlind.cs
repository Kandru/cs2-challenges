using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerBlind : IExtractor
    {
        public string EventClassName => "EventPlayerBlind";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_has_blinded", "player_got_blinded"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerBlind)gameEvent;
            var attacker = e.Attacker;
            var victim = e.Userid;
            d["isteamflash"] = attacker != null && victim != null ? EventData.Bool(attacker.TeamNum == victim.TeamNum) : "false";
            d["isselfflash"] = EventData.Bool(attacker == victim);
            d["blindduration"] = EventData.Num(e.BlindDuration);
            EventData.FillPlayer(d, attacker, "attacker");
            EventData.FillPlayer(d, victim, "victim");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerBlind)gameEvent;
            yield return (e.Attacker, "player_has_blinded");
            yield return (e.Userid, "player_got_blinded");
        }
    }
}
