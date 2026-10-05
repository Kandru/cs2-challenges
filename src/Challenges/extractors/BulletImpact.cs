using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BulletImpact : IExtractor
    {
        public string EventClassName => "EventBulletImpact";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["bullet_impact"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBulletImpact)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBulletImpact)gameEvent;
            yield return (e.Userid, "bullet_impact");
        }
    }
}
