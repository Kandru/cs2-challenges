using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        [ConsoleCommand("challenges", "toggle your challenge overview")]
        [ConsoleCommand("c", "toggle your challenge overview")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!c")]
        public void CommandShowChallenges(CCSPlayerController player, CommandInfo command)
        {
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.PlayerPawn == null
                || !player.PlayerPawn.IsValid
                || player.PlayerPawn.Value == null) return;
            if (_currentChallenge.Challenges.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.nochallenges"]);
                return;
            }
            if (player.PlayerPawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
                if (_playerHudPersonalChallenges.ContainsKey(player.NetworkIDString))
                {
                    command.ReplyToCommand(Localizer["command.hidegui"]);
                    HideGui(player);
                    // save chosen user setting
                    _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways = false;
                }
                else
                {
                    command.ReplyToCommand(Localizer["command.showgui"]);
                    ShowGui(player, 0);
                    // save chosen user setting
                    _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways = true;
                }
            else
                command.ReplyToCommand(Localizer["command.notalive"]);
        }
    }
}
