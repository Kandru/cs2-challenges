using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public sealed class AchievementEarned : IExtractor
    {
        public string EventClassName => "EventAchievementEarned";
        public IReadOnlyList<string> ChallengeTypes { get; } = ["player_achievement_earned"];

        public void Fill(GameEvent gameEvent, Dictionary<string, string> d)
        {
            var e = (EventAchievementEarned)gameEvent;
            d["achievement"] = e.Achievement.ToString();
            EventData.FillPlayer(d, e.Player, "player");
        }

        public IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent)
        {
            var e = (EventAchievementEarned)gameEvent;
            yield return (e.Player, "player_achievement_earned");
        }
    }
}
