using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class PlayerChat : IExtractor
    {
        public string EventClassName => "EventPlayerChat";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_chat"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventPlayerChat)gameEvent;
            var player = Utilities.GetPlayerFromUserid(e.Userid);
            d["teamonly"] = EventData.Bool(e.Teamonly);
            d["text"] = e.Text;
            EventData.FillPlayer(d, player, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventPlayerChat)gameEvent;
            yield return (Utilities.GetPlayerFromUserid(e.Userid), "player_chat");
        }
    }
}
