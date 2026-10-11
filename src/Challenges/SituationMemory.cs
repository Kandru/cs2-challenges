using System.Globalization;
using System.Text;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Challenges.Utils;
using CounterStrikeSharp.API.Modules.Events;

namespace Challenges
{
    /// <summary>
    /// Volatile round memory for trade kills, grenade holding/thrown, and smoke defuses.
    /// Never persisted. Cleared on round start, map end, and unload.
    /// </summary>
    internal sealed class SituationMemory
    {
        private const float SmokeRadiusSq = 144f * 144f;
        private const double DefaultTradeSeconds = 5.0;
        private const double ThrownExpireSeconds = 8.0;
        private const double SmokeExpireSeconds = 22.0;
        private const PlayerButtons AttackMask = PlayerButtons.Attack | PlayerButtons.Attack2;

        private static readonly HashSet<string> GrenadeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "hegrenade", "flashbang", "smokegrenade", "molotov", "incgrenade", "decoy",
        };

        private readonly List<KillRecord> _kills = [];
        private readonly Dictionary<int, string> _holding = [];
        private readonly List<ThrownGrenade> _thrown = [];
        private readonly List<LiveSmoke> _smokes = [];
        private readonly HashSet<string> _watchEvents = new(StringComparer.Ordinal);
        private readonly HashSet<int> _pendingThrownRemove = [];
        private readonly StringBuilder _thrownScratch = new(32);

        private float _bombX;
        private float _bombY;
        private float _bombZ;
        private bool _hasBombOrigin;
        private bool _thrownFlushScheduled;
        private double _tradeRetentionSeconds = DefaultTradeSeconds;

        public bool TradeEnabled { get; private set; }
        public bool GrenadeEnabled { get; private set; }
        public bool SmokeEnabled { get; private set; }
        public bool AnyEnabled => TradeEnabled | GrenadeEnabled | SmokeEnabled;

        public void Configure(IEnumerable<ChallengeTask> tasks)
        {
            bool trade = false;
            bool grenade = false;
            bool smoke = false;
            double maxTrade = 0;

            foreach (ChallengeTask task in tasks)
            {
                bool isTrade = string.Equals(task.Type, "player_trade", StringComparison.Ordinal);
                if (isTrade)
                {
                    trade = true;
                }

                bool sawSeconds = false;
                foreach (ChallengeRule rule in task.Rules)
                {
                    string key = rule.Key;
                    if (!grenade && key.Contains(".grenade", StringComparison.Ordinal))
                    {
                        grenade = true;
                    }

                    if (!smoke
                        && (key is "insmoke" or "smoke.distance" or "smoke.age"
                            || key.EndsWith(".insmoke", StringComparison.Ordinal)))
                    {
                        smoke = true;
                    }

                    if (!isTrade
                        || !string.Equals(key, "seconds", StringComparison.Ordinal)
                        || !float.TryParse(rule.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float sec))
                    {
                        continue;
                    }

                    sawSeconds = true;
                    if (sec > maxTrade)
                    {
                        maxTrade = sec;
                    }
                }

                if (isTrade && !sawSeconds && DefaultTradeSeconds > maxTrade)
                {
                    maxTrade = DefaultTradeSeconds;
                }
            }

            TradeEnabled = trade;
            GrenadeEnabled = grenade;
            SmokeEnabled = smoke;
            _tradeRetentionSeconds = trade && maxTrade > 0 ? maxTrade : DefaultTradeSeconds;

            _watchEvents.Clear();
            if (trade | grenade)
            {
                _watchEvents.Add("EventPlayerDeath");
            }

            if (grenade)
            {
                _watchEvents.Add("EventGrenadeThrown");
                _watchEvents.Add("EventHegrenadeDetonate");
                _watchEvents.Add("EventFlashbangDetonate");
                _watchEvents.Add("EventSmokegrenadeDetonate");
                _watchEvents.Add("EventMolotovDetonate");
                _watchEvents.Add("EventDecoyDetonate");
                _watchEvents.Add("EventItemEquip");
                _watchEvents.Add("OnPlayerButtonsChanged");
            }

            if (smoke)
            {
                _watchEvents.Add("EventSmokegrenadeDetonate");
                _watchEvents.Add("EventSmokegrenadeExpired");
                _watchEvents.Add("EventBombBegindefuse");
                _watchEvents.Add("EventBombDefused");
            }
        }

