using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerFalldamage : IExtractor
    {
        public string EventClassName => "EventPlayerFalldamage";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_falldamage"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerFalldamage)gameEvent;
            d["damage"] = EventData.Num(e.Damage);
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerFalldamage)gameEvent;
            yield return (e.Userid, "player_falldamage");
        }
    }
}
