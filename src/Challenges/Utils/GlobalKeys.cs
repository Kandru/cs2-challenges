namespace Challenges.Utils
{
    /// <summary>
    /// Keys the engine merges into event data, each only while a loaded rule reads it
    /// (see <c>documentation/rules/GlobalEventData.md</c>).
    /// </summary>
    internal static class GlobalKeys
    {
        public const string IsWarmup = "global.iswarmup";
        public const string IsDuringRound = "global.isduringround";
        public const string MapName = "global.mapname";
        public const string HasHostages = "global.hashostages";

        public static readonly string[] All = [IsWarmup, IsDuringRound, MapName, HasHostages];
    }
}
