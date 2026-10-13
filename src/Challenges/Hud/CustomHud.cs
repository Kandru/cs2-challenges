using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
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
        private const long CloseTransmitGraceMs = (long)(FadeSeconds * 1000);

        /// <summary>Player slots covered by the per-slot transmit tables.</summary>
        private const int MaxSlots = 65;

        private const byte ViewerUnknown = 0;
        private const byte ViewerHuman = 1;
        private const byte ViewerOther = 2;

        private static readonly CCSCustomHudLayout?[] _layouts = new CCSCustomHudLayout?[2];

        /// <summary>Per layout: the cached entity was matched against its resource while its handle stays valid.</summary>
        private static readonly bool[] _layoutVerified = new bool[2];

        /// <summary>Per layout: entity index of the verified layout, or -1.</summary>
        private static readonly int[] _layoutEntityIndex = [-1, -1];

        /// <summary>Per layout: <c>PlayerLayoutStates.Count</c> at validation, refreshed when a slot falls outside.</summary>
        private static readonly int[] _layoutSlotCount = new int[2];
        private static readonly Dictionary<int, HashSet<string>> _visibleBySlot = [];

        /// <summary>Per root layout and slot: the root panel is shown. Mirrors the root ids in <see cref="_visibleBySlot"/>.</summary>
        private static readonly bool[][] _rootVisible = SlotTable<bool>();

        /// <summary>
        /// Per root layout and slot: <see cref="Environment.TickCount64"/> at which the closing transmit grace ends; 0 = not closing.
        /// </summary>
        private static readonly long[][] _closingUntil = SlotTable<long>();

        /// <summary>Earliest pending <see cref="_closingUntil"/>; <see cref="long.MaxValue"/> when nothing is closing.</summary>
        private static long _nextClosingExpiry = long.MaxValue;

        /// <summary>Per slot: cached human check for transmit viewers. Reset with every roster rebuild and on release.</summary>
        private static readonly byte[] _viewerKind = new byte[MaxSlots];

        /// <summary>Per root layout: entity index transmitted this frame, or -1 while the layout is not live.</summary>
        private static readonly int[] _frameEntityIndex = new int[RootPanels.Length];
        private static readonly Dictionary<int, Dictionary<string, Dictionary<string, bool>>> _classBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, Dictionary<string, string>>> _textBySlot = [];
        private static readonly Dictionary<int, Dictionary<string, int>> _stepBySlot = [];
        private static readonly List<CCSPlayerController> _humanRoster = [];
        private static int _ticksPerSecond = 64;
        private static int _refreshPerTick = 2;
        private static int _refreshGap = MinRefreshGapTicks;
        private static uint _tick;

        /// <summary>Slots in the last roster; a change invalidates the cached scoreboards.</summary>
        private static ulong _rosterMask;

        public static bool IsPanelVisible(CCSPlayerController player, string panelId)
        {
            if (player is not { IsValid: true })
            {
                return false;
            }

            int slot = player.Slot;
            int root = RootIndex(panelId);
            if (root >= 0)
            {
                return (uint)slot < MaxSlots && _rootVisible[root][slot];
            }

            return _visibleBySlot.TryGetValue(slot, out HashSet<string>? set) && set.Contains(panelId);
        }

        private static T[][] SlotTable<T>()
        {
            T[][] table = new T[RootPanels.Length][];
            for (int i = 0; i < table.Length; i++)
            {
                table[i] = new T[MaxSlots];
            }

            return table;
        }

        private static int RootIndex(string panelId)
        {
            for (int i = 0; i < RootPanels.Length; i++)
            {
                if (string.Equals(RootPanels[i], panelId, StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return -1;
        }

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
            SetRootVisible(player.Slot, panelId, true);
            ClearClosingRoot(player.Slot, LayoutIndexForPanel(panelId));
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
                MarkClosingRoot(player.Slot, LayoutIndexForPanel(panelId));
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
            SetRootVisible(slot, panelId, false);
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
            Array.Clear(_viewerKind);
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
                    Menu.PaintIfDirty(player);
                }
            }
        }

        private static void RebuildHumanRoster()
        {
            ulong mask = 0;
            _humanRoster.Clear();
            Array.Clear(_viewerKind);
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                _humanRoster.Add(player);
                int slot = player.Slot;
                if ((uint)slot < MaxSlots)
                {
                    _viewerKind[slot] = ViewerHuman;
                }

                if ((uint)slot < 64)
                {
                    mask |= 1UL << slot;
                }
            }

            if (mask != _rosterMask)
            {
                _rosterMask = mask;
                Menu.InvalidateScoreboard();
            }

            int count = _humanRoster.Count;
            _refreshGap = count == 0
                ? MinRefreshGapTicks
                : Math.Max(MinRefreshGapTicks, (count + _refreshPerTick - 1) / _refreshPerTick);
        }

        /// <summary>
        /// Per frame: one pass over the viewers, array reads only. Each root layout is sent to a human viewer
        /// while its root panel is shown or closing; other viewers are left untouched.
        /// </summary>
        public static void OnCheckTransmit(CCheckTransmitInfoList infoList)
        {
            ExpireClosingRoots();

            bool anyLive = false;
            for (int i = 0; i < _frameEntityIndex.Length; i++)
            {
                int index = _layouts[i] is { IsValid: true } ? _layoutEntityIndex[i] : -1;
                _frameEntityIndex[i] = index;
                anyLive |= index >= 0;
            }

            if (!anyLive)
            {
                return;
            }

            int count = infoList.Count;
            for (int v = 0; v < count; v++)
            {
                (CCheckTransmitInfo info, CCSPlayerController? viewer) = infoList[v];
                if (viewer is null)
                {
                    continue;
                }

                int slot = viewer.Slot;
                if ((uint)slot >= MaxSlots || !IsHumanSlot(viewer, slot))
                {
                    continue;
                }

                for (int i = 0; i < _frameEntityIndex.Length; i++)
                {
                    int index = _frameEntityIndex[i];
                    if (index < 0)
                    {
                        continue;
                    }

                    if (_rootVisible[i][slot] || _closingUntil[i][slot] != 0)
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

        /// <summary>Roster-backed human check; unknown slots resolve once with the native reads.</summary>
        private static bool IsHumanSlot(CCSPlayerController viewer, int slot)
        {
            byte kind = _viewerKind[slot];
            if (kind == ViewerUnknown)
            {
                if (!viewer.IsValid)
                {
                    return false;
                }

                kind = viewer is { IsBot: false, IsHLTV: false } ? ViewerHuman : ViewerOther;
                _viewerKind[slot] = kind;
            }

            return kind == ViewerHuman;
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
            if ((uint)player.Slot < MaxSlots)
            {
                _viewerKind[player.Slot] = ViewerUnknown;
            }

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
            Menu.ResetCaches();
            ClearAllSlotState();
            for (int i = 0; i < _layouts.Length; i++)
            {
                if (_layouts[i] is { IsValid: true } layout)
                {
                    layout.AcceptInput("Kill");
                }
                ForgetLayout(i);
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

            for (int i = 0; i < RootPanels.Length; i++)
            {
                string root = RootPanels[i];
                SetHasClass(player, root, "ph-fading", false);
                SetHasClass(player, root, "ph-off", true);
                MarkClosingRoot(slot, i);
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
            if ((uint)slot < MaxSlots)
            {
                for (int i = 0; i < _rootVisible.Length; i++)
                {
                    _rootVisible[i][slot] = false;
                }
            }
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
            for (int slot = 0; slot < MaxSlots; slot++)
            {
                if (liveSlots.Contains(slot))
                {
                    continue;
                }

                for (int i = 0; i < _rootVisible.Length; i++)
                {
                    _rootVisible[i][slot] = false;
                    _closingUntil[i][slot] = 0;
                }
            }
        }

        private static void ClearAllSlotState()
        {
            _visibleBySlot.Clear();
            _classBySlot.Clear();
            _textBySlot.Clear();
            _stepBySlot.Clear();
            for (int i = 0; i < _rootVisible.Length; i++)
            {
                Array.Clear(_rootVisible[i]);
                Array.Clear(_closingUntil[i]);
            }

            _nextClosingExpiry = long.MaxValue;
            Array.Clear(_viewerKind);
            _humanRoster.Clear();
            _refreshGap = MinRefreshGapTicks;
            _tick = 0;
            _rosterMask = 0;
        }

        /// <summary>One comparison while nothing is due; otherwise an allocation-free sweep of the closing table.</summary>
        private static void ExpireClosingRoots()
        {
            long now = Environment.TickCount64;
            if (now < _nextClosingExpiry)
            {
                return;
            }

            // Writes below may mark new closings; they lower the bound again.
            _nextClosingExpiry = long.MaxValue;
            long next = long.MaxValue;
            for (int i = 0; i < _closingUntil.Length; i++)
            {
                long[] closing = _closingUntil[i];
                for (int slot = 0; slot < MaxSlots; slot++)
                {
                    long until = closing[slot];
                    if (until == 0)
                    {
                        continue;
                    }

                    if (now < until)
                    {
                        next = Math.Min(next, until);
                        continue;
                    }

                    closing[slot] = 0;
                    if (Utilities.GetPlayerFromSlot(slot) is { IsValid: true } player)
                    {
                        SetHasClass(player, RootPanels[i], "ph-off", true);
                        SetHasClass(player, RootPanels[i], "ph-fading", false);
                    }
                }
            }

            _nextClosingExpiry = Math.Min(_nextClosingExpiry, next);
        }

        /// <summary>
        /// Validated once per entity: <c>StrLayout</c> is only read again after the cached handle stops being valid.
        /// </summary>
        private static bool EnsureOne(int index)
        {
            if (_layouts[index] is { IsValid: true } existing)
            {
                if (_layoutVerified[index])
                {
                    return true;
                }

                if (LayoutPath(existing) == LayoutResources[index])
                {
                    Adopt(index, existing);
                    return true;
                }

                existing.AcceptInput("Kill");
            }

            ForgetLayout(index);
            string resource = LayoutResources[index];
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
                    Adopt(index, found);
                    return true;
                }
            }

            return Create(index);
        }

        private static void Adopt(int index, CCSCustomHudLayout layout)
        {
            _layouts[index] = layout;
            _layoutVerified[index] = true;
            _layoutEntityIndex[index] = (int)layout.Index;
            _layoutSlotCount[index] = ReadSlotCount(layout);
        }

        private static void ForgetLayout(int index)
        {
            _layouts[index] = null;
            _layoutVerified[index] = false;
            _layoutEntityIndex[index] = -1;
            _layoutSlotCount[index] = 0;
        }

        private static int ReadSlotCount(CCSCustomHudLayout layout)
        {
            try
            {
                return layout.PlayerLayoutStates.Count;
            }
            catch
            {
                return 0;
            }
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

                Adopt(index, hud);
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

            if (!Players.IsHumanViewer(player) || !EnsureOne(index) || _layouts[index] is not { } layout)
            {
                return false;
            }

            hud = layout;
            int slot = player.Slot;
            if (slot < 0)
            {
                return false;
            }

            if (slot < _layoutSlotCount[index])
            {
                return true;
            }

            // Re-read only when the cached count does not cover the slot (e.g. sized after spawn).
            _layoutSlotCount[index] = ReadSlotCount(layout);
            return slot < _layoutSlotCount[index];
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

        private static void SetRootVisible(int slot, string panelId, bool visible)
        {
            int root = RootIndex(panelId);
            if (root >= 0 && (uint)slot < MaxSlots)
            {
                _rootVisible[root][slot] = visible;
            }
        }

        private static void MarkClosingRoot(int slot, int layoutIndex)
        {
            if (layoutIndex < 0 || (uint)slot >= MaxSlots)
            {
                return;
            }

            long until = Environment.TickCount64 + CloseTransmitGraceMs;
            _closingUntil[layoutIndex][slot] = until;
            _nextClosingExpiry = Math.Min(_nextClosingExpiry, until);
        }

        private static void ClearClosingRoot(int slot, int layoutIndex)
        {
            if (layoutIndex >= 0 && (uint)slot < MaxSlots)
            {
                _closingUntil[layoutIndex][slot] = 0;
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
