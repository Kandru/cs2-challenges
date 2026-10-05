using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges.Extractors
{
    public interface IExtractor
    {
        /// <summary>CSSSharp game event class name, e.g. EventPlayerDeath.</summary>
        string EventClassName { get; }
        /// <summary>Challenge type strings this event can feed, e.g. player_kill.</summary>
        IReadOnlyList<string> ChallengeTypes { get; }
        /// <summary>Fill challengeData from the game event (keys lowercased as in old code). Do not add global.* keys.</summary>
        void Fill(GameEvent gameEvent, Dictionary<string, string> challengeData);
        /// <summary>
        /// Returns which players should be checked for which challenge types.
        /// Each tuple: (player, challengeType). Skip null/invalid players.
        /// </summary>
        IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent);
    }
}
