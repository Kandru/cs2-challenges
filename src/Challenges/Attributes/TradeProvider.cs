using System.Collections.ObjectModel;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// Recent kills; a mutual kill inside the window emits the synthetic <see cref="TradeType"/> once for each
    /// of the two players. Writes no keys into other events.
    /// </summary>
    internal sealed class TradeProvider : IAttributeProvider
    {
        /// <summary>Synthetic challenge type emitted for a mutual kill.</summary>
        public const string TradeType = "player_trade";

        /// <summary>
        /// Key layout of <see cref="TradeType"/>. The other player is written under <c>victim</c> only;
        /// rules may also use <c>userid</c>, resolved at load.
        /// </summary>
        public static readonly SyntheticKeySource TradeKeys = new(
            [
                "seconds", "weapon", "other_weapon", "same_weapon",
                "headshot", "other_headshot", "isteamkill", "isselfkill",
            ],
            ["attacker", "victim"],
            ReadOnlyDictionary<string, string>.Empty,
            new Dictionary<string, string>(StringComparer.Ordinal) { ["userid"] = "victim" });

        private const double DefaultTradeSeconds = 5.0;

        private static readonly string[] TradeAttackerKeys = PlayerKeys.For("attacker");
        private static readonly string[] TradeVictimKeys = PlayerKeys.For("victim");
        private static readonly string[] Watched = ["EventPlayerDeath"];

        private readonly List<KillRecord> _kills = [];
        private double _retentionSeconds = DefaultTradeSeconds;
        /// <summary>Player fields the loaded <c>player_trade</c> rules read, per trade prefix.</summary>
        private PlayerFields _attackerFields;
        private PlayerFields _victimFields;

        public string Name => "trade";
        public IReadOnlyList<AttributeSuffix> PlayerSuffixes => [];
        public IReadOnlyList<AttributeEventKey> EventKeys => [];
        public bool UsesRoster => false;
        public IReadOnlyCollection<string> WatchedEvents => Watched;

        public bool Configure(IReadOnlyList<ChallengeTask> tasks)
        {
            bool trade = false;
            double maxTrade = 0;
            PlayerFields attacker = PlayerFields.None;
            PlayerFields victim = PlayerFields.None;
            foreach (ChallengeTask task in tasks)
            {
                if (!string.Equals(task.Type, TradeType, StringComparison.Ordinal))
                {
                    continue;
                }

                trade = true;
                bool sawSeconds = false;
                foreach (CompiledRule rule in task.CompiledRules)
                {
                    string key = rule.Key;
                    attacker |= PlayerKeys.TryMatch(key, "attacker", out PlayerFields a) ? a : PlayerFields.None;
                    victim |= PlayerKeys.TryMatch(key, "victim", out PlayerFields v) ? v : PlayerFields.None;
                    if (!string.Equals(key, "seconds", StringComparison.Ordinal) || rule.Number is not double sec)
                    {
                        continue;
                    }

                    sawSeconds = true;
                    if (sec > maxTrade)
                    {
                        maxTrade = sec;
                    }
                }

                if (!sawSeconds && DefaultTradeSeconds > maxTrade)
                {
                    maxTrade = DefaultTradeSeconds;
                }
            }

            _attackerFields = attacker;
            _victimFields = victim;
            _retentionSeconds = trade && maxTrade > 0 ? maxTrade : DefaultTradeSeconds;
            return trade;
        }

        public void Observe(string eventName, GameEvent gameEvent, ProviderContext context)
        {
            if (eventName == "EventPlayerDeath")
            {
                OnPlayerDeath((EventPlayerDeath)gameEvent, context);
            }
        }

        public void AfterEvent(string eventName, GameEvent gameEvent)
        {
        }

        public void WritePlayer(Dictionary<string, EventValue> d, string[] keys, uint suffixes, CCSPlayerController? player)
        {
        }

        public void WriteEvent(string eventName, GameEvent gameEvent, Dictionary<string, EventValue> d, uint keys)
        {
        }

        public void Clear() => _kills.Clear();

        public void ForgetSlot(int slot)
        {
            for (int i = _kills.Count - 1; i >= 0; i--)
            {
                KillRecord k = _kills[i];
                if (k.AttackerSlot == slot || k.VictimSlot == slot)
                {
                    _kills.RemoveAt(i);
                }
            }
        }

        private void OnPlayerDeath(EventPlayerDeath e, ProviderContext context)
        {
            CCSPlayerController? victim = e.Userid;
            CCSPlayerController? attacker = e.Attacker;
            if (victim is not { IsValid: true } || attacker is not { IsValid: true })
            {
                return;
            }

            int attackerSlot = attacker.Slot;
            int victimSlot = victim.Slot;
            if (attackerSlot == victimSlot)
            {
                return;
            }

            double now = RoundClock.NowSeconds();
            PruneKills(now);

            int reverseIndex = -1;
            for (int i = _kills.Count - 1; i >= 0; i--)
            {
                KillRecord k = _kills[i];
                if (k.AttackerSlot != victimSlot
                    || k.VictimSlot != attackerSlot
                    || now - k.At > _retentionSeconds)
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
            EmitTrade(context, first, second, seconds);
            EmitTrade(context, second, first, seconds);
        }

        private void EmitTrade(ProviderContext context, KillRecord mine, KillRecord theirs, double seconds)
        {
            CCSPlayerController player = mine.Attacker;
            CCSPlayerController other = mine.Victim;
            if (!player.IsValid || !other.IsValid)
            {
                return;
            }

            string myWeapon = Weapons.Short(mine.Weapon);
            string theirWeapon = Weapons.Short(theirs.Weapon);
            Dictionary<string, EventValue> d = new(16)
            {
                ["seconds"] = EventValue.Of((float)seconds),
                ["weapon"] = EventValue.Of(myWeapon),
                ["other_weapon"] = EventValue.Of(theirWeapon),
                ["same_weapon"] = EventValue.Of(Weapons.Same(myWeapon, theirWeapon)),
                ["headshot"] = EventValue.Of(mine.Headshot),
                ["other_headshot"] = EventValue.Of(theirs.Headshot),
                ["isteamkill"] = EventValue.Of(mine.TeamKill | theirs.TeamKill),
                ["isselfkill"] = EventValue.Of(false),
            };
            EventData.FillPlayer(d, player, TradeAttackerKeys, _attackerFields);
            EventData.FillPlayer(d, other, TradeVictimKeys, _victimFields);
            context.EnqueueOne(player, TradeType, d);
        }

        private void PruneKills(double now)
        {
            double keep = _retentionSeconds;
            for (int i = _kills.Count - 1; i >= 0; i--)
            {
                if (now - _kills[i].At > keep)
                {
                    _kills.RemoveAt(i);
                }
            }
        }

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
    }
}
