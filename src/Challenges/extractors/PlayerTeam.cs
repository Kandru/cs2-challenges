using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerTeam : IExtractor
    {
        public string EventClassName => "EventPlayerTeam";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_team"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerTeam)gameEvent;
            d["disconnect"] = EventData.Bool(e.Disconnect);
            d["silent"] = EventData.Bool(e.Silent);
            d["old_team"] = Enum.GetName(typeof(CsTeam), e.Oldteam) ?? "Unknown";
            d["new_team"] = Enum.GetName(typeof(CsTeam), e.Team) ?? "Unknown";
            EventData.FillPlayer(d, e.Userid, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerTeam)gameEvent;
            yield return (e.Userid, "player_team");
        }
    }
}
