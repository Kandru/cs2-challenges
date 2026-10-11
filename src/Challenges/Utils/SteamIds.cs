using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>Steam id gate for progress credit (not bot / empty / zero).</summary>
    public static class SteamIds
    {
        /// <summary>
        /// True when <paramref name="steamId"/> can own challenge progress.
        /// Rejects null/empty, <c>0</c>, and <c>BOT</c> (case-insensitive).
        /// </summary>
        public static bool IsCreditable(string? steamId) =>
            !string.IsNullOrEmpty(steamId)
            && !steamId.Equals("0", StringComparison.Ordinal)
            && !steamId.Equals("BOT", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Live controller in <paramref name="slot"/> only when it still owns <paramref name="steamId"/>.
        /// </summary>
        public static CCSPlayerController? GetPlayerIfSteam(int slot, string steamId)
        {
            CCSPlayerController? player = Utilities.GetPlayerFromSlot(slot);
            return player is { IsValid: true }
                && string.Equals(player.NetworkIDString, steamId, StringComparison.OrdinalIgnoreCase)
                    ? player
                    : null;
        }
    }
}
