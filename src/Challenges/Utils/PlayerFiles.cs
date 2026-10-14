using System.Text.Json;
using CounterStrikeSharp.API.Modules.Extensions;

namespace Challenges
{
    /// <summary>On-disk player JSON paths and (de)serialization.</summary>
    internal static class PlayerFiles
    {
        public static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static string DirectoryPath(PluginConfig config) =>
            Path.Combine(Path.GetDirectoryName(config.GetConfigPath()) ?? ".", "players");

        /// <summary>Target path. Does not create directories.</summary>
        public static string FilePath(PluginConfig config, string steamId) =>
            FilePath(DirectoryPath(config), steamId);

        /// <summary>Target path inside an already resolved <see cref="DirectoryPath"/>. Does not create directories.</summary>
        public static string FilePath(string directory, string steamId) =>
            Path.Combine(directory, $"{SafeFileName(steamId)}.json");

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

        public static byte[] Serialize(PlayerState state) =>
            JsonSerializer.SerializeToUtf8Bytes(state, JsonOptions);

        /// <summary>Atomic write via temp file + replace.</summary>
        public static void Write(string path, PlayerState state) => WriteBytes(path, Serialize(state));

        /// <summary>
        /// Atomic write of already serialized bytes via temp file + replace. Safe off the game thread.
        /// A positive <paramref name="version"/> gives the temp file a unique name, so a background write never
        /// shares its temp file with another write of the same path.
        /// </summary>
        public static void WriteBytes(string path, byte[] bytes, long version = 0) =>
            Commit(WriteTemp(path, bytes, version), path);

        /// <summary>
        /// First step of <see cref="WriteBytes"/>: <paramref name="bytes"/> in the temp file next to
        /// <paramref name="path"/>, whose path is returned. Nothing is left behind on failure.
        /// </summary>
        public static string WriteTemp(string path, byte[] bytes, long version)
        {
            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string temp = version > 0 ? $"{path}.{version}.tmp" : path + ".tmp";
            try
            {
                File.WriteAllBytes(temp, bytes);
                return temp;
            }
            catch
            {
                Discard(temp);
                throw;
            }
        }

        /// <summary>Second step of <see cref="WriteBytes"/>: replaces <paramref name="path"/> with the temp file.</summary>
        public static void Commit(string temp, string path)
        {
            try
            {
                File.Move(temp, path, overwrite: true);
            }
            catch
            {
                Discard(temp);
                throw;
            }
        }

        /// <summary>Deletes a temp file of <see cref="WriteTemp"/>; never throws.</summary>
        public static void Discard(string temp)
        {
            try
            {
                File.Delete(temp);
            }
            catch
            {
            }
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
