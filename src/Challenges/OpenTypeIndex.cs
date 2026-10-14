namespace Challenges
{
    /// <summary>
    /// Which challenge types still have a task a player has not completed, cached on the player
    /// (<see cref="PlayerState.OpenTypes"/>) and recomputed lazily after progress changed. Type ids are the
    /// indexes of the task lists given to <see cref="Rebuild"/>. Game thread only.
    /// </summary>
    internal sealed class OpenTypeIndex
    {
        /// <summary>Source of <see cref="_layout"/>; unique across instances.</summary>
        private static int _nextLayout;

        private List<(ChallengeDefinition Challenge, ChallengeTask Task)>[] _types = [];
        /// <summary>Identifies the current type id assignment for <see cref="PlayerState.OpenTypes"/>.</summary>
        private int _layout;
        private string _scheduleKey = string.Empty;

        /// <summary>New type id assignment: entry i holds the tasks of type id i in <paramref name="scheduleKey"/>.</summary>
        public void Rebuild(List<(ChallengeDefinition Challenge, ChallengeTask Task)>[] types, string scheduleKey)
        {
            _types = types;
            _layout = Interlocked.Increment(ref _nextLayout);
            _scheduleKey = scheduleKey;
        }

        public void Clear() => _types = [];

        /// <summary>
        /// False when every task of type <paramref name="typeId"/> is complete for <paramref name="state"/>.
        /// Locked or cooling-down tasks count as open. Recomputed lazily after progress changed.
        /// </summary>
        public bool IsOpen(PlayerState state, int typeId)
        {
            bool[]? open = state.OpenTypes;
            if (open == null
                || state.OpenTypesDirty
                || state.OpenTypesLayout != _layout
                || state.OpenTypesVersion != state.ProgressVersion)
            {
                open = Recompute(state);
            }

            return open[typeId];
        }

        private bool[] Recompute(PlayerState state)
        {
            bool[] open = state.OpenTypes is { } existing && existing.Length == _types.Length
                ? existing
                : new bool[_types.Length];
            for (int id = 0; id < _types.Length; id++)
            {
                bool any = false;
                foreach ((ChallengeDefinition challenge, ChallengeTask task) in _types[id])
                {
                    if (!ChallengeProgress.IsTaskComplete(state, _scheduleKey, challenge.Id, task))
                    {
                        any = true;
                        break;
                    }
                }

                open[id] = any;
            }

            state.OpenTypes = open;
            state.OpenTypesLayout = _layout;
            state.OpenTypesVersion = state.ProgressVersion;
            state.OpenTypesDirty = false;
            return open;
        }
    }
}
