using System.Text.Json;
using CounterStrikeSharp.API.Modules.Extensions;
using Challenges.Configs;

namespace Challenges.Huds
{
    /// <summary>Disk-backed player rows for the scoreboard All filter.</summary>
    internal static class OfflinePlayers
    {
        private static readonly JsonSerializerOptions JsonOptions = new();

        public sealed record SavedPlayer(string SteamId, string Name, PlayerState State);

        public static IEnumerable<SavedPlayer> LoadAll()
        {
            string dir = Path.Combine(
                Path.GetDirectoryName(Context.Config.GetConfigPath()) ?? ".",
                "players");
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
            try
            {
                PlayerState? loaded = JsonSerializer.Deserialize<PlayerState>(File.ReadAllText(path), JsonOptions);
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
            catch
            {
                return null;
            }
        }
    }
}
