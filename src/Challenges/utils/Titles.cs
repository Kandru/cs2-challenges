using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Challenges.Utils
{
    public static class Titles
    {
        public static string Resolve(Dictionary<string, string> titles, CCSPlayerController player)
        {
            if (titles.Count == 0)
            {
                return string.Empty;
            }

            string lang = player.IsBot
                ? string.Empty
                : PlayerLanguageExtensions.GetLanguage(player).TwoLetterISOLanguageName;

            if (!string.IsNullOrEmpty(lang)
                && titles.TryGetValue(lang, out string? localized)
                && !string.IsNullOrEmpty(localized))
            {
                return localized;
            }

            return titles.Values.First();
        }

        public static string Expand(string title, int count, int total) =>
            title.Replace("{count}", count.ToString()).Replace("{total}", total.ToString());
    }
}
