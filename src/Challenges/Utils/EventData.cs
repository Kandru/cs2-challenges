using System.Globalization;
using CounterStrikeSharp.API.Core;

namespace Challenges.Utils
{
    public static class EventData
    {
        public static string Bool(bool value) => value ? "true" : "false";

        public static string Num<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        /// <summary>
        /// Writes the player properties selected by <paramref name="fields"/> under <paramref name="keys"/>
        /// (from <see cref="PlayerKeys.For"/> of the controller's canonical prefix). Nothing is read when no
        /// field is requested; every property is read at most once.
        /// </summary>
        public static void FillPlayer(
            Dictionary<string, string> d,
            CCSPlayerController? player,
            string[] keys,
            PlayerFields fields)
        {
            if (fields == PlayerFields.None || player == null || !player.IsValid)
            {
                return;
            }

            CSMatchStats_t? stats = (fields & PlayerFields.Stats) != 0
                ? player.ActionTrackingServices?.MatchStats
                : null;
            for (int i = 0; i < PlayerKeys.Count; i++)
            {
                PlayerFields bit = (PlayerFields)(1 << i);
                if ((fields & bit) != 0)
                {
                    d[keys[i]] = Read(player, bit, stats);
                }
            }
        }

        /// <summary>Generated extractors: fills player prefix <paramref name="prefix"/> of <paramref name="need"/>.</summary>
        public static void FillPlayer(
            Dictionary<string, string> d,
            CCSPlayerController? player,
            FillDemand need,
            int prefix) =>
            FillPlayer(d, player, need.PrefixKeys[prefix], need.Prefixes[prefix]);

        private static string Read(CCSPlayerController player, PlayerFields field, CSMatchStats_t? stats) =>
            field switch
            {
                PlayerFields.Name => player.PlayerName,
                PlayerFields.IsBot => Bool(player.IsBot),
                PlayerFields.Team => player.Team.ToString(),
                PlayerFields.Alive => Bool(player.PawnIsAlive),
                PlayerFields.Ping => Num(player.Ping),
                PlayerFields.Money => player.InGameMoneyServices is { } money ? Num(money.Account) : "0",
                PlayerFields.Score => Num(player.Score),
                PlayerFields.StatsKills => stats != null ? Num(stats.Kills) : "0",
                PlayerFields.StatsAssists => stats != null ? Num(stats.Assists) : "0",
                PlayerFields.StatsDeaths => stats != null ? Num(stats.Deaths) : "0",
                PlayerFields.StatsDamage => stats != null ? Num(stats.Damage) : "0",
                PlayerFields.Health => Num(player.PawnHealth),
                PlayerFields.Armor => Num(player.PawnArmor),
                PlayerFields.HasDefusor => Bool(player.PawnHasDefuser),
                PlayerFields.HasHelmet => Bool(player.PawnHasHelmet),
                _ => string.Empty,
            };
    }
}
