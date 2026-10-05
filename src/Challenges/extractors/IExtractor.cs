using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges.Extractors
{
    public enum ExtractorKind
    {
        GameEvent,
        Listener
    }

    public interface IExtractor
    {
        ExtractorKind Kind { get; }
        /// <summary>CSSSharp game event class name or listener nested-type name.</summary>
        string EventClassName { get; }
        /// <summary>Challenge type strings this source can feed.</summary>
        IReadOnlyList<string> ChallengeTypes { get; }
        /// <summary>Fill challengeData from the game event. No-op for listeners.</summary>
        void Fill(GameEvent gameEvent, Dictionary<string, string> challengeData);
        /// <summary>Players to check for each challenge type. Empty for listeners.</summary>
        IEnumerable<(CCSPlayerController? Player, string ChallengeType)> Targets(GameEvent gameEvent);
    }
}
