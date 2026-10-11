using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>Applies <c>gui.theme</c> root classes (<c>theme-*</c>) to a HUD card.</summary>
    public static class HudTheme
    {
        /// <summary>Keep in sync with <c>tools/hud_themes.py</c> (validated by <c>make panorama</c>). Rainbow order.</summary>
        public readonly record struct Entry(string Name, int DiscordColor);

        public static readonly Entry[] All =
        [
            new("red", 0xe05a5a),
            new("coral", 0xff7a59),
            new("orange", 0xff8a3d),
            new("copper", 0xe08a5a),
            new("gold", 0xf0a531),
            new("t", 0xeabe54),
            new("lime", 0xc6f04a),
            new("green", 0x6fdc78),
            new("teal", 0x2ec9a6),
            new("cyan", 0x3ee0f0),
            new("blue", 0x5b8cff),
            new("ct", 0x96c8fa),
            new("purple", 0xb07cff),
            new("magenta", 0xe85dff),
            new("pink", 0xff6b9a),
            new("silver", 0xc9d2dc),
        ];

        public static string Normalize(string? theme)
        {
            if (string.IsNullOrWhiteSpace(theme))
            {
                return "gold";
            }

            ReadOnlySpan<char> value = theme.AsSpan().Trim();
            for (int i = 0; i < All.Length; i++)
            {
                if (value.Equals(All[i].Name, StringComparison.OrdinalIgnoreCase))
                {
                    return All[i].Name;
                }
            }

            return "gold";
        }

        public static void Apply(CCSPlayerController player, string panelId)
        {
            string active = Normalize(Context.Config.Gui.Theme);
            for (int i = 0; i < All.Length; i++)
            {
                string name = All[i].Name;
                CustomHud.SetHasClass(player, panelId, "theme-" + name, name == active);
            }
        }

        public static int DiscordColor(string theme)
        {
            string name = Normalize(theme);
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Name == name)
                {
                    return All[i].DiscordColor;
                }
            }

            return 0xf0a531;
        }
    }
}
