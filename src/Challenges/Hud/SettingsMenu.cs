using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>
    /// Settings view of the menu (<c>view-settings</c> on the menu root): tracker on/off, position and size.
    /// While it is open the tracker shows the overview as a live preview.
    /// </summary>
    public static class SettingsMenu
    {
        public const string PanelId = "ch-set";
        public const string ViewClass = "view-settings";
        private const string TrackerOffClass = "tracker-off";
        private const string ButtonPrefix = "ch-set-";

        private sealed record PositionButton(string ButtonId, TrackerPosition Position, string Var, string Key);

        private static readonly PositionButton[] PositionButtons =
        [
            new(HudMenu.BtnSettingsPosTopLeft, TrackerPosition.TopLeft, "set_pos_tl", "hud.settings.position.top_left"),
            new(HudMenu.BtnSettingsPosTopRight, TrackerPosition.TopRight, "set_pos_tr", "hud.settings.position.top_right"),
            new(HudMenu.BtnSettingsPosLeft, TrackerPosition.Left, "set_pos_l", "hud.settings.position.left"),
            new(HudMenu.BtnSettingsPosRight, TrackerPosition.Right, "set_pos_r", "hud.settings.position.right"),
        ];

        private static readonly (string Var, string Key)[] Labels =
        [
            ("set_title", "hud.settings.title"),
            ("set_back", "hud.settings.back"),
            ("set_widget_h", "hud.settings.widget"),
            ("set_show", "hud.settings.show"),
            ("set_pos_h", "hud.settings.position"),
            ("set_size_h", "hud.settings.size"),
            ("set_hint", "hud.settings.hint"),
        ];

        /// <summary>Every dialog variable of the view, cleared by <see cref="WriteDefaults"/>.</summary>
        private static readonly string[] Vars =
        [
            .. Labels.Select(l => l.Var),
            .. PositionButtons.Select(p => p.Var),
            "set_show_state",
            "set_size",
        ];

        public static bool Owns(string buttonId) => buttonId.StartsWith(ButtonPrefix, StringComparison.Ordinal);

        public static void Open(CCSPlayerController player, PlayerState state)
        {
            state.MenuSettings = true;
            Tracker.BeginPreview(player);
        }

        public static void Close(CCSPlayerController player, PlayerState state)
        {
            state.MenuSettings = false;
            Tracker.EndPreview(player);
        }

        /// <summary>Handles a <c>ch-set-*</c> button; false when nothing changed and the menu needs no repaint.</summary>
        public static bool OnButton(CCSPlayerController player, PlayerState state, string buttonId)
        {
            switch (buttonId)
            {
                case HudMenu.BtnSettingsBack:
                    Close(player, state);
                    return true;
                case HudMenu.BtnSettingsClose:
                    HudMenu.Close(player);
                    return false;
                case HudMenu.BtnSettingsShow:
                    HudSettings.ToggleVisible(state);
                    break;
                case HudMenu.BtnSettingsSizeDown:
                case HudMenu.BtnSettingsSizeUp:
                    if (!HudSettings.StepSize(state, buttonId == HudMenu.BtnSettingsSizeUp ? 1 : -1))
                    {
                        return false;
                    }

                    break;
                default:
                    if (!TrySetPosition(state, buttonId))
                    {
                        return false;
                    }

                    break;
            }

            Tracker.ApplySettings(player);
            return true;
        }

        private static bool TrySetPosition(PlayerState state, string buttonId)
        {
            foreach (PositionButton button in PositionButtons)
            {
                if (button.ButtonId == buttonId)
                {
                    return HudSettings.SetPosition(state, button.Position);
                }
            }

            return false;
        }

        public static void Paint(CCSPlayerController player, PlayerState state)
        {
            foreach ((string varName, string key) in Labels)
            {
                CustomHud.SetText(player, Menu.Panel, varName, Context.Text(player, key));
            }

            bool visible = HudSettings.TrackerVisible(state);
            CustomHud.SetText(
                player,
                Menu.Panel,
                "set_show_state",
                Context.Text(player, visible ? "hud.settings.on" : "hud.settings.off"));
            CustomHud.SetHasClass(player, HudMenu.BtnSettingsShow, "is-on", visible);
            CustomHud.SetHasClass(player, PanelId, TrackerOffClass, !visible);

            TrackerPosition position = HudSettings.PositionOf(state);
            foreach (PositionButton button in PositionButtons)
            {
                CustomHud.SetText(player, Menu.Panel, button.Var, Context.Text(player, button.Key));
                CustomHud.SetHasClass(player, button.ButtonId, "active", button.Position == position);
            }

            int size = HudSettings.SizeIndexOf(state);
            CustomHud.SetText(player, Menu.Panel, "set_size", Context.FormatPercent(player, HudSettings.Sizes[size]));
            CustomHud.SetHasClass(player, HudMenu.BtnSettingsSizeDown, "is-disabled", size == 0);
            CustomHud.SetHasClass(player, HudMenu.BtnSettingsSizeUp, "is-disabled", size == HudSettings.Sizes.Length - 1);
        }

        /// <summary>The XML state of the view: widget on, top-right, both size steps enabled.</summary>
        public static void WriteDefaults(CCSPlayerController player)
        {
            foreach (string name in Vars)
            {
                CustomHud.SetText(player, Menu.Panel, name, string.Empty);
            }

            CustomHud.SetHasClass(player, HudMenu.BtnSettingsShow, "is-on", true);
            CustomHud.SetHasClass(player, PanelId, TrackerOffClass, false);
            foreach (PositionButton button in PositionButtons)
            {
                CustomHud.SetHasClass(player, button.ButtonId, "active", button.Position == TrackerPosition.TopRight);
            }

            CustomHud.SetHasClass(player, HudMenu.BtnSettingsSizeDown, "is-disabled", false);
            CustomHud.SetHasClass(player, HudMenu.BtnSettingsSizeUp, "is-disabled", false);
        }
    }
}
