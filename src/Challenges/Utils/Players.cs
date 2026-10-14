using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
{
    public static class Players
    {
        public static bool IsHumanViewer([NotNullWhen(true)] CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false };

        /// <summary>Same live slot (CSS wrappers are not reliable for ==).</summary>
        public static bool Same(CCSPlayerController? a, CCSPlayerController? b) =>
            a is { IsValid: true } && b is { IsValid: true } && a.Slot == b.Slot;

        public static IEnumerable<CCSPlayerController> GetHumans()
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (IsHumanViewer(player))
                {
                    yield return player;
                }
            }
        }

        /// <summary>Generated extractors: lists every connected player as a target of <paramref name="type"/>.</summary>
        public static void AddAll(List<(CCSPlayerController? Player, string Type)> buffer, string type)
        {
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                buffer.Add((player, type));
            }
        }

        public static bool IsPlayingTeam(CCSPlayerController player) =>
            player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
    }
}
