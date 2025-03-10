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
            if (_currentSchedule.Challenges.Count == 0)
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
            if (_currentSchedule.Challenges.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.nochallenges"]);
                return;
            }
            // send event to other plugins
            // get first challenge and use as test data
            var challenge = _currentSchedule.Challenges.ElementAt(0);
            var eventData = new Dictionary<string, Dictionary<string, string>>
            {
                ["info"] = new Dictionary<string, string>
                {
                    { "title", GetChallengeTitle(challenge.Value, player) },
                    { "type", challenge.Value.Type },
                    { "current_amount", _playerConfigs.ContainsKey(player.NetworkIDString)
                        && _playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                        && _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(challenge.Key) ? _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][challenge.Key].Amount.ToString() : "0" },
                    { "total_amount", challenge.Value.Amount.ToString() }
                }
            };
            // iterate through Data
            foreach (var kvp2 in challenge.Value.Data)
            {
                eventData.Add(kvp2.Key, kvp2.Value);
            }
            if (player.UserId == null) return;
            TriggerEvent(new PlayerCompletedChallengeEvent((int)player.UserId, eventData));
            command.ReplyToCommand(Localizer["core.event.trigger"].Value
                .Replace("{eventName}", nameof(PlayerCompletedChallengeEvent)));
        }
    }
}
