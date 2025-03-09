using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;


namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private void OnChallengeCompletion(CCSPlayerController player, ChallengesBlueprint challenge, Dictionary<string, Dictionary<string, string>> data)
        {
            // check if we have data for our own plugin
            if (!data.TryGetValue("Challenges", out var challengesData)) return;
            // notify player if challenge is a rule
            player.PrintToChat(LocalizerExtensions.ForPlayer(Localizer, player, "challenges.rule.broken"));
            player.PrintToChat(GetChallengeTitle(_currentSchedule.Challenges[challenge.Key], player));
            player.PrintToCenterAlert(GetChallengeTitle(_currentSchedule.Challenges[challenge.Key], player));
            // iterate through our data
            foreach (var kvp in challengesData)
            {
                if (kvp.Key.StartsWith("delete_progress")) OnChallengeCompletionDeleteChallengeProgress(player, challenge, kvp.Value);
                else if (kvp.Key.StartsWith("delete_completed")) OnChallengeCompletionDeleteChallengeProgress(player, challenge, kvp.Value, true);
                else if (kvp.Key.StartsWith("mark_completed")) OnChallengeCompletionCompleteChallenge(player, kvp.Value);
            }
        }

        private void OnChallengeCompletionDeleteChallengeProgress(CCSPlayerController player, ChallengesBlueprint challenge, string challengeKey, bool deleteCompleted = false)
        {
            // check if player has progress for this challenge and delete it
            if (player == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || !_currentSchedule.Challenges.ContainsKey(challengeKey)
                || !_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                || !_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(challengeKey)
                || (_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][challengeKey].Amount
                    >= _currentSchedule.Challenges[challengeKey].Amount
                    && !deleteCompleted)) return;
            Server.NextFrame(() =>
            {
                if (player == null
                    || !_playerConfigs.ContainsKey(player.NetworkIDString)
                    || !_currentSchedule.Challenges.ContainsKey(challengeKey)
                    || !_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                    || !_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(challengeKey)) return;
                // inform player only if challenge is not the same
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

        private void OnChallengeCompletionCompleteChallenge(CCSPlayerController player, string challengeKey)
        {
            // check player and challenge exist
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
    }
}
