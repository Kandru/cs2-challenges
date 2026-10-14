using CounterStrikeSharp.API.Core;

namespace Challenges.Extractors
{
    /// <summary>
    /// Controllers of the current game event by player prefix index, so <see cref="IExtractor.Targets"/>,
    /// <see cref="IExtractor.Fill"/> and attribute writes share one native read each. Reused by the engine,
    /// cleared after every extractor call. Game thread only, not reentrant.
    /// </summary>
    public sealed class EventPlayers
    {
        private readonly CCSPlayerController?[] _players;
        private readonly bool[] _read;

        /// <summary>Sized for the extractor with the most player prefixes.</summary>
        public EventPlayers(IReadOnlyList<IExtractor> extractors)
        {
            int capacity = 0;
            foreach (IExtractor extractor in extractors)
            {
                capacity = Math.Max(capacity, extractor.PlayerPrefixes.Count);
            }

            _players = new CCSPlayerController?[capacity];
            _read = new bool[capacity];
        }

        /// <summary>True when prefix <paramref name="prefixIndex"/> was read since the last <see cref="Clear"/>.</summary>
        public bool TryGet(int prefixIndex, out CCSPlayerController? player)
        {
            if ((uint)prefixIndex < (uint)_read.Length && _read[prefixIndex])
            {
                player = _players[prefixIndex];
                return true;
            }

            player = null;
            return false;
        }

        /// <summary>Stores the controller read for prefix <paramref name="prefixIndex"/>, null included, and returns it.</summary>
        public CCSPlayerController? Set(int prefixIndex, CCSPlayerController? player)
        {
            if ((uint)prefixIndex < (uint)_read.Length)
            {
                _players[prefixIndex] = player;
                _read[prefixIndex] = true;
            }

            return player;
        }

        /// <summary>Forgets every controller so none outlives its event.</summary>
        public void Clear()
        {
            Array.Clear(_players);
            Array.Clear(_read);
        }
    }
}
