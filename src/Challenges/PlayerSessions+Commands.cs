using ChallengesShared.Events;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
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

        [ConsoleCommand("sendtestchallengeevent", "sends a test challenge event to listening plugins for testing purposes <3")]
        [RequiresPermissions("@css/root")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!sendtestchallengeevent")]
        public void CommandTest(CCSPlayerController player, CommandInfo command)
        {
            if (_currentChallenge.Challenges.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.nochallenges"]);
                return;
            }
            // send event to other plugins
            Dictionary<string, string> data = new();
            // get first challenge and use as test data
            var challenge = _currentChallenge.Challenges.ElementAt(0);
            data.Add("title", challenge.Value.Title);
            data.Add("type", challenge.Value.Type);
            data.Add("amount", challenge.Value.Amount.ToString());
            // iterate through Data
            foreach (var kvp in challenge.Value.Data)
            {
                data.Add(kvp.Key, kvp.Value);
            }
            TriggerEvent(new PlayerCompletedChallengeEvent(player, data));
            command.ReplyToCommand(Localizer["core.event.trigger"].Value
                .Replace("{eventName}", nameof(PlayerCompletedChallengeEvent)));
        }
    }
}
