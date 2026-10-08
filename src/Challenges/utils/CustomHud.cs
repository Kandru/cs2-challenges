using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Huds;

namespace Challenges.Utils
{
    /// <summary>
    /// Shared CCSCustomHudLayout driver for Challenges tracker + menu overlays.
    /// </summary>
    public static class CustomHud
    {
        /// <summary>Never refresh a player more often than this many ticks.</summary>
        private const int MinRefreshGapTicks = 5;

        /// <summary>Full-server refresh budget: 128 players/sec ⇒ 2 per tick at 64 tick.</summary>
        private const int FullServerRefreshPerSecond = 128;

        private static readonly string[] RootPanels =
        [
            Tracker.Panel,
            Menu.Panel,
        ];

        private const string PanoramaVersion = global::Challenges.Challenges.PanoramaVersion;
        private const string LayoutDir = "panorama/layout/custom_game/challenges/";

        private static readonly string[] LayoutResources =
        [
            LayoutDir + "tracker_" + PanoramaVersion + ".vxml_c",
            LayoutDir + "menu_" + PanoramaVersion + ".vxml_c",
        ];

        private static readonly string[] PercentClass =
            [.. Enumerable.Range(0, 101).Select(i => $"p{i}")];

        private static readonly string[] CaptureRoots = [Menu.Panel];
        public const double FadeSeconds = 1.0;
        private static readonly TimeSpan CloseTransmitGrace = TimeSpan.FromSeconds(FadeSeconds);

        private static readonly CCSCustomHudLayout?[] _layouts = new CCSCustomHudLayout?[2];
        private static readonly Dictionary<int, HashSet<string>> _visibleBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, DateTime>> _closingRootsBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, Dictionary<string, bool>>> _classBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, Dictionary<string, string>>> _textBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, int>> _stepBySlot = [];
        private static readonly List<CCSPlayerController> _humanRoster = [];
        private static int _ticksPerSecond = 64;
        private static int _refreshPerTick = 2;
        private static int _refreshGap = MinRefreshGapTicks;
        private static uint _tick;

        public static bool IsPanelVisible(CCSPlayerController player, string panelId) =>
            player is { IsValid: true }
            && _visibleBySlot.TryGetValue(player.Slot, out HashSet<string>? set)
            && set.Contains(panelId);

        public static bool EnsureSpawned(CCSPlayerController player) =>
            Players.IsHumanViewer(player) && EnsureLayouts();

        private static bool EnsureLayouts()
        {
            bool allOk = true;
            for (int i = 0; i < LayoutResources.Length; i++)
            {
                if (!EnsureOne(i))
                {
                    allOk = false;
                }
            }
            return allOk;
        }

        public static bool TryGetLayout(CCSPlayerController player, string rootPanelId, out CCSCustomHudLayout hud)
        {
            hud = null!;
            if (!EnsureSpawned(player))
            {
                return false;
            }

            int index = Array.IndexOf(RootPanels, rootPanelId);
            if (index < 0 || _layouts[index] is not { IsValid: true } layout)
            {
                return false;
            }

            hud = layout;
            return true;
        }

        public static bool IsLayout(CCSCustomHudLayout layout, string rootPanelId)
        {
            int index = Array.IndexOf(RootPanels, rootPanelId);
            return index >= 0
                && _layouts[index] is { IsValid: true } live
                && live.Handle == layout.Handle;
        }

        public static void ShowPanel(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            if (!_visibleBySlot.TryGetValue(player.Slot, out HashSet<string>? set))
            {
                _visibleBySlot[player.Slot] = set = [];
            }

            bool wasVisible = set.Contains(panelId);
            set.Add(panelId);
            ClearClosingRoot(player.Slot, TransmitRoot(panelId));
            SetHasClass(player, panelId, "ph-off", false);
            if (wasVisible)
            {
                SetHasClass(player, panelId, "ph-fading", false);
            }
            else
            {
                ScheduleFadeIn(player, panelId);
            }
        }

