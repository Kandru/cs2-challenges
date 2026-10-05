using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BotTakeover : IExtractor
    {
        public string EventClassName => "EventBotTakeover";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_bot_takeover"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBotTakeover)gameEvent;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventBotTakeover)gameEvent;
            yield return (e.Userid, "player_bot_takeover");
        }
    }
}
