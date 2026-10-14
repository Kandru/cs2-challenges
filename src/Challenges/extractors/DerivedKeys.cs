using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Utils;

namespace Challenges.Extractors
{
    /// <summary>
    /// Derived rule keys that are not 1:1 CSS fields (team/self flags, enum names). Generated extractors call these
    /// with the controllers they already read; key names come from <c>DERIVED_KEYS</c> in <c>tools/generate_events.py</c>.
    /// </summary>
    public static class DerivedKeys
    {
        /// <summary>Whether both controllers are in one team, and whether they are the same player.</summary>
        public static void TeamSelf(
            Dictionary<string, EventValue> d,
            CCSPlayerController? attacker,
            CCSPlayerController? victim,
            string teamKey,
            string selfKey)
        {
            d[teamKey] = EventValue.Of(attacker != null && victim != null && attacker.TeamNum == victim.TeamNum);
            d[selfKey] = EventValue.Of(Players.Same(attacker, victim));
        }

        /// <summary>Whether the two controllers compare equal.</summary>
        public static void Equal(
            Dictionary<string, EventValue> d,
            CCSPlayerController? left,
            CCSPlayerController? right,
            string key) =>
            d[key] = EventValue.Of(left == right);

        /// <summary><see cref="CsTeam"/> names of two team numbers; <c>Unknown</c> for other numbers.</summary>
        public static void TeamNames(Dictionary<string, EventValue> d, int oldTeam, int newTeam, string oldKey, string newKey)
        {
            d[oldKey] = EventValue.Of(Enum.GetName(typeof(CsTeam), oldTeam) ?? "Unknown");
            d[newKey] = EventValue.Of(Enum.GetName(typeof(CsTeam), newTeam) ?? "Unknown");
        }
    }
}
