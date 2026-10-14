using System.Collections.ObjectModel;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// Recent kills; a mutual kill inside the window emits the synthetic <see cref="TradeType"/> once for each
    /// of the two players: <c>targetOne</c> checks the player first, data is only built for an accepted target and
    /// handed to <c>enqueueOne</c>. Writes no keys into other events; the registry feeds it <c>player_death</c>
    /// while a <see cref="TradeType"/> task is loaded. Game thread only.
    /// </summary>
    internal sealed class TradeProvider(
        Func<CCSPlayerController, string, List<ProgressTarget>?> targetOne,
        Action<string, Dictionary<string, EventValue>, List<ProgressTarget>> enqueueOne)
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

        private readonly List<KillRecord> _kills = [];
        private double _retentionSeconds = DefaultTradeSeconds;
        /// <summary>Player fields the loaded <c>player_trade</c> rules read, per trade prefix.</summary>
        private PlayerFields _attackerFields;
        private PlayerFields _victimFields;

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

        public void OnPlayerDeath(EventPlayerDeath e)
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

            bool teamKill = attacker.TeamNum == victim.TeamNum;
            KillRecord kill = new(attackerSlot, victimSlot, e.Weapon ?? "", e.Headshot, teamKill, RoundClock.NowSeconds());
            if (!TryTakeReverse(kill, out KillRecord first))
            {
                return;
            }

            // The earlier kill was the current victim's on the current attacker.
            double seconds = Math.Max(0, kill.At - first.At);
            EmitTrade(victim, attacker, first, kill, seconds);
            EmitTrade(attacker, victim, kill, first, seconds);
        }

        /// <summary>
        /// Drops expired kills, then takes the latest kill of <paramref name="kill"/>'s victim on its attacker:
        /// a trade. Without one, <paramref name="kill"/> is remembered and false returned.
        /// </summary>
        internal bool TryTakeReverse(in KillRecord kill, out KillRecord first)
        {
            PruneKills(kill.At);
            for (int i = _kills.Count - 1; i >= 0; i--)
            {
                first = _kills[i];
                if (first.AttackerSlot == kill.VictimSlot && first.VictimSlot == kill.AttackerSlot)
                {
                    _kills.RemoveAt(i);
                    return true;
                }
            }

            first = default;
            _kills.Add(kill);
            return false;
        }

        /// <summary><paramref name="mine"/> is the kill of <paramref name="player"/> on <paramref name="other"/>.</summary>
        private void EmitTrade(
            CCSPlayerController player,
            CCSPlayerController other,
            in KillRecord mine,
            in KillRecord theirs,
            double seconds)
        {
            if (targetOne(player, TradeType) is not { } targets)
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
            enqueueOne(TradeType, d, targets);
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

        /// <summary>One kill by slot; the controllers of a trade come from the event that completes it.</summary>
        internal readonly record struct KillRecord(
            int AttackerSlot,
            int VictimSlot,
            string Weapon,
            bool Headshot,
            bool TeamKill,
            double At);
    }
}
