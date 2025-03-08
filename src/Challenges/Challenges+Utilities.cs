using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private void DebugPrint(string message)
        {
            if (Config.Debug)
            {
                Console.WriteLine(Localizer["core.debugprint"].Value.Replace("{message}", message));
            }
        }

        private long GetUnixTimestamp(DateTime? currentTime = null)
        {
            if (currentTime == null)
                currentTime = DateTime.UtcNow;
            return ((DateTimeOffset)currentTime).ToUnixTimeSeconds();
        }

        private static string GetChallengeTitle(ChallengesBlueprint challenge, CCSPlayerController player)
        {
            return challenge.Title.TryGetValue(PlayerLanguageExtensions.GetLanguage(player).TwoLetterISOLanguageName, out var userTitle)
                ? userTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value);
        }

        private static string GetScheduleTitle(RunningChallengeSchedule challenge, CCSPlayerController player)
        {
            return challenge.Title.TryGetValue(PlayerLanguageExtensions.GetLanguage(player).TwoLetterISOLanguageName, out var userTitle)
                ? userTitle
                : (challenge.Title.TryGetValue(CoreConfig.ServerLanguage, out var serverTitle)
                    ? serverTitle
                    : challenge.Title.First().Value);
        }
    }
}