using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;


namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private void OnCompletionAction(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            // check if we have action data
            if (challenge.Actions.Count == 0) return;
            // iterate through possible actions
            foreach (var kvp in challenge.Actions)
            {
                switch (kvp.Key)
                {
                    case "challenge.delete.progress" when kvp.Values.Count == 1:
                        ActionChallengeDelete(player, challenge, kvp.Values[0]);
                        break;
                    case "challenge.delete.completed" when kvp.Values.Count == 1:
                        ActionChallengeDelete(player, challenge, kvp.Values[0], true);
                        break;
                    case "challenge.mark.completed" when kvp.Values.Count == 1:
                        ActionChallengeMarkCompleted(player, kvp.Values[0]);
                        break;
                    case "notify.player.rule_broken":
                        ActionNotifyPlayerRuleBroken(player, challenge.Key);
                        break;
                    default:
                        DebugPrint($"Action {kvp.Key} not found for challenge {challenge.Key}.");
                        break;
                }
            }
        }

        private void ActionChallengeDelete(CCSPlayerController player, ChallengesBlueprint challenge, string challengeKey, bool deleteCompleted = false)
        {
            // check if player has progress for this challenge
            if (player == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || !_currentSchedule.Challenges.ContainsKey(challengeKey)
                || !_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                || !_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(challengeKey)
                || (_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][challengeKey].Amount
                    >= _currentSchedule.Challenges[challengeKey].Amount
                    && !deleteCompleted)) return;
            // delay execution to next frame
            Server.NextFrame(() =>
            {
                // check if player and stuff still exists
                if (player == null
                    || !_playerConfigs.ContainsKey(player.NetworkIDString)
                    || !_currentSchedule.Challenges.ContainsKey(challengeKey)
                    || !_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                    || !_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(challengeKey)) return;
                // inform player only if challenge is not the same (e.g. a rule challenge has been executed)
                // this informs the player about resetting the challenge
                if (challengeKey != challenge.Key)
                    player.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, player, "challenges.deleted")
                        .Replace("{challenge}", GetChallengeTitle(_currentSchedule.Challenges[challengeKey], player)
                            .Replace("{total}", _currentSchedule.Challenges[challengeKey].Amount.ToString())
                            .Replace("{count}", _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][challengeKey].Amount.ToString())));
                // delete challenge
                _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].Remove(challengeKey);
                // redraw GUI
                float duration = _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways
                    ? 0
                    : Config.GUI.OnRoundStartDuration;
                ShowGui(player, duration);
            });
            return;
        }

        private void ActionChallengeMarkCompleted(CCSPlayerController player, string challengeKey)
        {
            // check if player and challenge exists
            if (player == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || !_currentSchedule.Challenges.ContainsKey(challengeKey)
                || !_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)) return;
            // set challenge as completed
            _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][challengeKey] = new PlayerConfigChallenges
            {
                Amount = _currentSchedule.Challenges[challengeKey].Amount,
                LastUpdate = GetUnixTimestamp()
            };
        }

        private void ActionNotifyPlayerRuleBroken(CCSPlayerController player, string challengeKey)
        {
            // check if player still exists
            if (player == null || !player.IsValid) return;
            // play sound if available
            if (Config.Notifications.ChallengeRuleBrokenSound != "")
                player.ExecuteClientCommand($"play {Config.Notifications.ChallengeRuleBrokenSound}");
            player.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, player, "challenges.rule.broken"));
            player.PrintToChat(GetChallengeTitle(_currentSchedule.Challenges[challengeKey], player));
            player.PrintToCenterAlert(GetChallengeTitle(_currentSchedule.Challenges[challengeKey], player));
        }
    }
}
