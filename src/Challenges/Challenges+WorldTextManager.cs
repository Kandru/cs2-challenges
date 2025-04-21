using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
{
    // code with help by T3Marius
    // https://github.com/T3Marius/CS2ScreenMenuAPI
    internal static class WorldTextManager
    {
        internal static CPointWorldText? Create(
            CCSPlayerController? player,
            string message,
            float size = 35,
            Color? color = null,
            string font = "",
            float shiftX = 0f,
            float shiftY = 0f,
            bool drawBackground = true,
            float backgroundFactor = 1f
        )
        {
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.IsHLTV
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return null;
            CCSPlayerPawn playerPawn = player.PlayerPawn.Value;
            // get viewmodel
            var handle = new CHandle<CCSGOViewModel>((IntPtr)(playerPawn.ViewModelServices!.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel") + 4));
            if (!handle.IsValid)
            {
                CCSGOViewModel viewmodel = Utilities.CreateEntityByName<CCSGOViewModel>("predicted_viewmodel")!;
                viewmodel.DispatchSpawn();
                handle.Raw = viewmodel.EntityHandle.Raw;
                Utilities.SetStateChanged(playerPawn, "CCSPlayerPawnBase", "m_pViewModelServices");
            }
            // create worldText
            CPointWorldText worldText = Utilities.CreateEntityByName<CPointWorldText>("point_worldtext")!;
            worldText.MessageText = message;
            worldText.Enabled = true;
            worldText.FontSize = size;
            worldText.Fullbright = true;
            worldText.Color = color ?? Color.Aquamarine;
            worldText.WorldUnitsPerPx = (0.25f / 1050) * size;
            worldText.FontName = font;
            worldText.JustifyHorizontal = PointWorldTextJustifyHorizontal_t.POINT_WORLD_TEXT_JUSTIFY_HORIZONTAL_LEFT;
            worldText.JustifyVertical = PointWorldTextJustifyVertical_t.POINT_WORLD_TEXT_JUSTIFY_VERTICAL_CENTER;
            worldText.ReorientMode = PointWorldTextReorientMode_t.POINT_WORLD_TEXT_REORIENT_NONE;
            // draw background if applicable
            if (drawBackground)
            {
                // set background height
                float backgroundHeight = 0.01f * backgroundFactor * message.Split('\n').Length;
                if (backgroundHeight == 0) backgroundHeight = 0.05f * backgroundFactor;
                // set background width
                float backgroundWidth = 0.1f * backgroundFactor;
                worldText.DrawBackground = true;
                worldText.BackgroundBorderHeight = backgroundHeight;
                worldText.BackgroundBorderWidth = backgroundWidth;
            }
            QAngle eyeAngles = playerPawn.EyeAngles;
            Vector forward = new(), right = new(), up = new();
            NativeAPI.AngleVectors(eyeAngles.Handle, forward.Handle, right.Handle, up.Handle);
            Vector offset = new();
            offset += forward * 7;
            offset += right * shiftX;
            offset += up * shiftY;
            QAngle angles = new()
            {
                Y = eyeAngles.Y + 270,
                Z = 90 - eyeAngles.X,
                X = 0
            };
            // spawn and parent
            worldText.DispatchSpawn();
            worldText.Teleport(playerPawn.AbsOrigin! + offset + new Vector(0, 0, playerPawn.ViewOffset.Z), angles, null);
            worldText.AcceptInput("SetParent", handle.Value, null, "!activator");
            return worldText;
        }
    }
}