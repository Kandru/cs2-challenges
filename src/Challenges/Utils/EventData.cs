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
        /// (from <see cref="PlayerKeys.For"/>). Nothing is read when no field is requested.
        /// </summary>
        public static void FillPlayer(
            Dictionary<string, string> d,
            CCSPlayerController? player,
            string[] keys,
            PlayerFields fields) =>
            FillPlayer(d, player, keys, fields, keys, PlayerFields.None);

        /// <summary>
        /// Same controller under two prefixes (e.g. <c>userid</c> / <c>victim</c>): every property is read
        /// once and written to each prefix that requests it.
        /// </summary>
        public static void FillPlayer(
            Dictionary<string, string> d,
            CCSPlayerController? player,
            string[] keysA,
            PlayerFields fieldsA,
            string[] keysB,
            PlayerFields fieldsB)
        {
            PlayerFields all = fieldsA | fieldsB;
            if (all == PlayerFields.None || player == null || !player.IsValid)
            {
                return;
            }

            CSMatchStats_t? stats = (all & PlayerFields.Stats) != 0
                ? player.ActionTrackingServices?.MatchStats
                : null;
            for (int i = 0; i < PlayerKeys.Count; i++)
            {
                PlayerFields bit = (PlayerFields)(1 << i);
                if ((all & bit) == 0)
                {
                    continue;
                }

                string value = Read(player, bit, stats);
                if ((fieldsA & bit) != 0)
                {
                    d[keysA[i]] = value;
                }

                if ((fieldsB & bit) != 0)
                {
                    d[keysB[i]] = value;
                }
            }
        }

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