        public bool Watches(string eventClassName) => _watchEvents.Contains(eventClassName);

        public void Clear()
        {
            _kills.Clear();
            _holding.Clear();
            _thrown.Clear();
            _smokes.Clear();
            _pendingThrownRemove.Clear();
            _thrownFlushScheduled = false;
            _hasBombOrigin = false;
        }

        public void ForgetPlayer(CCSPlayerController player)
        {
            int slot = player.Slot;
            _holding.Remove(slot);
            _thrown.RemoveAll(t => t.Slot == slot);
        }

        public void Observe(
            string eventClassName,
            GameEvent gameEvent,
            Action<CCSPlayerController, string, Dictionary<string, string>> enqueueOne)
        {
            switch (eventClassName)
            {
                case "EventPlayerDeath":
                    if (TradeEnabled)
                    {
                        OnPlayerDeath((EventPlayerDeath)gameEvent, enqueueOne);
                    }

                    return;
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
                    OnSmokeDetonate((EventSmokegrenadeDetonate)gameEvent);
                    return;
                case "EventMolotovDetonate":
                    ScheduleOldestThrown("molotov", "incgrenade");
                    return;
                case "EventDecoyDetonate":
                    ScheduleOldestThrown("decoy");
                    return;
                case "EventSmokegrenadeExpired":
                    int entityId = ((EventSmokegrenadeExpired)gameEvent).Entityid;
                    _smokes.RemoveAll(s => s.EntityId == entityId);
                    return;
                case "EventBombBegindefuse":
                case "EventBombDefused":
                    TryCacheBombOrigin();
                    return;
            }
        }

        public void Enrich(string eventClassName, GameEvent gameEvent, Dictionary<string, string> d)
        {
            if (GrenadeEnabled && eventClassName == "EventPlayerDeath")
            {
                var e = (EventPlayerDeath)gameEvent;
                WriteGrenadeKeys(d, "attacker", e.Attacker);
                WriteGrenadeKeys(d, "victim", e.Userid);
                d["userid.grenade"] = d["victim.grenade"];
                d["userid.grenade.holding"] = d["victim.grenade.holding"];
                d["userid.grenade.thrown"] = d["victim.grenade.thrown"];
                if (e.Userid is { IsValid: true } victim)
                {
                    _holding.Remove(victim.Slot);
                }

                return;
            }

            if (SmokeEnabled && eventClassName is "EventBombBegindefuse" or "EventBombDefused")
            {
                WriteSmokeKeys(d, eventClassName, gameEvent);
            }
        }

        public void OnButtonsChanged(CCSPlayerController player, PlayerButtons pressed, PlayerButtons released)
        {
            if (!GrenadeEnabled || player is not { IsValid: true })
            {
                return;
            }

            int slot = player.Slot;
            if (!TryActiveGrenade(player, out string? active, out bool pinPulled))
            {
                _holding.Remove(slot);
                return;
            }

            bool pressedAttack = (pressed & AttackMask) != 0;
            bool releasedAttack = (released & AttackMask) != 0;
            if (!pressedAttack && !releasedAttack && !_holding.ContainsKey(slot))
            {
                return;
            }

            if (pinPulled)
            {
                _holding[slot] = active!;
                return;
            }

            if (releasedAttack)
            {
                _holding.Remove(slot);
            }
        }

