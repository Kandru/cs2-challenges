using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private Dictionary<string, string> GetGlobalEventData()
        {
            return new Dictionary<string, string>
            {
                { "global.iswarmup", GetGameRules()?.WarmupPeriod.ToString() ?? "false" },
                { "global.isduringround", _isDuringRound.ToString() },
                { "global.mapname", Server.MapName }
            };
        }

        private static Dictionary<string, string> GetCCSPlayerControllerProperties(CCSPlayerController? player, string prefix)
        {
            if (player == null || !player.IsValid) return new Dictionary<string, string>();
            return new Dictionary<string, string>{
                { $"{prefix}.name", player.PlayerName },
                { $"{prefix}.isbot", player.IsBot.ToString() },
                { $"{prefix}.team", player.Team.ToString() },
                { $"{prefix}.alive", player.PawnIsAlive.ToString() },
                { $"{prefix}.ping", player.Ping.ToString() },
                { $"{prefix}.money", player.InGameMoneyServices?.Account.ToString() ?? "0" },
                { $"{prefix}.score", player.Score.ToString() },
                { $"{prefix}.stats.kills", player.ActionTrackingServices?.MatchStats?.Kills.ToString() ?? "0" },
                { $"{prefix}.stats.assists", player.ActionTrackingServices?.MatchStats?.Assists.ToString() ?? "0" },
                { $"{prefix}.stats.deaths", player.ActionTrackingServices?.MatchStats?.Deaths.ToString() ?? "0" },
                { $"{prefix}.stats.damage", player.ActionTrackingServices?.MatchStats?.Damage.ToString() ?? "0" },
                { $"{prefix}.health", player.PawnHealth.ToString() },
                { $"{prefix}.armor", player.PawnArmor.ToString() },
                { $"{prefix}.hasdefusor", player.PawnHasDefuser.ToString() },
                { $"{prefix}.hashelmet", player.PawnHasHelmet.ToString() }
            };
        }
    }
}