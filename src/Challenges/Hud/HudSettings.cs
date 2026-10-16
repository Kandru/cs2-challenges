using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public enum TrackerPosition
    {
        TopRight,
        Right,
        TopLeft,
        Left,
    }

    /// <summary>
    /// Player tracker preferences from the menu settings (visible, position, size) on top of the
    /// <c>gui.tracker_*</c> defaults. Position and size are class groups on the tracker root
    /// (<c>pos-*</c>, <c>size-*</c>); top-right at 100% is the sheet default and carries no class.
    /// </summary>
    public static class HudSettings
    {
        /// <summary>Config and player-file names, indexed by <see cref="TrackerPosition"/>.</summary>
        private static readonly string[] PositionNames = ["top_right", "right", "top_left", "left"];

        /// <summary>Tracker root class per <see cref="TrackerPosition"/>; null = sheet default.</summary>
        private static readonly string?[] PositionClasses = [null, "pos-r", "pos-tl", "pos-l"];

        /// <summary>Selectable sizes in percent. Keep in sync with <c>tools/hud_scales.py</c> (validated by <c>make panorama</c>).</summary>
        public static readonly int[] Sizes = [80, 90, 100, 110, 120];

        public const int DefaultSize = 100;

        /// <summary>Tracker root class per <see cref="Sizes"/> entry; null = sheet default.</summary>
        private static readonly string?[] SizeClasses = [.. Sizes.Select(s => s == DefaultSize ? null : $"size-{s}")];

        public static bool TrackerVisible(PlayerState state) => state.Hud.TrackerVisible ?? true;

        public static TrackerPosition PositionOf(PlayerState state) =>
            ParsePosition(state.Hud.TrackerPosition)
            ?? ParsePosition(Context.Config.Gui.TrackerPosition)
            ?? TrackerPosition.TopRight;

        /// <summary>Index into <see cref="Sizes"/>: the player's size, else <c>gui.tracker_size</c>, snapped to a step.</summary>
        public static int SizeIndexOf(PlayerState state) =>
            NearestSize(state.Hud.TrackerSize ?? Context.Config.Gui.TrackerSize);

        /// <summary>Writes the position and size classes on the tracker root; unchanged classes send nothing.</summary>
        public static void ApplyTracker(CCSPlayerController player, PlayerState state)
        {
            WriteGroup(player, PositionClasses, (int)PositionOf(state));
            WriteGroup(player, SizeClasses, SizeIndexOf(state));
        }

        /// <summary>The XML state (top-right, 100%): no position or size class.</summary>
        public static void WriteDefaults(CCSPlayerController player)
        {
            WriteGroup(player, PositionClasses, -1);
            WriteGroup(player, SizeClasses, -1);
        }

        public static void ToggleVisible(PlayerState state)
        {
            state.Hud.TrackerVisible = !TrackerVisible(state);
            state.Dirty = true;
        }

        /// <summary>False when the player already chose <paramref name="position"/>.</summary>
        public static bool SetPosition(PlayerState state, TrackerPosition position)
        {
            string name = PositionNames[(int)position];
            if (name == state.Hud.TrackerPosition)
            {
                return false;
            }

            state.Hud.TrackerPosition = name;
            state.Dirty = true;
            return true;
        }

        /// <summary>One step along <see cref="Sizes"/>; false at either end.</summary>
        public static bool StepSize(PlayerState state, int delta)
        {
            int index = SizeIndexOf(state) + delta;
            if ((uint)index >= (uint)Sizes.Length)
            {
                return false;
            }

            state.Hud.TrackerSize = Sizes[index];
            state.Dirty = true;
            return true;
        }

        private static void WriteGroup(CCSPlayerController player, string?[] classes, int active)
        {
            for (int i = 0; i < classes.Length; i++)
            {
                if (classes[i] is { } name)
                {
                    CustomHud.SetHasClass(player, Tracker.Panel, name, i == active);
                }
            }
        }

        private static TrackerPosition? ParsePosition(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            ReadOnlySpan<char> name = value.AsSpan().Trim();
            for (int i = 0; i < PositionNames.Length; i++)
            {
                if (name.Equals(PositionNames[i], StringComparison.OrdinalIgnoreCase))
                {
                    return (TrackerPosition)i;
                }
            }

            return null;
        }

        private static int NearestSize(int percent)
        {
            int best = 0;
            for (int i = 1; i < Sizes.Length; i++)
            {
                if (Math.Abs(Sizes[i] - percent) < Math.Abs(Sizes[best] - percent))
                {
                    best = i;
                }
            }

            return best;
        }
    }
}
