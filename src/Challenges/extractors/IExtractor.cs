using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Extractors
{
    public enum ExtractorKind
    {
        GameEvent,
        Listener
    }

    /// <summary>
    /// Key layout of one data source (extractor or synthetic type). Sources write canonical keys only;
    /// rule keys spelled with an alias are resolved to them at load by <see cref="RuleKeys.Canonical"/>.
    /// </summary>
    public interface IKeySource
    {
        /// <summary>Canonical event keys the source can write; indexes <see cref="FillDemand.Keys"/>.</summary>
        IReadOnlyList<string> Keys { get; }
        /// <summary>Canonical player key prefixes, one per controller; indexes <see cref="FillDemand.Prefixes"/>.</summary>
        IReadOnlyList<string> PlayerPrefixes { get; }
        /// <summary>Alias key to canonical key (e.g. <c>dmgarmor</c> to <c>dmg_armor</c>).</summary>
        IReadOnlyDictionary<string, string> KeyAliases { get; }
        /// <summary>
        /// Alias prefix to the canonical prefix of the same controller (e.g. <c>victim</c> to <c>userid</c>);
        /// applies to every suffix after the prefix.
        /// </summary>
        IReadOnlyDictionary<string, string> PrefixAliases { get; }
    }

    public interface IExtractor : IKeySource
    {
        ExtractorKind Kind { get; }
        /// <summary>CSSSharp game event class name or listener nested-type name.</summary>
        string EventClassName { get; }
        /// <summary>Challenge type strings this source can feed.</summary>
        IReadOnlyList<string> ChallengeTypes { get; }
        /// <summary>Keys written by <see cref="Extractors.DerivedKeys.Apply"/>; any of them sets <see cref="FillDemand.Derived"/>.</summary>
        IReadOnlyList<string> DerivedKeys { get; }
        /// <summary>Writes the keys <paramref name="need"/> asks for. No-op for listeners.</summary>
        void Fill(GameEvent gameEvent, Dictionary<string, EventValue> challengeData, FillDemand need);
        /// <summary>Appends the players to check for each type <paramref name="need"/> marks active. No-op for listeners.</summary>
        void Targets(
            GameEvent gameEvent,
            List<(CCSPlayerController? Player, string ChallengeType)> buffer,
            FillDemand need);
        /// <summary>
        /// Controller behind <see cref="IKeySource.PlayerPrefixes"/>[<paramref name="prefixIndex"/>], for attribute
        /// providers. Always null for listeners; their generated handler passes the controller itself.
        /// </summary>
        CCSPlayerController? GetPrefixPlayer(GameEvent gameEvent, int prefixIndex);
    }
}