        private void OnPlayerDeath(
            EventPlayerDeath e,
            Action<CCSPlayerController, string, Dictionary<string, string>> enqueueOne)
        {
            CCSPlayerController? victim = e.Userid;
            CCSPlayerController? attacker = e.Attacker;
            if (victim is not { IsValid: true }
                || attacker is not { IsValid: true }
                || ReferenceEquals(attacker, victim))
            {
                return;
            }

            double now = NowSeconds();
            PruneKills(now);

            int attackerSlot = attacker.Slot;
            int victimSlot = victim.Slot;
            int reverseIndex = -1;
            for (int i = _kills.Count - 1; i >= 0; i--)
            {
                KillRecord k = _kills[i];
                if (k.AttackerSlot != victimSlot
                    || k.VictimSlot != attackerSlot
                    || now - k.At > _tradeRetentionSeconds)
                {
                    continue;
                }

                reverseIndex = i;
                break;
            }

            string weapon = e.Weapon ?? "";
            bool teamKill = attacker.TeamNum == victim.TeamNum;
            if (reverseIndex < 0)
            {
                _kills.Add(new KillRecord(attackerSlot, victimSlot, attacker, victim, weapon, e.Headshot, teamKill, now));
                return;
            }

            KillRecord first = _kills[reverseIndex];
            _kills.RemoveAt(reverseIndex);
            double seconds = Math.Max(0, now - first.At);
            var second = new KillRecord(attackerSlot, victimSlot, attacker, victim, weapon, e.Headshot, teamKill, now);
            EmitTrade(enqueueOne, first, second, seconds);
            EmitTrade(enqueueOne, second, first, seconds);
        }

        private static void EmitTrade(
            Action<CCSPlayerController, string, Dictionary<string, string>> enqueueOne,
            KillRecord mine,
            KillRecord theirs,
            double seconds)
        {
            CCSPlayerController player = mine.Attacker;
            CCSPlayerController other = mine.Victim;
            if (!player.IsValid || !other.IsValid)
            {
                return;
            }

            string myWeapon = ShortWeapon(mine.Weapon);
            string theirWeapon = ShortWeapon(theirs.Weapon);
            Dictionary<string, string> d = new(16)
            {
                ["seconds"] = EventData.Num((float)seconds),
                ["weapon"] = myWeapon,
                ["other_weapon"] = theirWeapon,
                ["same_weapon"] = EventData.Bool(SameWeapon(myWeapon, theirWeapon)),
                ["headshot"] = EventData.Bool(mine.Headshot),
                ["other_headshot"] = EventData.Bool(theirs.Headshot),
                ["isteamkill"] = EventData.Bool(mine.TeamKill | theirs.TeamKill),
                ["isselfkill"] = "false",
            };
            EventData.FillPlayer(d, player, "attacker");
            EventData.FillPlayer(d, other, "victim");
            EventData.FillPlayer(d, other, "userid");
            enqueueOne(player, "player_trade", d);
        }

        private void OnGrenadeThrown(EventGrenadeThrown e)
        {
            if (e.Userid is not { IsValid: true } player)
            {
                return;
            }

            string shortName = ShortWeapon(e.Weapon);
            if (!GrenadeNames.Contains(shortName))
            {
                return;
            }

            int slot = player.Slot;
            _holding.Remove(slot);
            _thrown.Add(new ThrownGrenade(slot, shortName, NowSeconds()));
        }

        private void OnItemEquip(EventItemEquip e)
        {
            if (e.Userid is not { IsValid: true } player)
            {
                return;
            }

            int slot = player.Slot;
            string shortName = ShortWeapon(e.Item);
            if (GrenadeNames.Contains(shortName)
                && TryActiveGrenade(player, out _, out bool pinPulled)
                && pinPulled)
            {
                _holding[slot] = shortName;
                return;
            }

            _holding.Remove(slot);
        }

        private void OnSmokeDetonate(EventSmokegrenadeDetonate e)
        {
            if (GrenadeEnabled)
            {
                ScheduleOldestThrown("smokegrenade");
            }

            if (SmokeEnabled)
            {
                _smokes.Add(new LiveSmoke(e.Entityid, e.X, e.Y, e.Z, NowSeconds()));
            }
        }

        private void ScheduleOldestThrown(string shortName, string? altName = null)
        {
            double now = NowSeconds();
            PruneThrown(now);

            int index = FindOldestThrown(shortName);
            if (altName != null)
            {
                int alt = FindOldestThrown(altName);
                if (index < 0 || (alt >= 0 && _thrown[alt].At < _thrown[index].At))
                {
                    index = alt;
                }
            }

            if (index < 0 || !_pendingThrownRemove.Add(_thrown[index].Id) || _thrownFlushScheduled)
            {
                return;
            }

            _thrownFlushScheduled = true;
            Server.NextFrame(FlushPendingThrownRemovals);
        }

