using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class BombBeep : IExtractor
    {
        public string EventClassName => "EventBombBeep";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["bomb_beep"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventBombBeep)gameEvent;
            d["entindex"] = EventData.Num(e.Entindex);
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            foreach (var entry in Utilities.GetPlayers())
            {
                yield return (entry, "bomb_beep");
            }
        }
    }
}
