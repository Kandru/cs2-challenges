using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class ExitBombzone : IExtractor
    {
        public string EventClassName => "EventExitBombzone";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["exit_bombzone"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventExitBombzone)gameEvent;
            d["hasbomb"] = EventData.Bool(e.Hasbomb);
            d["isplanted"] = EventData.Bool(e.Isplanted);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventExitBombzone)gameEvent;
            yield return (e.Userid, "exit_bombzone");
        }
    }
}