        private void FlushPendingThrownRemovals()
        {
            _thrownFlushScheduled = false;
            if (_pendingThrownRemove.Count == 0)
            {
                return;
            }

            _thrown.RemoveAll(t => _pendingThrownRemove.Contains(t.Id));
            _pendingThrownRemove.Clear();
        }

        private void WriteGrenadeKeys(Dictionary<string, string> d, string prefix, CCSPlayerController? player)
        {
            if (player is not { IsValid: true })
            {
                d[$"{prefix}.grenade"] = "none";
                d[$"{prefix}.grenade.holding"] = "";
                d[$"{prefix}.grenade.thrown"] = "";
                return;
            }

            int slot = player.Slot;
            bool holding = _holding.TryGetValue(slot, out string? holdWeapon);
            string thrown = BuildThrownList(slot);
            d[$"{prefix}.grenade"] = (holding, thrown.Length > 0) switch
            {
                (true, true) => "both",
                (true, false) => "holding",
                (false, true) => "thrown",
                _ => "none",
            };
            d[$"{prefix}.grenade.holding"] = holding ? holdWeapon! : "";
            d[$"{prefix}.grenade.thrown"] = thrown;
        }

        private string BuildThrownList(int slot)
        {
            _thrownScratch.Clear();
            double now = NowSeconds();
            for (int i = 0; i < _thrown.Count; i++)
            {
                ThrownGrenade t = _thrown[i];
                if (t.Slot != slot || now - t.At > ThrownExpireSeconds)
                {
                    continue;
                }

                if (_thrownScratch.Length > 0)
                {
                    _thrownScratch.Append(',');
                }

                _thrownScratch.Append(t.ShortName);
            }

            return _thrownScratch.Length == 0 ? "" : _thrownScratch.ToString();
        }

        private void WriteSmokeKeys(Dictionary<string, string> d, string eventClassName, GameEvent gameEvent)
        {
            PruneSmokes(NowSeconds());
            TryCacheBombOrigin();

            CCSPlayerController? defuser = eventClassName == "EventBombBegindefuse"
                ? ((EventBombBegindefuse)gameEvent).Userid
                : ((EventBombDefused)gameEvent).Userid;

            ProbeBomb(out bool bombIn, out float bombDist, out float bombAge);
            ProbePlayer(defuser, out bool playerIn);

            d["insmoke"] = EventData.Bool(bombIn);
            d["smoke.distance"] = EventData.Num(bombDist);
            d["smoke.age"] = EventData.Num(bombAge);

            string playerPrefix = eventClassName == "EventBombBegindefuse" ? "player" : "defuser";
            string playerInSmoke = EventData.Bool(playerIn);
            d[$"{playerPrefix}.insmoke"] = playerInSmoke;
            d["userid.insmoke"] = playerInSmoke;
        }

        private void ProbeBomb(out bool inside, out float distance, out float age)
        {
            if (!_hasBombOrigin)
            {
                inside = false;
                distance = 9999f;
                age = 0f;
                return;
            }

            Probe(_bombX, _bombY, _bombZ, out inside, out distance, out age);
        }

        private void ProbePlayer(CCSPlayerController? player, out bool inside)
        {
            if (!TryOrigin(player, out float x, out float y, out float z))
            {
                inside = false;
                return;
            }

            Probe(x, y, z, out inside, out _, out _);
        }

        private void Probe(float x, float y, float z, out bool inside, out float distance, out float age)
        {
            if (_smokes.Count == 0)
            {
                inside = false;
                distance = 9999f;
                age = 0f;
                return;
            }

            double now = NowSeconds();
            float bestCoveringSq = float.MaxValue;
            float bestCoveringAge = 0f;
            float bestAnySq = float.MaxValue;
            float bestAnyAge = 0f;
            inside = false;

            for (int i = 0; i < _smokes.Count; i++)
            {
                LiveSmoke smoke = _smokes[i];
                float dx = x - smoke.X;
                float dy = y - smoke.Y;
                float dz = z - smoke.Z;
                float distSq = (dx * dx) + (dy * dy) + (dz * dz);
                float smokeAge = (float)Math.Max(0, now - smoke.At);

                if (distSq < bestAnySq)
                {
                    bestAnySq = distSq;
                    bestAnyAge = smokeAge;
                }

                if (distSq > SmokeRadiusSq || distSq >= bestCoveringSq)
                {
                    continue;
                }

                inside = true;
                bestCoveringSq = distSq;
                bestCoveringAge = smokeAge;
            }

            if (inside)
            {
                distance = MathF.Sqrt(bestCoveringSq);
                age = bestCoveringAge;
                return;
            }

            distance = MathF.Sqrt(bestAnySq);
            age = bestAnyAge;
        }

