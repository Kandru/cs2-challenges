using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Challenges
{
    public static class Titles
    {
        public static string Resolve(Dictionary<string, string> titles, CCSPlayerController player) =>
            Resolve(titles, player.GetLanguage());

        public static string Resolve(Dictionary<string, string> titles, CultureInfo culture)
        {
            if (titles.Count == 0)
            {
                return string.Empty;
            }

            if (Pick(titles, culture.Name) is { } regional)
            {
                return regional;
            }

            if (Pick(titles, culture.TwoLetterISOLanguageName) is { } localized)
            {
                return localized;
            }

            return titles.Values.First();
        }

        public static string Expand(string title, int count, int total) =>
            title.Replace("{count}", count.ToString()).Replace("{total}", total.ToString());

        public static string For(CCSPlayerController player, Dictionary<string, string> titles, int count = 0, int total = 0) =>
            Expand(Resolve(titles, player), count, total);

        public static Dictionary<string, string> TaskOrChallenge(ChallengeDefinition challenge, ChallengeTask task) =>
            task.Title.Count > 0 ? task.Title : challenge.Title;

        private static string? Pick(Dictionary<string, string> titles, string? key) =>
            !string.IsNullOrEmpty(key)
            && titles.TryGetValue(key, out string? value)
            && !string.IsNullOrEmpty(value)
                ? value
                : null;
    }
}