        /// <summary>
        /// Start opacity fade-out. When <paramref name="collapseAfter"/> is true, transmit stays for
        /// <see cref="FadeSeconds"/> then the card collapses. When false, the card stays mounted for a content swap.
        /// </summary>
        public static void BeginFadeOut(CCSPlayerController player, string panelId, bool collapseAfter = true)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            SetHasClass(player, panelId, "ph-fading", true);
            if (collapseAfter)
            {
                MarkClosingRoot(player.Slot, TransmitRoot(panelId));
            }
        }

        public static void PulseFadeIn(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            SetHasClass(player, panelId, "ph-off", false);
            ScheduleFadeIn(player, panelId);
        }

        public static void FinishFadeOut(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            SetHasClass(player, panelId, "ph-off", true);
            SetHasClass(player, panelId, "ph-fading", false);
            ForgetVisible(player.Slot, panelId);
        }

        public static void HidePanel(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            BeginFadeOut(player, panelId, collapseAfter: true);
            ForgetVisible(player.Slot, panelId);
        }

        private static void ScheduleFadeIn(CCSPlayerController player, string panelId)
        {
            SetHasClass(player, panelId, "ph-fading", true);
            int slot = player.Slot;
            string id = panelId;
            Server.NextFrame(() =>
            {
                CCSPlayerController? live = Utilities.GetPlayerFromSlot(slot);
                if (live is { IsValid: true } && IsPanelVisible(live, id))
                {
                    SetHasClass(live, id, "ph-fading", false);
                }
            });
        }

        private static void ForgetVisible(int slot, string panelId)
        {
            if (!_visibleBySlot.TryGetValue(slot, out HashSet<string>? set))
            {
                return;
            }

            set.Remove(panelId);
            if (set.Count == 0)
            {
                _visibleBySlot.Remove(slot);
            }
        }

        public static void SetText(CCSPlayerController player, string panelId, string name, string value)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            string text = value ?? string.Empty;
            if (_textBySlot.TryGetValue(player.Slot, out Dictionary<string, Dictionary<string, string>>? byPanel)
                && byPanel.TryGetValue(panelId, out Dictionary<string, string>? vars)
                && vars.TryGetValue(name, out string? prev)
                && prev == text)
            {
                return;
            }

            if (!TryWrite(player, panelId, out CCSCustomHudLayout hud))
            {
                return;
            }

            hud.SetDialogVariableStringForPlayer(player, panelId, name, text);

            if (!_textBySlot.TryGetValue(player.Slot, out byPanel))
            {
                _textBySlot[player.Slot] = byPanel = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            }

            if (!byPanel.TryGetValue(panelId, out vars))
            {
                byPanel[panelId] = vars = new Dictionary<string, string>(StringComparer.Ordinal);
            }

