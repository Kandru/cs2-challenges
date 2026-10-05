using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class EnterBombzone : IExtractor
    {
        public string EventClassName => "EventEnterBombzone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["enter_bombzone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventEnterBombzone)gameEvent;
            d["hasbomb"] = EventData.Bool(e.Hasbomb);
            d["isplanted"] = EventData.Bool(e.Isplanted);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventEnterBombzone)gameEvent;
            yield return (e.Userid, "enter_bombzone");
        }
    }
}
