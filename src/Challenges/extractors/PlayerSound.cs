using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerSound : IExtractor
    {
        public string EventClassName => "EventPlayerSound";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_sound"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerSound)gameEvent;
            d["duration"] = EventData.Num(e.Duration);
            d["radius"] = EventData.Num(e.Radius);
            d["step"] = EventData.Bool(e.Step);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerSound)gameEvent;
            yield return (e.Userid, "player_sound");
        }
    }
}
