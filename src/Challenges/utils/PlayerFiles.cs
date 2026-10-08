using System.Text.Json;
using Challenges.Configs;
using CounterStrikeSharp.API.Modules.Extensions;

namespace Challenges.Utils
{
    /// <summary>Single place for on-disk player JSON paths and (de)serialization.</summary>
    internal static class PlayerFiles
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string DirectoryPath(PluginConfig config) =>
            Path.Combine(Path.GetDirectoryName(config.GetConfigPath()) ?? ".", "players");

        public static string FilePath(PluginConfig config, string steamId)
        {
            string dir = DirectoryPath(config);
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"{SafeFileName(steamId)}.json");
        }

        /// <summary>Existing player JSON path, or null if the file is missing (does not create the directory).</summary>
        public static string? ExistingFilePath(PluginConfig config, string steamId)
        {
            if (string.IsNullOrEmpty(steamId))
            {
                return null;
            }

            string dir = DirectoryPath(config);
            if (!Directory.Exists(dir))
            {
                return null;
            }

            string path = Path.Combine(dir, $"{SafeFileName(steamId)}.json");
            return File.Exists(path) ? path : null;
        }

        private static string SafeFileName(string steamId) =>
            string.Concat(steamId.Split(Path.GetInvalidFileNameChars()));

        public static PlayerState? TryRead(string path, Action<Exception>? onError = null)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PlayerState>(File.ReadAllText(path), JsonOptions);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
                return null;
            }
        }

        public static void Write(string path, PlayerState state) =>
            File.WriteAllText(path, JsonSerializer.Serialize(state, JsonOptions));
    }
}
