using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerChangename : IExtractor
    {
        public string EventClassName => "EventPlayerChangename";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_changed_name"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerChangename)gameEvent;
            d["new_name"] = e.Newname;
            d["old_name"] = e.Oldname;
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerChangename)gameEvent;
            yield return (e.Userid, "player_changed_name");
        }
    }
}