            vars[name] = text;
        }

        public static bool HasClass(CCSPlayerController player, string panelId, string className) =>
            player is { IsValid: true }
            && _classBySlot.TryGetValue(player.Slot, out Dictionary<string, Dictionary<string, bool>>? byPanel)
            && byPanel.TryGetValue(panelId, out Dictionary<string, bool>? classes)
            && classes.TryGetValue(className, out bool enabled)
            && enabled;

        public static bool SetHasClass(CCSPlayerController player, string panelId, string className, bool enabled)
        {
            if (player is not { IsValid: true } || string.IsNullOrEmpty(className))
            {
                return false;
            }

            if (_classBySlot.TryGetValue(player.Slot, out Dictionary<string, Dictionary<string, bool>>? byPanel)
                && byPanel.TryGetValue(panelId, out Dictionary<string, bool>? classes)
                && classes.TryGetValue(className, out bool prev)
                && prev == enabled)
            {
                return true;
            }

            if (!TryWrite(player, panelId, out CCSCustomHudLayout hud))
            {
                return false;
            }

            hud.SetHasClassForPlayer(player, panelId, className, enabled);

            if (!_classBySlot.TryGetValue(player.Slot, out byPanel))
            {
                _classBySlot[player.Slot] = byPanel = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);
            }

            if (!byPanel.TryGetValue(panelId, out classes))
            {
                byPanel[panelId] = classes = new Dictionary<string, bool>(StringComparer.Ordinal);
            }

            classes[className] = enabled;
            return true;
        }

        /// <summary>Challenge fills: nearest 10% on the <c>p0</c>…<c>p100</c> clip ladder.</summary>
        public static void SetStepPercent(CCSPlayerController player, string panelId, int percent) =>
            WritePercent(player, panelId, (int)Math.Clamp(Math.Round(percent / 10.0) * 10, 0, 100), step: 10);

        /// <summary>Timer drain: exact percent on the <c>p0</c>…<c>p100</c> clip ladder.</summary>
        public static void SetPercent(CCSPlayerController player, string panelId, int percent) =>
            WritePercent(player, panelId, Math.Clamp(percent, 0, 100), step: 1);

        private static void WritePercent(CCSPlayerController player, string panelId, int pct, int step)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            if (!_stepBySlot.TryGetValue(player.Slot, out Dictionary<string, int>? byPanel))
            {
                _stepBySlot[player.Slot] = byPanel = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            bool written;
            if (byPanel.TryGetValue(panelId, out int previous))
            {
                if (previous == pct)
                {
                    return;
                }

                previous = Math.Clamp(previous, 0, 100);
                SetHasClass(player, panelId, PercentClass[previous], false);
                written = SetHasClass(player, panelId, PercentClass[pct], true);
            }
            else
            {
                written = true;
                for (int p = 0; p <= 100; p += step)
                {
                    written &= SetHasClass(player, panelId, PercentClass[p], p == pct);
                }
            }

            if (written)
            {
                byPanel[panelId] = pct;
            }
            else
            {
                byPanel.Remove(panelId);
            }
        }

        public static void CacheTickRate()
        {
            float interval = Server.TickInterval;
            _ticksPerSecond = interval > 0f
                ? Math.Max(1, (int)Math.Round(1.0 / interval))
                : 64;
            _refreshPerTick = Math.Max(1, FullServerRefreshPerSecond / _ticksPerSecond);
            _tick = 0;
            _humanRoster.Clear();
            _refreshGap = MinRefreshGapTicks;
        }

        public static void OnTick()
        {
            uint tick = _tick++;

            if (tick % (uint)_ticksPerSecond == 0)
            {
                RebuildHumanRoster();
            }

            int count = _humanRoster.Count;
            if (count == 0)
            {
                return;
            }

            int gap = _refreshGap;
            for (int i = (int)(tick % (uint)gap); i < count; i += gap)
            {
                CCSPlayerController player = _humanRoster[i];
                if (Players.IsHumanViewer(player))
                {
                    Tracker.Refresh(player);
                }
            }
        }

        private static void RebuildHumanRoster()
        {
            _humanRoster.Clear();
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                _humanRoster.Add(player);
            }

            int count = _humanRoster.Count;
            _refreshGap = count == 0
                ? MinRefreshGapTicks
                : Math.Max(MinRefreshGapTicks, (count + _refreshPerTick - 1) / _refreshPerTick);
        }

        public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            ExpireClosingRoots();

            for (int i = 0; i < _layouts.Length; i++)
            {
                if (_layouts[i] is not { IsValid: true } layout)
                {
                    continue;
                }

                int index = (int)layout.Index;
                string root = RootPanels[i];
                foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
                {
                    if (!Players.IsHumanViewer(viewer))
                    {
                        continue;
                    }

                    if (ShouldTransmit(viewer.Slot, root))
                    {
                        info.TransmitEntities.Add(index);
                    }
                    else
                    {
                        info.TransmitEntities.Remove(index);
                    }
                }
            }
        }

        public static void ResetRound()
        {
            HashSet<int> liveSlots = [];
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                liveSlots.Add(player.Slot);
                HudMenu.ReleasePlayer(player);
                RestorePlayerDefaults(player);
            }

            ClearOrphanedSlots(liveSlots);
        }

        public static void ReleasePlayer(CCSPlayerController? player)
        {
            if (player is null || !player.IsValid)
            {
                return;
            }

            HudMenu.ReleasePlayer(player);
            RestorePlayerDefaults(player);
            foreach (string root in CaptureRoots)
            {
                if (TryGetLayout(player, root, out CCSCustomHudLayout hud))
                {
                    hud.SetInputCaptureEnabled(player, false);
                }
            }
        }

        public static void Shutdown()
        {
            HudMenu.Shutdown();
            ClearAllSlotState();
            for (int i = 0; i < _layouts.Length; i++)
            {
                if (_layouts[i] is { IsValid: true } layout)
                {
                    layout.AcceptInput("Kill");
                }
                _layouts[i] = null;
            }
        }

        private static void RestorePlayerDefaults(CCSPlayerController player)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            int slot = player.Slot;
            bool dirty = _classBySlot.ContainsKey(slot)
                || _textBySlot.ContainsKey(slot)
                || _visibleBySlot.ContainsKey(slot);
            if (!dirty)
            {
                return;
            }

            _classBySlot.TryGetValue(slot, out Dictionary<string, Dictionary<string, bool>>? classes);
            _textBySlot.TryGetValue(slot, out Dictionary<string, Dictionary<string, string>>? texts);
            ClearSlotCaches(slot);

            if (classes is not null)
            {
                foreach ((string panelId, Dictionary<string, bool> map) in classes)
                {
                    foreach ((string className, bool enabled) in map)
                    {
                        if (enabled)
                        {
                            SetHasClass(player, panelId, className, false);
                        }
                    }
                }
            }

            if (texts is not null)
            {
                foreach ((string panelId, Dictionary<string, string> vars) in texts)
                {
                    foreach ((string name, string value) in vars)
                    {
                        if (value.Length > 0)
                        {
                            SetText(player, panelId, name, string.Empty);
                        }
                    }
                }
            }

            foreach (string root in RootPanels)
            {
                SetHasClass(player, root, "ph-fading", false);
                SetHasClass(player, root, "ph-off", true);
                MarkClosingRoot(slot, root);
            }

            Tracker.WriteDefaults(player);
            Menu.WriteDefaults(player);
            ClearSlotCaches(slot);
        }

        private static void ClearSlotCaches(int slot)
        {
            _visibleBySlot.Remove(slot);
            _classBySlot.Remove(slot);
            _textBySlot.Remove(slot);
            _stepBySlot.Remove(slot);
        }

        private static void ClearOrphanedSlots(HashSet<int> liveSlots)
        {
            void Purge<T>(Dictionary<int, T> map)
            {
                foreach (int slot in map.Keys.Where(s => !liveSlots.Contains(s)).ToList())
                {
                    map.Remove(slot);
                }
            }

            Purge(_visibleBySlot);
            Purge(_classBySlot);
            Purge(_textBySlot);
            Purge(_stepBySlot);
            Purge(_closingRootsBySlot);
        }

        private static void ClearAllSlotState()
        {
            _visibleBySlot.Clear();
            _closingRootsBySlot.Clear();
            _classBySlot.Clear();
            _textBySlot.Clear();
            _stepBySlot.Clear();
            _humanRoster.Clear();
            _refreshGap = MinRefreshGapTicks;
            _tick = 0;
        }

        private static void ExpireClosingRoots()
        {
            if (_closingRootsBySlot.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            foreach (int slot in _closingRootsBySlot.Keys.ToList())
            {
                Dictionary<string, DateTime> closing = _closingRootsBySlot[slot];
                foreach (string root in closing.Where(kv => now >= kv.Value).Select(kv => kv.Key).ToList())
                {
                    closing.Remove(root);
                    if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                    {
                        SetHasClass(player, root, "ph-off", true);
                        SetHasClass(player, root, "ph-fading", false);
                    }
                }

                if (closing.Count == 0)
                {
                    _closingRootsBySlot.Remove(slot);
                }
            }
        }

        private static bool EnsureOne(int index)
        {
            string resource = LayoutResources[index];
            if (_layouts[index] is { IsValid: true } existing && LayoutPath(existing) == resource)
            {
                return true;
            }

            if (_layouts[index] is { IsValid: true } stale)
            {
                stale.AcceptInput("Kill");
                _layouts[index] = null;
            }

            foreach (CCSCustomHudLayout found in
                     Utilities.FindAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout"))
            {
                if (found is not { IsValid: true })
                {
                    continue;
                }

                string? path = LayoutPath(found);
                if (path == resource)
                {
                    _layouts[index] = found;
                    return true;
                }
            }

            return Create(index);
        }

        private static bool Create(int index)
        {
            string resource = LayoutResources[index];
            try
            {
                CCSCustomHudLayout? hud = Utilities.CreateEntityByName<CCSCustomHudLayout>("custom_hud_layout");
                if (hud is null || hud.Handle == IntPtr.Zero)
                {
                    Console.WriteLine($"[Challenges][CustomHud] CreateEntityByName failed for {resource}");
                    return false;
                }

                using (CEntityKeyValues kv = new())
                {
                    kv.SetVector("origin", 0f, 0f, 0f);
                    kv.SetString("layout", resource);
                    kv.SetBool("observable", false);
                    hud.DispatchSpawn(kv);
                }

                if (hud is not { IsValid: true })
                {
                    Console.WriteLine($"[Challenges][CustomHud] DispatchSpawn left entity invalid for {resource}");
                    return false;
                }

                _layouts[index] = hud;
                Console.WriteLine($"[Challenges][CustomHud] Spawned #{hud.Index} → {resource}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Challenges][CustomHud] Spawn failed for {resource}: {ex.Message}");
                return false;
            }
        }

        private static bool TryWrite(CCSPlayerController player, string panelId, out CCSCustomHudLayout hud)
        {
            hud = null!;
            int index = LayoutIndexForPanel(panelId);
            if (index < 0)
            {
                return false;
            }

            if (!EnsureSpawned(player) || _layouts[index] is not { IsValid: true } layout)
            {
                return false;
            }

            hud = layout;
            try
            {
                return player.Slot >= 0 && player.Slot < hud.PlayerLayoutStates.Count;
            }
            catch
            {
                return false;
            }
        }

        private static int LayoutIndexForPanel(string panelId)
        {
            if (panelId == Tracker.Panel
                || panelId == Tracker.HintId
                || panelId.StartsWith("ch-trow-", StringComparison.Ordinal)
                || panelId.StartsWith("ch-tfill-", StringComparison.Ordinal)
                || panelId.StartsWith("ch-ttask-", StringComparison.Ordinal)
                || panelId.StartsWith("ch-ttimer", StringComparison.Ordinal))
            {
                return 0;
            }

            if (panelId == Menu.Panel
                || panelId.StartsWith("ch-", StringComparison.Ordinal)
                || panelId.StartsWith("ph-", StringComparison.Ordinal))
            {
                return 1;
            }

            return -1;
        }

        private static bool ShouldTransmit(int slot, string rootPanelId)
        {
            if (_visibleBySlot.TryGetValue(slot, out HashSet<string>? set) && set.Contains(rootPanelId))
            {
                return true;
            }

            if (_closingRootsBySlot.TryGetValue(slot, out Dictionary<string, DateTime>? closing)
                && closing.TryGetValue(rootPanelId, out DateTime until))
            {
                return DateTime.UtcNow < until;
            }

            return false;
        }

        private static string TransmitRoot(string panelId)
        {
            int index = LayoutIndexForPanel(panelId);
            return index >= 0 ? RootPanels[index] : panelId;
        }

        private static void MarkClosingRoot(int slot, string rootPanelId)
        {
            if (!_closingRootsBySlot.TryGetValue(slot, out Dictionary<string, DateTime>? closing))
            {
                _closingRootsBySlot[slot] = closing = new Dictionary<string, DateTime>(StringComparer.Ordinal);
            }

            closing[rootPanelId] = DateTime.UtcNow + CloseTransmitGrace;
        }

        private static void ClearClosingRoot(int slot, string rootPanelId)
        {
            if (!_closingRootsBySlot.TryGetValue(slot, out Dictionary<string, DateTime>? closing))
            {
                return;
            }

            closing.Remove(rootPanelId);
            if (closing.Count == 0)
            {
                _closingRootsBySlot.Remove(slot);
            }
        }

        private static string? LayoutPath(CCSCustomHudLayout layout)
        {
            try
            {
                return layout.StrLayout;
            }
            catch
            {
                return null;
            }
        }
    }
}
