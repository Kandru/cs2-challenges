using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BuymenuClose : IExtractor
    {
        public string EventClassName => "EventBuymenuClose";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["buymenu_close"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBuymenuClose)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBuymenuClose)gameEvent;
            yield return (e.Userid, "buymenu_close");
        }
    }
}
