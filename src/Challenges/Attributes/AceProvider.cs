using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Utils;
using Challenges.Utils;

namespace Challenges.Attributes
{
    /// <summary>
    /// Kills per attacker slot this round; writes <c>isace.{min}.{max}</c> on <c>player_death</c> for every
    /// loaded <c>isace</c> bound pair. Each attacker is credited at most once per pair and round.
    /// </summary>
    internal sealed class AceProvider(RoundRoster roster) : IAttributeProvider
    {
        public static readonly AttributeEventKey[] Keys =
        [
            new AttributeEventKey("EventPlayerDeath", "isace", "bool"),
        ];

        private static readonly string[] Watched = ["EventPlayerDeath"];

        private readonly List<AceBound> _bounds = [];
        private readonly int[] _kills = new int[RoundRoster.MaxSlots];
        private readonly HashSet<(int Slot, int Min, int Max)> _credited = [];
        /// <summary>Bounds the current death completes, indexed like <see cref="_bounds"/>.</summary>
        private bool[] _hits = [];
        /// <summary>Smallest loaded <c>min_players</c>; fewer kills can never complete a bound.</summary>
        private int _minKills;

        public IReadOnlyList<AttributeEventKey> EventKeys => Keys;
        public IReadOnlyCollection<string> WatchedEvents => Watched;

        public bool Configure(IReadOnlyList<ChallengeTask> tasks)
        {
            HashSet<(int Min, int Max)> seen = [];
            _bounds.Clear();
            foreach (ChallengeTask task in tasks)
            {
                foreach (CompiledRule rule in task.CompiledRules)
                {
                    if (!string.Equals(rule.Key, AceBounds.RuleKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    (int min, int max) = AceBounds.Resolve(rule.Source, warn: true);
                    if (seen.Add((min, max)))
                    {
                        _bounds.Add(new AceBound(min, max, AceBounds.DataKey(min, max)));
                    }
                }
            }

            _minKills = 0;
            foreach (AceBound bound in _bounds)
            {
                if (_minKills == 0 || bound.Min < _minKills)
                {
                    _minKills = bound.Min;
                }
            }

            _hits = new bool[_bounds.Count];
            return _bounds.Count > 0;
        }

        public void Observe(string eventName, GameEvent gameEvent)
        {
            if (eventName == "EventPlayerDeath")
            {
                OnDeath((EventPlayerDeath)gameEvent);
            }
        }

        public void WriteEvent(string eventName, GameEvent gameEvent, Dictionary<string, EventValue> d, uint keys)
        {
            for (int i = 0; i < _bounds.Count; i++)
            {
                d[_bounds[i].Key] = EventValue.Of(_hits[i]);
            }
        }

        public void Clear()
        {
            Array.Clear(_kills);
            _credited.Clear();
            Array.Clear(_hits);
        }

        public void ForgetSlot(int slot)
        {
            if ((uint)slot < (uint)_kills.Length)
            {
                _kills[slot] = 0;
            }

            _credited.RemoveWhere(entry => entry.Slot == slot);
        }

        private void OnDeath(EventPlayerDeath e)
        {
            Array.Clear(_hits);

            CCSPlayerController? attacker = e.Attacker;
            CCSPlayerController? victim = e.Userid;
            if (!Players.IsHumanViewer(attacker) || !Players.IsHumanViewer(victim))
            {
                return;
            }

            int slot = attacker.Slot;
            int victimSlot = victim.Slot;
            if (slot == victimSlot || (uint)slot >= (uint)_kills.Length)
            {
                return;
            }

            int enemyTeam = victim.TeamNum;
            if (attacker.TeamNum == enemyTeam
                || enemyTeam is not ((int)CsTeam.Terrorist or (int)CsTeam.CounterTerrorist))
            {
                return;
            }

            int kills = ++_kills[slot];
            if (kills < _minKills)
            {
                return;
            }

            int enemies = CountEnemyHumans(victimSlot, enemyTeam);
            for (int i = 0; i < _bounds.Count; i++)
            {
                AceBound bound = _bounds[i];
                if (enemies < bound.Min || kills != Math.Min(enemies, bound.Max))
                {
                    continue;
                }

                if (!_credited.Add((slot, bound.Min, bound.Max)))
                {
                    continue;
                }

                _hits[i] = true;
            }
        }

        /// <summary>Humans in the victim's team, dead or alive; the victim counts exactly once.</summary>
        private int CountEnemyHumans(int victimSlot, int team)
        {
            int count = roster.HumansInTeam(team);
            return roster.IsHumanInTeam(victimSlot, team) ? count : count + 1;
        }

        private readonly record struct AceBound(int Min, int Max, string Key);
    }
}
