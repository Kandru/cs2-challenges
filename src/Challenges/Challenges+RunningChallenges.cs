using ChallengesShared.Events;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Utils;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private RunningChallengeSchedule _currentSchedule = new();
        private Dictionary<string, CPointWorldText> _playerHudPersonalChallenges = [];

        private void CheckForRunningSchedule()
        {
            DebugPrint("checking for running schedule");
            _currentSchedule = new RunningChallengeSchedule();
            if (_availableChallenges.Schedules.Count == 0 || _availableChallenges.Blueprints.Count == 0) return;
            foreach (var kvp in _availableChallenges.Schedules)
            {
                if (DateTime.TryParse(kvp.Value.StartDate, out DateTime startDate)
                    && DateTime.TryParse(kvp.Value.EndDate, out DateTime endDate)
                    && startDate <= DateTime.UtcNow
                    && endDate >= DateTime.UtcNow)
                {
                    DebugPrint($"found running schedule {kvp.Key}");
                    _currentSchedule.Title = kvp.Value.Title;
                    _currentSchedule.Key = kvp.Key;
                    _currentSchedule.StartDate = kvp.Value.StartDate;
                    _currentSchedule.EndDate = kvp.Value.EndDate;
                    // Generate a unique key for the schedule
                    var hashInput = $"{_currentSchedule.Title.First()}{_currentSchedule.StartDate}{_currentSchedule.EndDate}";
                    var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(hashInput));
                    _currentSchedule.Key = Convert.ToBase64String(hashBytes);
                    // Add challenges to the current schedule
                    foreach (var challenge in kvp.Value.Challenges)
                    {
                        if (_currentSchedule.Challenges.ContainsKey(challenge)) continue;

                        if (_availableChallenges.Blueprints.TryGetValue(challenge, out var blueprint))
                        {
                            DebugPrint($"found blueprint {challenge} for schedule {kvp.Key}");
                            _currentSchedule.Challenges.Add(challenge, blueprint);
                        }
                        else
                        {
                            var wildcardKeys = _availableChallenges.Blueprints.Keys.Where(k => k.StartsWith(challenge.TrimEnd('*'))).ToList();
                            foreach (var wildcardKey in wildcardKeys)
                            {
                                if (_currentSchedule.Challenges.ContainsKey(wildcardKey)) continue;
                                if (_availableChallenges.Blueprints.TryGetValue(wildcardKey, out blueprint))
                                {
                                    DebugPrint($"found wildcard blueprint {wildcardKey} for schedule {kvp.Key}");
                                    _currentSchedule.Challenges.Add(wildcardKey, blueprint);
                                }
                            }
                        }
                    }
                    // Check if the current schedule is different from the last one
                    if (_currentSchedule.Key != Config.TempData.CurrentSchedule.Key)
                    {
                        Config.TempData.CurrentSchedule.Key = _currentSchedule.Key;
                        SendDiscordMessageOnNewSchedule(_currentSchedule);
                    }
                    break;
                }
            }
        }

        private void CheckChallengeGoal(CCSPlayerController? player, string type, Dictionary<string, string> data)
        {
            // Enqueue the task to be processed
            EnqueueChallengeTask(() => ProcessCheckChallengeGoal(player, type, data));
        }

        private async Task ProcessCheckChallengeGoal(CCSPlayerController? player, string type, Dictionary<string, string> data)
        {
            // stop if not enabled
            if (!Config.Enabled || player == null || !player.IsValid)
                return;
            if (!Config.AllowBots && player.IsBot)
                return;
            string steamId = player.NetworkIDString;
            // TODO: player.UserID || player.PlayerName will crash because not in the main thread
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            // stop if player has no config
            if (!_playerConfigs.ContainsKey(steamId))
                return;
            // stop if no challenges
            if (_currentSchedule.Challenges.Count == 0)
            {
                // clear challenges for player if no schedule
                _playerConfigs[steamId].Challenges.Clear();
                return;
            }
            // remove outdated challenges
            RemoveOutdatedChallenges(player);
            DebugPrint($"CheckChallengeGoal for {steamId} -> {type}");
            // get challenges for specified type
            var challenges = _currentSchedule.Challenges.Where(x => x.Value.Type == type).ToList();
            if (challenges.Count == 0) return;
            // check for each challenge
            foreach (var kvp in challenges)
            {
                if (HasCompletedChallenge(player, kvp.Value)) continue;
                if (!CanChallengeBeCompleted(kvp.Value, steamId)) continue;
                if (HasCooldown(player, kvp.Value)) continue;
                if (!CompliesWithRules(kvp.Value, data)) continue;
                UpdatePlayerChallenges(player, kvp.Value);
                await ChallengeNotification(player, kvp.Value);
            }

            await Task.CompletedTask;
        }

        private void RemoveOutdatedChallenges(CCSPlayerController player)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            foreach (var kvp in _playerConfigs[steamId].Challenges)
            {
                if (kvp.Key == _currentSchedule.Key) continue;
                DebugPrint($"deleting outdated challenge {kvp.Key} for user {steamId}");
                _playerConfigs[steamId].Challenges.Remove(kvp.Key);
            }
        }

        private bool HasCompletedChallenge(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            if (_playerConfigs[steamId].Challenges.ContainsKey(_currentSchedule.Key)
                && _playerConfigs[steamId].Challenges[_currentSchedule.Key].ContainsKey(challenge.Key)
                && _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].Amount >= challenge.Amount)
            {
                DebugPrint($"user {steamId} has already completed challenge {challenge.Key}");
                return true;
            }
            return false;
        }

        private bool HasCooldown(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            if (_playerConfigs[steamId].Challenges.ContainsKey(_currentSchedule.Key)
                && _playerConfigs[steamId].Challenges.ContainsKey(challenge.Key)
                && _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].LastUpdate + challenge.Cooldown > GetUnixTimestamp())
            {
                DebugPrint($"user {steamId} has cooldown for challenge {challenge.Key}");
                return true;
            }
            return false;
        }

        private bool CompliesWithRules(ChallengesBlueprint challenge, Dictionary<string, string> data)
        {
            foreach (var rule in challenge.Rules)
            {
                if (!data.ContainsKey(rule.Key.ToLower()))
                {
                    DebugPrint($"rule {rule.Key} not found in data for type {challenge.Type}");
                    return false;
                }

                var currentValue = data[rule.Key.ToLower()];
                var targetValue = rule.Value.ToLower();
                DebugPrint($"checking rule {rule.Key} {rule.Operator} {targetValue} against {currentValue}");

                if (!EvaluateRule(rule, currentValue, targetValue))
                    return false;
            }
            return true;
        }

        private bool EvaluateRule(ChallengesBlueprintRules rule, string currentValue, string targetValue)
        {
            switch (rule.Operator)
            {
                case "==": return currentValue == targetValue;
                case "!=": return currentValue != targetValue;
                case ">": return float.Parse(currentValue) > float.Parse(targetValue);
                case "<": return float.Parse(currentValue) < float.Parse(targetValue);
                case ">=": return float.Parse(currentValue) >= float.Parse(targetValue);
                case "<=": return float.Parse(currentValue) <= float.Parse(targetValue);
                case "bool==": return bool.Parse(currentValue) == bool.Parse(targetValue);
                case "bool!=": return bool.Parse(currentValue) != bool.Parse(targetValue);
                case "contains": return currentValue.Contains(targetValue);
                case "!contains": return !currentValue.Contains(targetValue);
                default:
                    DebugPrint($"unknown operator {rule.Operator}");
                    return false;
            }
        }

        private void UpdatePlayerChallenges(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            // add schedule to player config if not exists
            if (!_playerConfigs[steamId].Challenges.ContainsKey(_currentSchedule.Key))
                _playerConfigs[steamId].Challenges.Add(_currentSchedule.Key, new Dictionary<string, PlayerConfigChallenges>());
            // add challenge to player config if not exists
            if (!_playerConfigs[steamId].Challenges[_currentSchedule.Key].ContainsKey(challenge.Key))
            {
                // add new challenge
                _playerConfigs[steamId].Challenges[_currentSchedule.Key].Add(challenge.Key, new PlayerConfigChallenges
                {
                    Amount = 1,
                    LastUpdate = GetUnixTimestamp() + challenge.Cooldown
                });
            }
            else
            {
                // update existing challenge
                _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].Amount++;
                _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].LastUpdate = GetUnixTimestamp() + challenge.Cooldown;
            }
        }

        private async Task ChallengeNotification(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            // check if challenge is completed
            if (_playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].Amount >= challenge.Amount)
            {
                DebugPrint($"user {steamId} has completed challenge {challenge.Key}");
                if (challenge.AnnounceCompletion)
                {
                    await NotifyChallengeCompletion(player, challenge);
                }
                await TriggerCompletionEvent(player, challenge);
            }
            else
            {
                // check if challenge progress should be announced
                if (Config.Notifications.NotifyPlayerOnChallengeProgress && challenge.AnnounceProgress)
                    await NotifyChallengeProgress(player, challenge);
                await TriggerProgressEvent(player, challenge);
            }
            // show updated players gui if enabled
            if (Config.GUI.ShowOnChallengeUpdate)
            {
                _ = Server.NextFrameAsync(() =>
                {
                    if (player == null || !player.IsValid || !_playerConfigs.ContainsKey(steamId)) return;
                    ShowGui(player, Config.GUI.OnChallengeUpdateDuration);
                });
            }
            await Task.CompletedTask;
        }

        private async Task NotifyChallengeCompletion(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            // sync with game thread to avoid crashes
            _ = Server.NextFrameAsync(() =>
            {
                if (player == null || !player.IsValid || !_playerConfigs.ContainsKey(steamId)) return;
                if (challenge.Visible && challenge.AnnounceCompletion) SendDiscordMessageOnChallengeCompleted(player, challenge);

                foreach (CCSPlayerController entry in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
                {
                    if ((entry == player && !Config.Notifications.NotifyPlayerOnChallengeComplete)
                        || (entry != player && !Config.Notifications.NotifyOtherOnChallengeComplete)) continue;

                    string message = entry == player
                        ? LocalizerExtensions.ForPlayer(Localizer, entry, "challenges.completed.user")
                        : LocalizerExtensions.ForPlayer(Localizer, entry, "challenges.completed.other");

                    if (entry == player && Config.Notifications.ChallengeCompleteSound != "")
                        if (Config.Notifications.ChallengeCompleteSound.StartsWith("sounds/"))
                        {
                            // simply play sound (will be played at 100% volume regardless of the player's volume settings)
                            player.ExecuteClientCommand($"play {Config.Notifications.ChallengeCompleteSound}");
                        }
                        else
                        {
                            // only players that rolled the dice will hear the sound
                            RecipientFilter filter = [player];
                            // will be played at the player's volume settings
                            player.EmitSound(Config.Notifications.ChallengeCompleteSound, filter);
                        }

                    entry.PrintToChat(message.Replace("{challenge}", GetChallengeTitle(challenge, player))
                        .Replace("{player}", player.PlayerName)
                        .Replace("{total}", challenge.Amount.ToString())
                        .Replace("{count}", challenge.Amount.ToString()));
                }
            });
            await Task.CompletedTask;
        }

        private async Task TriggerCompletionEvent(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            _ = Server.NextFrameAsync(() =>
            {
                if (player == null || !player.IsValid || !_playerConfigs.ContainsKey(steamId) || player.UserId == null) return;
                // check internal actions
                OnCompletionAction(player, challenge);
                // build event data for external plugins
                var eventData = new Dictionary<string, Dictionary<string, string>>
                {
                    ["info"] = new Dictionary<string, string>
                {
                    { "title", GetChallengeTitle(challenge, player) },
                    { "type", challenge.Type },
                    { "amount", challenge.Amount.ToString() },
                    { "cooldown", challenge.Cooldown.ToString() }
                }
                };
                // add data for external plugins
                foreach (var kvp2 in challenge.Data)
                {
                    eventData.Add(kvp2.Key, kvp2.Value);
                }
                // trigger event
                TriggerEvent(new PlayerCompletedChallengeEvent((int)player.UserId, eventData));
            });
            await Task.CompletedTask;
        }

        private async Task NotifyChallengeProgress(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            string count = _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].Amount.ToString();

            _ = Server.NextFrameAsync(() =>
            {
                if (player == null || !player.IsValid || !_playerConfigs.ContainsKey(steamId)) return;
                // play sound for player if enabled
                if (Config.Notifications.ChallengeProgressSound != "")
                    if (Config.Notifications.ChallengeProgressSound.StartsWith("sounds/"))
                    {
                        // simply play sound (will be played at 100% volume regardless of the player's volume settings)
                        player.ExecuteClientCommand($"play {Config.Notifications.ChallengeProgressSound}");
                    }
                    else
                    {
                        // only players that rolled the dice will hear the sound
                        RecipientFilter filter = [player];
                        // will be played at the player's volume settings
                        player.EmitSound(Config.Notifications.ChallengeProgressSound, filter);
                    }
                // build and send message
                string message = LocalizerExtensions.ForPlayer(Localizer, player, "challenges.progress")
                    .Replace("{challenge}", GetChallengeTitle(challenge, player))
                    .Replace("{total}", challenge.Amount.ToString())
                    .Replace("{count}", count);
                player.PrintToChat(message);
            });
            await Task.CompletedTask;
        }

        private async Task TriggerProgressEvent(CCSPlayerController player, ChallengesBlueprint challenge)
        {
            string steamId = player.NetworkIDString;
            if (Config.AllowBots && player.IsBot)
                steamId = "BOT";
            _ = Server.NextFrameAsync(() =>
            {
                if (player == null || !player.IsValid || !_playerConfigs.ContainsKey(steamId) || player.UserId == null) return;
                // build event data for external plugins
                var eventData = new Dictionary<string, Dictionary<string, string>>
                {
                    ["info"] = new Dictionary<string, string>
                    {
                        { "title", GetChallengeTitle(challenge, player) },
                        { "type", challenge.Type },
                        { "current_amount", _playerConfigs[steamId].Challenges[_currentSchedule.Key][challenge.Key].Amount.ToString() },
                        { "total_amount", challenge.Amount.ToString() },
                        { "cooldown", challenge.Cooldown.ToString() }
                    }
                };
                // add data for external plugins
                foreach (var kvp2 in challenge.Data)
                {
                    eventData.Add(kvp2.Key, kvp2.Value);
                }
                // trigger event
                TriggerEvent(new PlayerProgressedChallengeEvent((int)player.UserId, eventData));
            });
            await Task.CompletedTask;
        }
    }
}
