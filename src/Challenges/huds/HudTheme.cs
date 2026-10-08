using CounterStrikeSharp.API.Core;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Applies <c>gui.theme</c> root classes (<c>theme-*</c>) to a HUD card.</summary>
    public static class HudTheme
    {
        // Keep in sync with tools/hud_themes.py (validated by make panorama). Rainbow order.
        private static readonly string[] Names =
        [
            "red", "coral", "orange", "copper", "gold", "t", "lime", "green",
            "teal", "cyan", "blue", "ct", "purple", "magenta", "pink", "silver",
        ];

        public static string Normalize(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                return "gold";
            }

            ReadOnlySpan<char> value = theme.AsSpan().Trim();
            for (int i = 0; i < Names.Length; i++)
            {
                if (value.Equals(Names[i], StringComparison.OrdinalIgnoreCase))
                {
                    return Names[i];
                }
            }

            return "gold";
        }

        public static void Apply(CCSPlayerController player, string panelId)
        {
            string active = Normalize(Context.Config.Gui.Theme);
            for (int i = 0; i < Names.Length; i++)
            {
                CustomHud.SetHasClass(player, panelId, "theme-" + Names[i], Names[i] == active);
            }
        }
    }
}
