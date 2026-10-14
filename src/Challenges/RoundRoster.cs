using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    /// <summary>
    /// Team membership and human flag per player slot, with per-team human counters.
    /// Built with one player scan per round (or lazily on first read) and kept current from
    /// <c>player_team</c> and <c>player_disconnect</c>. Game thread only.
    /// </summary>
    internal sealed class RoundRoster
    {
        public const int MaxSlots = 65;
        public const int TeamCount = 4;

        private readonly byte[] _team = new byte[MaxSlots];
        private readonly bool[] _human = new bool[MaxSlots];
        private readonly bool[] _present = new bool[MaxSlots];
        private readonly int[] _humans = new int[TeamCount];

        private bool _built;

        /// <summary>Re-reads every connected player. One scan; used at round start.</summary>
        public void Rebuild()
        {
            Reset();
            foreach (CCSPlayerController player in Utilities.GetPlayers())
            {
                if (player.IsValid)
                {
                    Set(player.Slot, player.TeamNum, IsHuman(player));
                }
            }

            _built = true;
        }

        /// <summary>Forgets everything; the next read rebuilds.</summary>
        public void Invalidate()
        {
            Reset();
            _built = false;
        }

        /// <summary><paramref name="team"/> is the new team from the event (the controller may lag).</summary>
        public void OnTeam(CCSPlayerController? player, int team, bool disconnect)
        {
            if (!_built || player is not { IsValid: true })
            {
                return;
            }

            if (disconnect)
            {
                Remove(player.Slot);
                return;
            }

            Set(player.Slot, team, IsHuman(player));
        }

        public void Remove(int slot)
        {
            if ((uint)slot >= MaxSlots || !_present[slot])
            {
                return;
            }

            if (_human[slot])
            {
                _humans[_team[slot]]--;
            }

            _present[slot] = false;
            _human[slot] = false;
            _team[slot] = 0;
        }

        /// <summary>Human players in <paramref name="team"/>, dead or alive.</summary>
        public int HumansInTeam(int team)
        {
            EnsureBuilt();
            return (uint)team < TeamCount ? _humans[team] : 0;
        }

        public bool IsHumanInTeam(int slot, int team)
        {
            EnsureBuilt();
            return (uint)slot < MaxSlots && _present[slot] && _human[slot] && _team[slot] == team;
        }

        /// <summary>Team number of the slot, or 0 when the slot is empty.</summary>
        public int TeamOf(int slot)
        {
            EnsureBuilt();
            return (uint)slot < MaxSlots && _present[slot] ? _team[slot] : 0;
        }

        private void EnsureBuilt()
        {
            if (!_built)
            {
                Rebuild();
            }
        }

        private void Set(int slot, int team, bool human)
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
            if (human)
            {
                _humans[team]++;
            }
        }

        private void Reset()
        {
            Array.Clear(_team);
            Array.Clear(_human);
            Array.Clear(_present);
            Array.Clear(_humans);
        }

        private static bool IsHuman(CCSPlayerController player) => !player.IsBot && !player.IsHLTV;
    }
}
