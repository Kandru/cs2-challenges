using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// Grenade in hand (pin pulled), thrown recently, or being thrown, per player slot. Writes the
    /// <c>grenade*</c> suffixes for any player prefix. A victim's holding state is dropped after its death event.
    /// </summary>
    internal sealed class GrenadeProvider : IAttributeProvider
    {
        public static readonly AttributeSuffix[] Suffixes =
        [
            new AttributeSuffix("grenade", "string"),
            new AttributeSuffix("grenade.holding", "string"),
            new AttributeSuffix("grenade.thrown", "string"),
            new AttributeSuffix("grenade.throwing", "string"),
        ];

        private const uint StateBit = 1u << 0;
        private const uint HoldingBit = 1u << 1;
        private const uint ThrownBit = 1u << 2;
        private const uint ThrowingBit = 1u << 3;
        private const double ThrownExpireSeconds = 8.0;
        private const double ThrowingExpireSeconds = 1.0;
        private const PlayerButtons AttackMask = PlayerButtons.Attack | PlayerButtons.Attack2;

        private static readonly string[] Watched =
        [
            "EventGrenadeThrown",
            "EventHegrenadeDetonate",
            "EventFlashbangDetonate",
            "EventSmokegrenadeDetonate",
            "EventMolotovDetonate",
            "EventDecoyDetonate",
            "EventItemEquip",
            "EventPlayerDeath",
            "OnPlayerButtonsChanged",
        ];

        private static readonly HashSet<string> GrenadeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade", "flashbang", "smokegrenade", "molotov", "incgrenade", "decoy",
        };

        /// <summary>Grenade short name with the pin pulled, per slot; null when none.</summary>
        private readonly string?[] _holding = new string?[RoundRoster.MaxSlots];
        private readonly List<ThrownGrenade> _thrown = [];
        /// <summary>Names of one grenade list being built; joined by <see cref="JoinNames"/>.</summary>
        private readonly List<string> _names = [];
        private readonly StringBuilder _scratch = new(32);
        private bool _thrownFlushScheduled;

        public IReadOnlyList<AttributeSuffix> PlayerSuffixes => Suffixes;
        public IReadOnlyCollection<string> WatchedEvents => Watched;

        public bool Configure(IReadOnlyList<ChallengeTask> tasks) =>
            AttributeKeys.AnyRule(tasks, static key => AttributeKeys.SuffixIndex(key, Suffixes) >= 0);

        public void Observe(string eventName, GameEvent gameEvent)
        {
            switch (eventName)
            {
                case "EventGrenadeThrown":
                    OnGrenadeThrown((EventGrenadeThrown)gameEvent);
                    return;
                case "EventItemEquip":
                    OnItemEquip((EventItemEquip)gameEvent);
                    return;
                case "EventHegrenadeDetonate":
                    ScheduleOldestThrown("hegrenade");
                    return;
                case "EventFlashbangDetonate":
                    ScheduleOldestThrown("flashbang");
                    return;
                case "EventSmokegrenadeDetonate":
                    ScheduleOldestThrown("smokegrenade");
                    return;
                case "EventMolotovDetonate":
                    ScheduleOldestThrown("molotov", "incgrenade");
                    return;
                case "EventDecoyDetonate":
                    ScheduleOldestThrown("decoy");
                    return;
            }
        }

        public void AfterEvent(string eventName, GameEvent gameEvent)
        {
            if (eventName == "EventPlayerDeath" && ((EventPlayerDeath)gameEvent).Userid is { IsValid: true } victim)
            {
                SetHolding(victim.Slot, null);
            }
        }

        public void WritePlayer(Dictionary<string, EventValue> d, string[] keys, uint suffixes, CCSPlayerController? player)
        {
            if (player is not { IsValid: true })
            {
                WriteIf(d, keys, suffixes, StateBit, 0, "none");
                WriteIf(d, keys, suffixes, HoldingBit, 1, "");
                WriteIf(d, keys, suffixes, ThrownBit, 2, "");
                WriteIf(d, keys, suffixes, ThrowingBit, 3, "");
                return;
            }

            int slot = player.Slot;
            string? holdWeapon = HoldingOf(slot);
            bool holding = holdWeapon != null;
            double now = RoundClock.NowSeconds();
            string thrown = (suffixes & (StateBit | ThrownBit)) != 0 ? BuildThrownList(slot, now) : "";
            if ((suffixes & StateBit) != 0)
            {
                d[keys[0]] = EventValue.Of((holding, thrown.Length > 0) switch
                {
                    (true, true) => "both",
                    (true, false) => "holding",
                    (false, true) => "thrown",
                    _ => "none",
                });
            }

            WriteIf(d, keys, suffixes, HoldingBit, 1, holdWeapon ?? "");
            WriteIf(d, keys, suffixes, ThrownBit, 2, thrown);
            if ((suffixes & ThrowingBit) != 0)
            {
                d[keys[3]] = EventValue.Of(BuildThrowingList(player, slot, holding, holdWeapon, now));
            }
        }

        public void OnButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (player is not { IsValid: true })
            {
                return;
            }

            int slot = player.Slot;
            bool releasedAttack = (released & AttackMask) != 0;
            if (!releasedAttack && (pressed & AttackMask) == 0 && HoldingOf(slot) == null)
            {
                return;
            }

            if (!TryActiveGrenade(player, out string? active, out bool pinPulled))
            {
                SetHolding(slot, null);
                return;
            }

            if (pinPulled)
            {
                SetHolding(slot, active!);
                return;
            }

            if (releasedAttack)
            {
                SetHolding(slot, null);
            }
        }

        public void Clear()
        {
            Array.Clear(_holding);
            _thrown.Clear();
            _thrownFlushScheduled = false;
        }

        public void ForgetSlot(int slot)
        {
            SetHolding(slot, null);
            for (int i = _thrown.Count - 1; i >= 0; i--)
            {
                if (_thrown[i].Slot == slot)
                {
                    _thrown.RemoveAt(i);
                }
            }
        }

        private static void WriteIf(Dictionary<string, EventValue> d, string[] keys, uint suffixes, uint bit, int index, string value)
        {
            if ((suffixes & bit) != 0)
            {
                d[keys[index]] = EventValue.Of(value);
            }
        }

        private string? HoldingOf(int slot) => (uint)slot < (uint)_holding.Length ? _holding[slot] : null;

        private void SetHolding(int slot, string? shortName)
        {
            if ((uint)slot < (uint)_holding.Length)
            {
                _holding[slot] = shortName;
            }
        }

        private void OnGrenadeThrown(EventGrenadeThrown e)
        {
            if (e.Userid is not { IsValid: true } player)
            {
                return;
            }

            string shortName = Weapons.Short(e.Weapon);
            if (!GrenadeNames.Contains(shortName))
            {
                return;
            }

            int slot = player.Slot;
            SetHolding(slot, null);
            _thrown.Add(new ThrownGrenade(slot, shortName, RoundClock.NowSeconds(), Detonated: false));
        }

        private void OnItemEquip(EventItemEquip e)
        {
            if (e.Userid is not { IsValid: true } player)
            {
                return;
            }

            int slot = player.Slot;
            string shortName = Weapons.Short(e.Item);
            if (GrenadeNames.Contains(shortName)
                && TryActiveGrenade(player, out _, out bool pinPulled)
                && pinPulled)
            {
                SetHolding(slot, shortName);
                return;
            }

            SetHolding(slot, null);
        }

        private void ScheduleOldestThrown(string shortName, string? altName = null)
        {
            PruneThrown(RoundClock.NowSeconds());

            int index = FindOldestThrown(shortName);
            if (altName != null)
            {
                int alt = FindOldestThrown(altName);
                if (index < 0 || (alt >= 0 && _thrown[alt].At < _thrown[index].At))
                {
                    index = alt;
                }
            }

            if (index < 0)
            {
                return;
            }

            // Still listed for the detonate event itself; dropped on the next frame.
            _thrown[index] = _thrown[index] with { Detonated = true };
            if (_thrownFlushScheduled)
            {
                return;
            }

            _thrownFlushScheduled = true;
            Server.NextFrame(FlushDetonatedThrown);
        }

        private void FlushDetonatedThrown()
        {
            _thrownFlushScheduled = false;
            for (int i = _thrown.Count - 1; i >= 0; i--)
            {
                if (_thrown[i].Detonated)
                {
                    _thrown.RemoveAt(i);
                }
            }
        }

        private string BuildThrownList(int slot, double now)
        {
            _names.Clear();
            for (int i = 0; i < _thrown.Count; i++)
            {
                ThrownGrenade t = _thrown[i];
                if (t.Slot == slot && now - t.At <= ThrownExpireSeconds)
                {
                    _names.Add(t.ShortName);
                }
            }

            return JoinNames();
        }

        /// <summary>
        /// Grenade names for Killerqueen-style checks: active grenade in hand,
        /// pin pulled, or thrown within the last second (switch-back window).
        /// </summary>
        private string BuildThrowingList(
            CCSPlayerController player,
            int slot,
            bool holding,
            string? holdWeapon,
            double now)
        {
            _names.Clear();
            if (TryActiveGrenade(player, out string? active, out _))
            {
                AddUniqueName(active);
            }

            if (holding)
            {
                AddUniqueName(holdWeapon);
            }

            for (int i = 0; i < _thrown.Count; i++)
            {
                ThrownGrenade t = _thrown[i];
                if (t.Slot == slot && now - t.At <= ThrowingExpireSeconds)
                {
                    AddUniqueName(t.ShortName);
                }
            }

            return JoinNames();
        }

        private void AddUniqueName(string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            foreach (string listed in _names)
            {
                if (string.Equals(listed, name, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            _names.Add(name);
        }

        /// <summary><see cref="_names"/> comma separated; a single name is returned as is.</summary>
        private string JoinNames()
        {
            switch (_names.Count)
            {
                case 0:
                    return "";
                case 1:
                    return _names[0];
            }

            _scratch.Clear();
            foreach (string name in _names)
            {
                if (_scratch.Length > 0)
                {
                    _scratch.Append(',');
                }

                _scratch.Append(name);
            }

            return _scratch.ToString();
        }

        private void PruneThrown(double now)
        {
            for (int i = _thrown.Count - 1; i >= 0; i--)
            {
                if (now - _thrown[i].At > ThrownExpireSeconds)
                {
                    _thrown.RemoveAt(i);
                }
            }
        }

        private int FindOldestThrown(string shortName)
        {
            int best = -1;
            double bestAt = double.MaxValue;
            for (int i = 0; i < _thrown.Count; i++)
            {
                ThrownGrenade t = _thrown[i];
                if (t.Detonated
                    || t.At >= bestAt
                    || !string.Equals(t.ShortName, shortName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bestAt = t.At;
                best = i;
            }

            return best;
        }

        private static bool TryActiveGrenade(CCSPlayerController player, out string? shortName, out bool pinPulled)
        {
            shortName = null;
            pinPulled = false;
            CBasePlayerWeapon? weapon = player.PlayerPawn?.Value?.WeaponServices?.ActiveWeapon?.Value;
            if (weapon is not { IsValid: true })
            {
                return false;
            }

            string name = Weapons.Short(weapon.DesignerName);
            if (!GrenadeNames.Contains(name))
            {
                return false;
            }

            shortName = name;
            pinPulled = weapon is CBaseCSGrenade grenade && grenade.PinPulled;
            return true;
        }

        /// <summary>A grenade thrown by <paramref name="Slot"/>; <paramref name="Detonated"/> marks it for removal on the next frame.</summary>
        private readonly record struct ThrownGrenade(int Slot, string ShortName, double At, bool Detonated);
    }
}
