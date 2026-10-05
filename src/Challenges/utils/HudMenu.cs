using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Enums;
using Challenges.Huds;
using CssTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Challenges.Utils
{
    public sealed class HudMenuOpenOptions
    {
        public required HudMenuInputMode InputMode { get; init; }
        public required Action<CCSPlayerController, string> OnButton { get; init; }
        public Action<CCSPlayerController>? OnClosed { get; init; }
    }

    public static class HudMenu
    {
        public const string PanelId = Menu.Panel;
        public const string BtnClose = "ph-close";
        public const string BtnPrev = "ph-prev";
        public const string BtnNext = "ph-next";
        public const string BtnScorePrev = "ch-score-prev";
        public const string BtnScoreNext = "ch-score-next";
        public const string BtnScoreSort = "ch-score-sort";
        public const string BtnFilterAll = "ch-filter-all";
        public const string BtnFilterProgress = "ch-filter-progress";
        public const string BtnFilterEnding = "ch-filter-ending";
        public const string BtnFilterStarting = "ch-filter-starting";

        private const float CaptureDelaySeconds = 0.2f;

        private sealed class Session
        {
            public required HudMenuOpenOptions Options;
            public int CaptureToken;
            public CssTimer? CaptureTimer;
        }

        private static readonly Dictionary<int, Session> _sessions = [];

        public static bool IsOpen(CCSPlayerController player) =>
            player is { IsValid: true } && _sessions.ContainsKey(player.Slot);

        public static bool Open(CCSPlayerController player, HudMenuOpenOptions options)
        {
            if (!CustomHud.EnsureSpawned(player))
            {
                return false;
            }

            if (_sessions.TryGetValue(player.Slot, out Session? existing))
            {
                CloseInternal(player, existing, invokeClosed: false);
            }

            Session session = new() { Options = options };
            _sessions[player.Slot] = session;
            CustomHud.HidePanel(player, PanelId);
            CustomHud.ShowPanel(player, PanelId);
            ScheduleCapture(player, session);
            return true;
        }

        public static void Close(CCSPlayerController player)
        {
            if (_sessions.TryGetValue(player.Slot, out Session? session))
            {
                CloseInternal(player, session, invokeClosed: true);
            }
        }

        public static void ReleasePlayer(CCSPlayerController? player)
        {
            if (player is not null && _sessions.TryGetValue(player.Slot, out Session? session))
            {
                CloseInternal(player, session, invokeClosed: false);
            }
        }

        public static void Shutdown()
        {
            foreach (int slot in _sessions.Keys.ToList())
            {
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player
                    && _sessions.TryGetValue(slot, out Session? session))
                {
                    CloseInternal(player, session, invokeClosed: false);
                }
                else
                {
                    _sessions.Remove(slot);
                }
            }
        }

        public static bool OnCustomHudClicked(CCSPlayerController player, CCSCustomHudLayout layout, string buttonId)
        {
            if (!_sessions.TryGetValue(player.Slot, out Session? session)
                || string.IsNullOrEmpty(buttonId)
                || session.Options.InputMode is HudMenuInputMode.Keys
                || !Menu.IsMenuLayout(layout))
            {
                return false;
            }

            try
            {
                session.Options.OnButton(player, buttonId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges][HudMenu] OnButton failed: {ex.Message}");
            }

            return true;
        }

        private static void ScheduleCapture(CCSPlayerController player, Session session)
        {
            if (session.Options.InputMode is not (HudMenuInputMode.Mouse or HudMenuInputMode.All))
            {
                return;
            }

            int token = ++session.CaptureToken;
            session.CaptureTimer = new CssTimer(CaptureDelaySeconds, () =>
            {
                if (!_sessions.TryGetValue(player.Slot, out Session? live)
                    || live.CaptureToken != token
                    || !player.IsValid
                    || !CustomHud.TryGetLayout(player, PanelId, out CCSCustomHudLayout hud))
                {
                    return;
                }

                live.CaptureTimer = null;
                hud.SetInputCaptureEnabled(player, true);
            });
        }

        private static void CloseInternal(CCSPlayerController player, Session session, bool invokeClosed)
        {
            session.CaptureToken++;
            session.CaptureTimer?.Kill();
            session.CaptureTimer = null;
            _sessions.Remove(player.Slot);

            if (player.IsValid && CustomHud.TryGetLayout(player, PanelId, out CCSCustomHudLayout hud))
            {
                hud.SetInputCaptureEnabled(player, false);
            }

            CustomHud.HidePanel(player, PanelId);

            if (invokeClosed && player.IsValid && session.Options.OnClosed is { } onClosed)
            {
                try
                {
                    onClosed(player);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Challenges][HudMenu] OnClosed failed: {ex.Message}");
                }
            }
        }
    }
}
