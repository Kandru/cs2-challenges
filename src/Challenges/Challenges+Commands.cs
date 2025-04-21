using ChallengesShared.Events;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Extensions;

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
                || player.Pawn == null
                || !player.Pawn.IsValid
                || player.Pawn.Value == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
            // get all challenges that can be completed by this player
            var challenges = _currentSchedule.Challenges
                .Where(kvp => CanChallengeBeCompleted(kvp.Value, player.NetworkIDString)
                    && IsChallengeAllowedOnThisMap(kvp.Value)
                    && kvp.Value.Visible)
                .ToList();
            if (challenges.Count == 0)
            {
                command.ReplyToCommand(Localizer["command.nochallenges"]);
                return;
            }
            if (player.Pawn.Value.LifeState == (byte)LifeState_t.LIFE_ALIVE)
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
            {
                var message = BuildGuiMessage(player);
                if (message != null)
                {
                    // print to center of screen (maybe gets overwritten by some other event)
                    player.PrintToCenterHtml(message.Replace("\n", "<br>"));
                    // print to chat as fallback
                    foreach (string line in message.Split("\n"))
                        command.ReplyToCommand(line);
                }
                else
                    command.ReplyToCommand(Localizer["command.nochallenges"]);
            }
        }

        [ConsoleCommand("topc", "players with most challenges solved")]
        [CommandHelper(whoCanExecute: CommandUsage.CLIENT_ONLY, minArgs: 0, usage: "!topc")]
        public void CommandShowTopPlayers(CCSPlayerController player, CommandInfo command)
        {
            if (player == null
                || !player.IsValid) return;
            if (_playersWithMostChallengesSolved.Count == 0)
            {
                foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
                {
                    entry.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, entry, "command.topc.nodata"));
                }
                return;
            }
            foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
            {
                entry.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, entry, "command.topc"));
                // get the top 5 of players with most challenges solved
                for (int i = 0; i < 5 && i < _playersWithMostChallengesSolved.Count; i++)
                {
                    entry.PrintToChat($"{i + 1}. {_playersWithMostChallengesSolved[i].Username} ({_playersWithMostChallengesSolved[i].Statistics.AmountChallengesSolved})");
                }
            }
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

        [ConsoleCommand("challenges", "Challenges admin commands")]
        [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY, minArgs: 1, usage: "<command>")]
        public void CommandMapVote(CCSPlayerController player, CommandInfo command)
        {
            string subCommand = command.GetArg(1);
            switch (subCommand.ToLower())
            {
                case "reload":
                    Config.Reload();
                    command.ReplyToCommand(Localizer["admin.reload"]);
                    break;
                case "disable":
                    Config.Enabled = false;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.disable"]);
                    break;
                case "enable":
                    Config.Enabled = true;
                    Config.Update();
                    command.ReplyToCommand(Localizer["admin.enable"]);
                    break;
                default:
                    command.ReplyToCommand(Localizer["admin.unknown_command"].Value
                        .Replace("{command}", subCommand));
                    break;
            }
        }
    }
}
