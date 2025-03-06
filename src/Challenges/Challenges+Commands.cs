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
                || player.PlayerPawn.Value == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
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
            // get first challenge and use as test data
            var challenge = _currentChallenge.Challenges.ElementAt(0);
            var eventData = new Dictionary<string, Dictionary<string, string>>
            {
                ["info"] = new Dictionary<string, string>
                {
                    { "title", challenge.Value.Title },
                    { "type", challenge.Value.Type },
                    { "current_amount", _playerConfigs[player.NetworkIDString].Challenges[challenge.Key].Amount.ToString() },
                    { "total_amount", challenge.Value.Amount.ToString() }
                }
            };
            // iterate through Data
            foreach (var kvp2 in challenge.Value.Data)
            {
                eventData.Add(kvp2.Key, kvp2.Value);
            }
            TriggerEvent(new PlayerCompletedChallengeEvent(player, eventData));
            command.ReplyToCommand(Localizer["core.event.trigger"].Value
                .Replace("{eventName}", nameof(PlayerCompletedChallengeEvent)));
        }
    }
}
