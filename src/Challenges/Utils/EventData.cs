using System.Globalization;
using CounterStrikeSharp.API.Core;

namespace Challenges.Utils
{
    public static class EventData
    {
        /// <summary>True when any loaded task rule uses a <c>*.isbot</c> key. Written by ChallengeEngine.</summary>
        public static bool IncludeIsBot;

        public static string Bool(bool value) => value ? "true" : "false";

        public static string Num<T>(T value) where T : IFormattable =>
            value.ToString(null, CultureInfo.InvariantCulture);

        public static void FillPlayer(Dictionary<string, string> d, CCSPlayerController? player, string prefix)
        {
            if (player == null || !player.IsValid)
            {
                return;
            }

            var stats = player.ActionTrackingServices?.MatchStats;
            d[$"{prefix}.name"] = player.PlayerName;
            if (IncludeIsBot)
            {
                d[$"{prefix}.isbot"] = Bool(player.IsBot);
            }

            d[$"{prefix}.team"] = player.Team.ToString();
            d[$"{prefix}.alive"] = Bool(player.PawnIsAlive);
            d[$"{prefix}.ping"] = Num(player.Ping);
            d[$"{prefix}.money"] = player.InGameMoneyServices is { } money ? Num(money.Account) : "0";
            d[$"{prefix}.score"] = Num(player.Score);
            d[$"{prefix}.stats.kills"] = stats != null ? Num(stats.Kills) : "0";
            d[$"{prefix}.stats.assists"] = stats != null ? Num(stats.Assists) : "0";
            d[$"{prefix}.stats.deaths"] = stats != null ? Num(stats.Deaths) : "0";
            d[$"{prefix}.stats.damage"] = stats != null ? Num(stats.Damage) : "0";
            d[$"{prefix}.health"] = Num(player.PawnHealth);
            d[$"{prefix}.armor"] = Num(player.PawnArmor);
            d[$"{prefix}.hasdefusor"] = Bool(player.PawnHasDefuser);
            d[$"{prefix}.hashelmet"] = Bool(player.PawnHasHelmet);
        }
    }
}
