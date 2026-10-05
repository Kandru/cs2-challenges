using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerDecal : IExtractor
    {
        public string EventClassName => "EventPlayerDecal";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_decal"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerDecal)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerDecal)gameEvent;
            yield return (e.Userid, "player_decal");
        }
    }
}
