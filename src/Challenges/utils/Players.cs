using System.Diagnostics.CodeAnalysis;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges.Utils
{
    public static class Players
    {
        public static bool IsHumanViewer([NotNullWhen(true)] CCSPlayerController? player) =>
            player is { IsValid: true, IsBot: false, IsHLTV: false };

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

        public static bool IsPlayingTeam(CCSPlayerController player) =>
            player.Team is CsTeam.Terrorist or CsTeam.CounterTerrorist;
    }
}
