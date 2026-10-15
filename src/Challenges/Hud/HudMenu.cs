using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Timers;
using CssTimer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace Challenges
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
        public const string BtnScorePrev = "ch-score-prev";
        public const string BtnScoreNext = "ch-score-next";
        public const string BtnListPrev = "ch-list-prev";
        public const string BtnListNext = "ch-list-next";
        public const string BtnFilterAll = "ch-filter-all";
        public const string BtnFilterSolved = "ch-filter-solved";
        public const string BtnFilterProgress = "ch-filter-progress";
        public const string BtnFilterEnding = "ch-filter-ending";
        public const string BtnFilterStarting = "ch-filter-starting";
        public const string BtnScoreFilterAll = "ch-score-f-all";
        public const string BtnScoreFilterOnline = "ch-score-f-online";
        public const string BtnScoreSortSolved = "ch-score-s-solved";
        public const string BtnScoreSortLifetime = "ch-score-s-lifetime";

        private const float CaptureDelaySeconds = 0.2f;

        private sealed class Session
        {
            public required HudMenuOpenOptions Options;
            public int CaptureToken;
        }

        private static readonly Dictionary<int, Session> _sessions = [];

        /// <summary>Delay between a chat command and the open: spawn and first paint stay out of the command's tick.</summary>
        private const float OpenDelaySeconds = 0.2f;

        /// <summary>Per slot: token of the pending open; a newer request, a close or a release bumps it.</summary>
        private static readonly int[] _openToken = new int[CustomHud.MaxSlots];
        private static readonly bool[] _openPending = new bool[CustomHud.MaxSlots];

        public static bool IsOpen(CCSPlayerController player) =>
            player is { IsValid: true } && _sessions.ContainsKey(player.Slot);

        public static bool IsOpenPending(CCSPlayerController player) =>
            player is { IsValid: true } && (uint)player.Slot < CustomHud.MaxSlots && _openPending[player.Slot];

        /// <summary>Runs <paramref name="open"/> one timer after a chat command, unless cancelled in between.</summary>
        public static void RequestOpen(CCSPlayerController player, Action<CCSPlayerController> open)
        {
            int slot = player.Slot;
            if ((uint)slot >= CustomHud.MaxSlots)
            {
                return;
            }

            int token = ++_openToken[slot];
            _openPending[slot] = true;
            _ = new CssTimer(OpenDelaySeconds, () =>
            {
                if (_openToken[slot] != token)
                {
                    return;
                }

                _openPending[slot] = false;
                if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } live)
                {
                    open(live);
                }
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private static void CancelOpen(int slot)
        {
            if ((uint)slot < CustomHud.MaxSlots)
            {
                _openToken[slot]++;
                _openPending[slot] = false;
            }
        }

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
            CancelOpen(player.Slot);
            if (_sessions.TryGetValue(player.Slot, out Session? session))
            {
                CloseInternal(player, session, invokeClosed: true);
            }
        }

        public static void ReleasePlayer(CCSPlayerController? player)
        {
            if (player is null)
            {
                return;
            }

            CancelOpen(player.Slot);
            if (_sessions.TryGetValue(player.Slot, out Session? session))
            {
                CloseInternal(player, session, invokeClosed: false);
            }
        }

        public static void Shutdown()
        {
            for (int slot = 0; slot < CustomHud.MaxSlots; slot++)
            {
                CancelOpen(slot);
            }

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

            // Cancelled by the token (and the session identity), never by Kill(): killing a one-shot timer
            // from another one-shot timer's callback corrupts the native timer list.
            int token = ++session.CaptureToken;
            _ = new CssTimer(CaptureDelaySeconds, () =>
            {
                if (!_sessions.TryGetValue(player.Slot, out Session? live)
                    || live != session
                    || live.CaptureToken != token
                    || !player.IsValid
                    || !CustomHud.TryGetLayout(player, PanelId, out CCSCustomHudLayout hud))
                {
                    return;
                }

                hud.SetInputCaptureEnabled(player, true);
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        private static void CloseInternal(CCSPlayerController player, Session session, bool invokeClosed)
        {
            session.CaptureToken++;
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
