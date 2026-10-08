using System.Text.Json;
using Challenges.Configs;
using CounterStrikeSharp.API.Modules.Extensions;

namespace Challenges.Utils
{
    /// <summary>On-disk player JSON paths and (de)serialization.</summary>
    internal static class PlayerFiles
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string DirectoryPath(PluginConfig config) =>
            Path.Combine(Path.GetDirectoryName(config.GetConfigPath()) ?? ".", "players");

        /// <summary>Target path. Does not create directories.</summary>
        public static string FilePath(PluginConfig config, string steamId) =>
            Path.Combine(DirectoryPath(config), $"{SafeFileName(steamId)}.json");

        private static string SafeFileName(string steamId) =>
            string.Concat(steamId.Split(Path.GetInvalidFileNameChars()));

        public static PlayerState? DeserializeBytes(ReadOnlySpan<byte> bytes)
        {
            PlayerState? state = JsonSerializer.Deserialize<PlayerState>(bytes, JsonOptions);
            if (state == null)
            {
                return null;
            }

            state.Challenges ??= new Dictionary<string, Dictionary<string, Dictionary<string, TaskProgress>>>();
            state.Statistics ??= new PlayerStatistics();
            state.Username ??= string.Empty;
            state.SteamId ??= string.Empty;
            state.Language ??= string.Empty;
            return state;
        }

        /// <summary>Atomic write via temp file + replace.</summary>
        public static void Write(string path, PlayerState state)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string temp = path + ".tmp";
            File.WriteAllBytes(temp, JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions));
            File.Move(temp, path, overwrite: true);
        }

        public static bool TryQuarantine(string path, out string? quarantinePath)
        {
            quarantinePath = path + ".bad";
            try
            {
                if (File.Exists(quarantinePath))
                {
                    quarantinePath = $"{path}.bad.{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                }

                File.Move(path, quarantinePath, overwrite: false);
                return true;
            }
            catch
            {
                quarantinePath = null;
                return false;
            }
        }
    }
}
