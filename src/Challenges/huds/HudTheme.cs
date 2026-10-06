using CounterStrikeSharp.API.Core;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Applies <c>gui.theme</c> root classes (<c>theme-gold</c>, …) to a HUD card.</summary>
    public static class HudTheme
    {
        private static readonly string[] Names = ["gold", "ct", "t", "green", "red", "purple"];
        private static readonly string[] Classes =
            ["theme-gold", "theme-ct", "theme-t", "theme-green", "theme-red", "theme-purple"];

        public static string Normalize(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                return Names[0];
            }

            ReadOnlySpan<char> value = theme.AsSpan().Trim();
            for (int i = 0; i < Names.Length; i++)
            {
                if (value.Equals(Names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return Names[i];
                }
            }

            return Names[0];
        }

        public static void Apply(CCSPlayerController player, string panelId)
        {
            string active = Normalize(Context.Config.Gui.Theme);
            for (int i = 0; i < Names.Length; i++)
            {
                CustomHud.SetHasClass(player, panelId, Classes[i], Names[i] == active);
            }
        }
    }
}
