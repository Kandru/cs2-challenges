namespace Challenges.Attributes
{
    /// <summary>Weapon name helpers shared by providers.</summary>
    internal static class Weapons
    {
        /// <summary>Name without the <c>weapon_</c> prefix; empty for null. Constant strings for grenades.</summary>
        public static string Short(string? weapon) =>
            weapon switch
            {
                null or "" => "",
                "weapon_hegrenade" => "hegrenade",
                "weapon_flashbang" => "flashbang",
                "weapon_smokegrenade" => "smokegrenade",
                "weapon_molotov" => "molotov",
                "weapon_incgrenade" => "incgrenade",
                "weapon_decoy" => "decoy",
                _ => weapon.StartsWith("weapon_", StringComparison.OrdinalIgnoreCase) ? weapon[7..] : weapon,
            };

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
