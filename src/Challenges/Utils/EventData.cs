using CounterStrikeSharp.API.Core;

namespace Challenges.Utils
{
    public static class EventData
    {
        /// <summary>
        /// Writes the player properties selected by <paramref name="fields"/> under <paramref name="keys"/>
        /// (from <see cref="PlayerKeys.For"/> of the controller's canonical prefix). Nothing is read when no
        /// field is requested; every property is read at most once.
        /// </summary>
        public static void FillPlayer(
            Dictionary<string, EventValue> d,
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
            Dictionary<string, EventValue> d,
            CCSPlayerController? player,
            FillDemand need,
            int prefix) =>
            FillPlayer(d, player, need.PrefixKeys[prefix], need.Prefixes[prefix]);

        private static EventValue Read(CCSPlayerController player, PlayerFields field, CSMatchStats_t? stats) =>
            field switch
            {
                PlayerFields.Name => EventValue.Of(player.PlayerName),
                PlayerFields.IsBot => EventValue.Of(player.IsBot),
                PlayerFields.Team => EventValue.Of(player.Team.ToString()),
                PlayerFields.Alive => EventValue.Of(player.PawnIsAlive),
                PlayerFields.Ping => EventValue.Of(player.Ping),
                PlayerFields.Money => EventValue.Of(player.InGameMoneyServices is { } money ? money.Account : 0),
                PlayerFields.Score => EventValue.Of(player.Score),
                PlayerFields.StatsKills => EventValue.Of(stats != null ? stats.Kills : 0),
                PlayerFields.StatsAssists => EventValue.Of(stats != null ? stats.Assists : 0),
                PlayerFields.StatsDeaths => EventValue.Of(stats != null ? stats.Deaths : 0),
                PlayerFields.StatsDamage => EventValue.Of(stats != null ? stats.Damage : 0),
                PlayerFields.Health => EventValue.Of(player.PawnHealth),
                PlayerFields.Armor => EventValue.Of(player.PawnArmor),
                PlayerFields.HasDefusor => EventValue.Of(player.PawnHasDefuser),
                PlayerFields.HasHelmet => EventValue.Of(player.PawnHasHelmet),
                _ => EventValue.Of(string.Empty),
            };
    }
}
