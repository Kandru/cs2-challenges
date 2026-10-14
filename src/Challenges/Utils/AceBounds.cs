namespace Challenges.Utils
{
    /// <summary>Defaults and data-key helpers for the <c>isace</c> situation rule.</summary>
    internal static class AceBounds
    {
        public const int DefaultMin = 2;
        public const int DefaultMax = 5;
        /// <summary>Rule key of the ace situation.</summary>
        public const string RuleKey = "isace";
        /// <summary>Challenge type an <c>isace</c> rule requires; it fails for any other type.</summary>
        public const string RequiredType = "player_kill";

        public static (int Min, int Max) Resolve(ChallengeRule rule, bool warn = false)
        {
            int min = rule.MinPlayers is int parsedMin && parsedMin > 0 ? parsedMin : DefaultMin;
            int max = rule.MaxPlayers is int parsedMax && parsedMax > 0 ? parsedMax : DefaultMax;
            if (max >= min)
            {
                return (min, max);
            }

            if (warn)
            {
                Console.WriteLine(
                    $"[Challenges] isace max_players ({max}) < min_players ({min}); using max_players={min}");
            }

            return (min, min);
        }

        public static string DataKey(int min, int max) => $"isace.{min}.{max}";

        public static string DataKey(ChallengeRule rule)
        {
            (int min, int max) = Resolve(rule);
            return DataKey(min, max);
        }
    }
}
