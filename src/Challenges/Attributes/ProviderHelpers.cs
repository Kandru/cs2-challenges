namespace Challenges.Attributes
{
    /// <summary>Weapon name helpers shared by providers.</summary>
    internal static class Weapons
    {
        /// <summary>Name without the <c>weapon_</c> prefix; empty for null.</summary>
        public static string Short(string? weapon)
        {
            if (string.IsNullOrEmpty(weapon))
            {
                return "";
            }

            return weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase)
                ? weapon[7..]
                : weapon;
        }

        /// <summary>Same short name, or both knives.</summary>
        public static bool Same(string left, string right) =>
            string.Equals(left, right, StringComparison.OrdinalIgnoreCase)
            || (left.Contains("knife", StringComparison.OrdinalIgnoreCase)
                && right.Contains("knife", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Wall clock in seconds for round-scoped expiry windows.</summary>
    internal static class RoundClock
    {
        public static double NowSeconds() =>
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 0.001;
    }
}