        private void TryCacheBombOrigin()
        {
            foreach (CPlantedC4 bomb in Utilities.FindAllEntitiesByDesignerName<CPlantedC4>("planted_c4"))
            {
                if (bomb is not { IsValid: true, AbsOrigin: { } origin })
                {
                    continue;
                }

                _bombX = origin.X;
                _bombY = origin.Y;
                _bombZ = origin.Z;
                _hasBombOrigin = true;
                return;
            }
        }

        private static bool TryOrigin(CCSPlayerController? player, out float x, out float y, out float z)
        {
            x = y = z = 0;
            CCSPlayerPawn? pawn = player is { IsValid: true } ? player.PlayerPawn?.Value : null;
            if (pawn is not { IsValid: true, AbsOrigin: { } origin })
            {
                return false;
            }

            x = origin.X;
            y = origin.Y;
            z = origin.Z;
            return true;
        }

        private void PruneKills(double now)
        {
            double keep = _tradeRetentionSeconds;
            _kills.RemoveAll(k => now - k.At > keep);
        }

        private void PruneThrown(double now) =>
            _thrown.RemoveAll(t => now - t.At > ThrownExpireSeconds);

        private void PruneSmokes(double now) =>
            _smokes.RemoveAll(s => now - s.At > SmokeExpireSeconds);

        private int FindOldestThrown(string shortName)
        {
            int best = -1;
            double bestAt = double.MaxValue;
            for (int i = 0; i < _thrown.Count; i++)
            {
                ThrownGrenade t = _thrown[i];
                if (_pendingThrownRemove.Contains(t.Id)
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

            string name = ShortWeapon(weapon.DesignerName);
            if (!GrenadeNames.Contains(name))
            {
                return false;
            }

            shortName = name;
            pinPulled = weapon is CBaseCSGrenade grenade && grenade.PinPulled;
            return true;
        }

        private static bool SameWeapon(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            || (left.Contains("knife", StringComparison.OrdinalIgnoreCase)
                && right.Contains("knife", StringComparison.OrdinalIgnoreCase));

        private static string ShortWeapon(string? weapon)
        {
            if (string.IsNullOrEmpty(weapon))
            {
                return "";
            }

            return weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
                ? weapon[7..]
                : weapon;
        }

        private static double NowSeconds() =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 0.001;

        private sealed class KillRecord(
            int attackerSlot,
            int victimSlot,
            CCSPlayerController attacker,
            CCSPlayerController victim,
            string weapon,
            bool headshot,
            bool teamKill,
            double at)
        {
            public int AttackerSlot { get; } = attackerSlot;
            public int VictimSlot { get; } = victimSlot;
            public CCSPlayerController Attacker { get; } = attacker;
            public CCSPlayerController Victim { get; } = victim;
            public string Weapon { get; } = weapon;
            public bool Headshot { get; } = headshot;
            public bool TeamKill { get; } = teamKill;
            public double At { get; } = at;
        }

        private sealed class ThrownGrenade(int slot, string shortName, double at)
        {
            private static int _nextId;
            public int Id { get; } = Interlocked.Increment(ref _nextId);
            public int Slot { get; } = slot;
            public string ShortName { get; } = shortName;
            public double At { get; } = at;
        }

        private readonly struct LiveSmoke(int entityId, float x, float y, float z, double at)
        {
            public int EntityId { get; } = entityId;
            public float X { get; } = x;
            public float Y { get; } = y;
            public float Z { get; } = z;
            public double At { get; } = at;
        }
    }
}
