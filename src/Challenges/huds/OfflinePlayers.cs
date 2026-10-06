using Challenges.Configs;
using Challenges.Utils;

namespace Challenges.Huds
{
    /// <summary>Disk-backed player rows for the scoreboard All filter.</summary>
    internal static class OfflinePlayers
    {
        public sealed record SavedPlayer(string SteamId, string Name, PlayerState State);

        public static IEnumerable<SavedPlayer> LoadAll()
        {
            string dir = PlayerFiles.DirectoryPath(Context.Config);
            if (!Directory.Exists(dir))
            {
                yield break;
            }

            foreach (string path in Directory.EnumerateFiles(dir, "*.json"))
            {
                if (TryLoad(path) is { } entry)
                {
                    yield return entry;
                }
            }
        }

        private static SavedPlayer? TryLoad(string path)
        {
            PlayerState? loaded = PlayerFiles.TryRead(path);
            if (loaded == null)
            {
                return null;
            }

            string steamId = string.IsNullOrEmpty(loaded.SteamId)
                ? Path.GetFileNameWithoutExtension(path)
                : loaded.SteamId;
            string name = string.IsNullOrEmpty(loaded.Username) ? steamId : loaded.Username;
            return new SavedPlayer(steamId, name, loaded);
        }
    }
}
