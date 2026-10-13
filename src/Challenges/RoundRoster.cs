using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>
    /// Team membership, human flag and alive state per player slot, with per-team counters.
    /// Built with one player scan per round (or lazily on first read) and kept current from
    /// <c>player_spawn</c>, <c>player_death</c>, <c>player_team</c> and <c>player_disconnect</c>.
    /// Game thread only.
    /// </summary>
    internal sealed class RoundRoster
    {
        public const int MaxSlots = 65;
        public const int TeamCount = 4;

        private readonly byte[] _team = new byte[MaxSlots];
        private readonly bool[] _human = new bool[MaxSlots];
        private readonly bool[] _alive = new bool[MaxSlots];
        private readonly bool[] _present = new bool[MaxSlots];
        private readonly int[] _humans = new int[TeamCount];
        private readonly int[] _aliveCount = new int[TeamCount];

        public bool IsBuilt { get; private set; }

        /// <summary>Re-reads every connected player. One scan; used at round start.</summary>
        public void Rebuild()
        {
            Reset();
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    Set(player.Slot, player.TeamNum, IsHuman(player), player.PawnIsAlive);
                }
            }

            IsBuilt = true;
        }

        /// <summary>Forgets everything; the next read rebuilds.</summary>
        public void Invalidate()
        {
            Reset();
            IsBuilt = false;
        }

        public void OnSpawn(CCSPlayerController? player)
        {
            if (!IsBuilt || player is not { IsValid: true })
            {
                return;
            }

            Set(player.Slot, player.TeamNum, IsHuman(player), alive: true);
        }

        public void OnDeath(CCSPlayerController? player)
        {
            if (!IsBuilt || player is not { IsValid: true })
            {
                return;
            }

            int slot = player.Slot;
            if ((uint)slot < MaxSlots && _present[slot])
            {
                Set(slot, _team[slot], _human[slot], alive: false);
            }
        }

        /// <summary><paramref name="team"/> is the new team from the event (the controller may lag).</summary>
        public void OnTeam(CCSPlayerController? player, int team, bool disconnect)
        {
            if (!IsBuilt || player is not { IsValid: true })
            {
                return;
            }

            if (disconnect)
            {
                Remove(player.Slot);
                return;
            }

            Set(player.Slot, team, IsHuman(player), alive: false);
        }

        public void Remove(int slot)
        {
            if ((uint)slot >= MaxSlots || !_present[slot])
            {
                return;
            }

            int team = _team[slot];
            if (_human[slot])
            {
                _humans[team]--;
            }

            if (_alive[slot])
            {
                _aliveCount[team]--;
            }

            _present[slot] = false;
            _human[slot] = false;
            _alive[slot] = false;
            _team[slot] = 0;
        }

        /// <summary>Human players in <paramref name="team"/>, dead or alive.</summary>
        public int HumansInTeam(int team)
        {
            EnsureBuilt();
            return (uint)team < TeamCount ? _humans[team] : 0;
        }

        /// <summary>Living players (humans and bots) in <paramref name="team"/>.</summary>
        public int AliveInTeam(int team)
        {
            EnsureBuilt();
            return (uint)team < TeamCount ? _aliveCount[team] : 0;
        }

        public bool IsHumanInTeam(int slot, int team)
        {
            EnsureBuilt();
            return (uint)slot < MaxSlots && _present[slot] && _human[slot] && _team[slot] == team;
        }

        public bool IsAlive(int slot)
        {
            EnsureBuilt();
            return (uint)slot < MaxSlots && _present[slot] && _alive[slot];
        }

        /// <summary>Team number of the slot, or 0 when the slot is empty.</summary>
        public int TeamOf(int slot)
        {
            EnsureBuilt();
            return (uint)slot < MaxSlots && _present[slot] ? _team[slot] : 0;
        }

        private void EnsureBuilt()
        {
            if (!IsBuilt)
            {
                Rebuild();
            }
        }

        private void Set(int slot, int team, bool human, bool alive)
        {
            if ((uint)slot >= MaxSlots)
            {
                return;
            }

            Remove(slot);
            if ((uint)team >= TeamCount)
            {
                team = 0;
            }

            _present[slot] = true;
            _team[slot] = (byte)team;
            _human[slot] = human;
            _alive[slot] = alive;
            if (human)
            {
                _humans[team]++;
            }

            if (alive)
            {
                _aliveCount[team]++;
            }
        }

        private void Reset()
        {
            Array.Clear(_team);
            Array.Clear(_human);
            Array.Clear(_alive);
            Array.Clear(_present);
            Array.Clear(_humans);
            Array.Clear(_aliveCount);
        }

        private static bool IsHuman(CCSPlayerController player) => !player.IsBot && !player.IsHLTV;
    }
}
