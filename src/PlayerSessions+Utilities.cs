using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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

        private void SendGlobalChatMessage(string message, float delay = 0, CCSPlayerController? player = null)
        {
            DebugPrint(message);
            foreach (CCSPlayerController entry in Utilities.GetPlayers())
            {
                if (entry == null || !entry.IsValid || entry.IsBot || entry == player) continue;
                if (delay > 0)
                    AddTimer(delay, () =>
                    {
                        if (entry == null || !entry.IsValid) return;
                        entry.PrintToChat(message);
                    });
                else
                    entry.PrintToChat(message);
            }
        }
    }
}