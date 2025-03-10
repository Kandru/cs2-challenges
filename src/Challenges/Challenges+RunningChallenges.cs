using ChallengesShared.Events;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private RunningChallengeSchedule _currentSchedule = new();
        private Dictionary<string, CPointWorldText> _playerHudPersonalChallenges = [];

        private void CheckForRunningSchedule()
        {
            DebugPrint("checking for running schedule");
            // reset current challenge
            _currentSchedule = new RunningChallengeSchedule();
            // check if we have a new challenge
            if (_availableChallenges.Schedules.Count == 0
                || _availableChallenges.Blueprints.Count == 0) return;
            // iterate through all schedules
            foreach (var kvp in _availableChallenges.Schedules)
            {
                if (DateTime.TryParse(kvp.Value.StartDate, out DateTime startDate)
                    && DateTime.TryParse(kvp.Value.EndDate, out DateTime endDate)
                    && startDate <= DateTime.UtcNow
                    && endDate >= DateTime.UtcNow)
                {
                    DebugPrint($"found running schedule {kvp.Key}");
                    // set current challenge
                    _currentSchedule.Title = kvp.Value.Title;
                    _currentSchedule.Key = kvp.Key;
                    _currentSchedule.StartDate = kvp.Value.StartDate;
                    _currentSchedule.EndDate = kvp.Value.EndDate;
                    // use unique key combinations to avoid having the same key for different challenges
                    // this will reset the challenge on change of date or title which is intentional
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashInput = $"{_currentSchedule.Title.First()}{_currentSchedule.StartDate}{_currentSchedule.EndDate}";
                        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
                        _currentSchedule.Key = Convert.ToBase64String(hashBytes);
                    }
                    _currentSchedule.StartDate = kvp.Value.StartDate;
                    _currentSchedule.EndDate = kvp.Value.EndDate;
                    // find blueprints for challenge
                    foreach (var challenge in kvp.Value.Challenges)
                    {
                        // check if challenge is already in list
                        if (_currentSchedule.Challenges.ContainsKey(challenge)) continue;
                        // check if we have a blueprint for the challenge
                        if (_availableChallenges.Blueprints.TryGetValue(challenge, out var blueprint))
                        {
                            DebugPrint($"found blueprint {challenge} for schedule {kvp.Key}");
                            _currentSchedule.Challenges.Add(challenge, blueprint);
                        }
                        else // check if we have wildcard blueprints for the challenge
                        {
                            var wildcardKeys = _availableChallenges.Blueprints.Keys.Where(k => k.StartsWith(challenge.TrimEnd('*'))).ToList();
                            if (wildcardKeys.Count > 0)
                            {
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
                            else
                            {
                                DebugPrint($"couldn't find blueprint {challenge} for challenge {kvp.Key}");
                            }
                        }
                    }
                    break;
                }
            }
        }

        private void CheckChallengeGoal(CCSPlayerController? player, string type, Dictionary<string, string> data)
        {
            if (!Config.Enabled
                || player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
            // check if we have a running challenge
            if (_currentSchedule.Challenges.Count == 0)
            {
                // delete all challenges from the user
                _playerConfigs[player.NetworkIDString].Challenges.Clear();
                return;
            }
            // check player for outdated challenges
            foreach (var kvp in _playerConfigs[player.NetworkIDString].Challenges)
            {
                if (kvp.Key == _currentSchedule.Key) continue;
                DebugPrint($"deleting outdated challenge {kvp.Key} for user {player.NetworkIDString}");
                _playerConfigs[player.NetworkIDString].Challenges.Remove(kvp.Key);
            }
            DebugPrint($"CheckChallengeGoal for {player.NetworkIDString} -> {type}");
            // check for running challenges of the specified type
            var challenges = _currentSchedule.Challenges.Where(x => x.Value.Type == type).ToList();
            if (challenges.Count == 0) return;
            foreach (var kvp in challenges)
            {
                DebugPrint($"found challenge {kvp.Key}");
                // check if the user has already completed the challenge
                if (_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                    && _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(kvp.Key)
                    && _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].Amount >= kvp.Value.Amount)
                {
                    DebugPrint($"user {player.NetworkIDString} has already completed challenge {kvp.Key}");
                    continue;
                }
                // check if the user can attend the challenge because of dependencies which have to be completed first
                if (!CanChallengeBeCompleted(kvp.Value, player))
                {
                    DebugPrint($"user {player.NetworkIDString} has not completed dependencies for challenge {kvp.Key}");
                    continue;
                }
                // check if the user can attend the challenge because of a possible cooldown
                if (_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key)
                    && _playerConfigs[player.NetworkIDString].Challenges.ContainsKey(kvp.Key)
                    && _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].LastUpdate + kvp.Value.Cooldown > GetUnixTimestamp())
                {
                    DebugPrint($"user {player.NetworkIDString} has cooldown for challenge {kvp.Key}");
                    continue;
                }
                // check if the user has complied with the rules of the challenge
                bool compliedWithRules = true;
                foreach (var rule in kvp.Value.Rules)
                {
                    // stop checking if we have a rule that is not in our data (e.g. wrong spelling)
                    if (!data.ContainsKey(rule.Key.ToLower()))
                    {
                        DebugPrint($"rule {rule.Key} not found in data for type {type}");
                        compliedWithRules = false;
                        continue;
                    }
                    // check if the rule is met
                    var currentValue = data[rule.Key.ToLower()];
                    var targetValue = rule.Value.ToLower();
                    DebugPrint($"checking rule {rule.Key} {rule.Operator} {targetValue} against {currentValue}");
                    // check mathematically and for boolean values
                    switch (rule.Operator)
                    {
                        case "==":
                            if (currentValue != targetValue) compliedWithRules = false;
                            break;
                        case "!=":
                            if (currentValue == targetValue) compliedWithRules = false;
                            break;
                        case ">":
                            if (float.Parse(currentValue) <= float.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case "<":
                            if (float.Parse(currentValue) >= float.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case ">=":
                            if (float.Parse(currentValue) < float.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case "<=":
                            if (float.Parse(currentValue) > float.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case "bool==":
                            if (bool.Parse(currentValue) != bool.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case "bool!=":
                            if (bool.Parse(currentValue) == bool.Parse(targetValue)) compliedWithRules = false;
                            break;
                        case "contains":
                            if (!currentValue.Contains(targetValue)) compliedWithRules = false;
                            break;
                        default:
                            DebugPrint($"unknown operator {rule.Operator}");
                            compliedWithRules = false;
                            break;
                    }
                    // do not test other rules because we already failed
                    if (!compliedWithRules) break;
                }
                // give points if all rules are met
                if (compliedWithRules)
                {
                    // add schedule key for player if not exists
                    if (!_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(_currentSchedule.Key))
                    {
                        _playerConfigs[player.NetworkIDString].Challenges.Add(_currentSchedule.Key, new Dictionary<string, PlayerConfigChallenges>());
                    }
                    // add challenge to user or update challenge
                    if (!_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].ContainsKey(kvp.Key))
                    {
                        _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key].Add(kvp.Key, new PlayerConfigChallenges
                        {
                            Amount = 1,
                            LastUpdate = GetUnixTimestamp() + kvp.Value.Cooldown
                        });
                    }
                    else
                    {
                        _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].Amount++;
                        _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].LastUpdate = GetUnixTimestamp() + kvp.Value.Cooldown;
                    }
                    // check if the user has completed the challenge
                    if (_playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].Amount >= kvp.Value.Amount)
                    {
                        DebugPrint($"user {player.NetworkIDString} has completed challenge {kvp.Key}");
                        // notify players about completion
                        if (kvp.Value.AnnounceCompletion)
                        {
                            Server.NextFrame(() =>
                            {
                                if (player == null
                                    || !player.IsValid) return;
                                foreach (CCSPlayerController entry in Utilities.GetPlayers())
                                {
                                    if (entry == null
                                    || !entry.IsValid
                                    || entry.IsBot
                                    || (entry == player && !Config.Notifications.NotifyPlayerOnChallengeComplete)
                                    || (entry != player && !Config.Notifications.NotifyOtherOnChallengeComplete)) continue;
                                    string message = "";
                                    if (entry == player)
                                    {
                                        // play sound if available
                                        if (Config.Notifications.ChallengeCompleteSound != "")
                                            player.ExecuteClientCommand($"play {Config.Notifications.ChallengeCompleteSound}");
                                        // build user message
                                        message = LocalizerExtensions.ForPlayer(Localizer, entry, "challenges.completed.user");
                                    }
                                    else
                                    {
                                        // build other message
                                        message = LocalizerExtensions.ForPlayer(Localizer, entry, "challenges.completed.other");
                                    }
                                    entry.PrintToChat(message.Replace("{challenge}", GetChallengeTitle(kvp.Value, player))
                                        .Replace("{player}", player.PlayerName)
                                        .Replace("{total}", kvp.Value.Amount.ToString())
                                        .Replace("{count}", kvp.Value.Amount.ToString()));
                                }
                            });
                        }
                        // send event to our plugin
                        OnCompletionAction(player, kvp.Value);
                        // send event to other plugins on next frame to decouple from listening plugins and partly avoid lags due to runtime contrains
                        Server.NextFrame(() =>
                        {
                            if (player == null
                                || !player.IsValid
                                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                            // prepare event data
                            var eventData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["info"] = new Dictionary<string, string>
                                {
                                    { "title", GetChallengeTitle(kvp.Value, player) },
                                    { "type", kvp.Value.Type },
                                    { "amount", kvp.Value.Amount.ToString() },
                                    { "cooldown", kvp.Value.Cooldown.ToString() }
                                }
                            };
                            // iterate through Data
                            foreach (var kvp2 in kvp.Value.Data)
                            {
                                eventData.Add(kvp2.Key, kvp2.Value);
                            }
                            // send event to other plugins
                            if (player.UserId != null) TriggerEvent(new PlayerCompletedChallengeEvent((int)player.UserId, eventData));
                        });
                    }
                    else
                    {
                        // notify user about progress
                        if (Config.Notifications.NotifyPlayerOnChallengeProgress && kvp.Value.AnnounceProgress)
                        {
                            // do this before next frame to avoid wrong counting (e.g. multiple counter updates in one frame)
                            string count = _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].Amount.ToString();
                            Server.NextFrame(() =>
                            {
                                if (player == null
                                    || !player.IsValid) return;
                                // play sound if available
                                if (Config.Notifications.ChallengeProgressSound != "")
                                    player.ExecuteClientCommand($"play {Config.Notifications.ChallengeProgressSound}");
                                // send progress message
                                string message = LocalizerExtensions.ForPlayer(Localizer, player, "challenges.progress")
                                    .Replace("{challenge}", GetChallengeTitle(kvp.Value, player))
                                    .Replace("{total}", kvp.Value.Amount.ToString())
                                    .Replace("{count}", count);
                                player.PrintToChat(message);
                            });
                        }
                        // send event to other plugins on next frame to decouple from listening plugins and partly avoid lags due to runtime contrains
                        Server.NextFrame(() =>
                        {
                            if (player == null
                                || !player.IsValid
                                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                            // prepare event data
                            var eventData = new Dictionary<string, Dictionary<string, string>>
                            {
                                ["info"] = new Dictionary<string, string>
                                {
                                    { "title", GetChallengeTitle(kvp.Value, player) },
                                    { "type", kvp.Value.Type },
                                    { "current_amount", _playerConfigs[player.NetworkIDString].Challenges[_currentSchedule.Key][kvp.Key].Amount.ToString() },
                                    { "total_amount", kvp.Value.Amount.ToString() },
                                    { "cooldown", kvp.Value.Cooldown.ToString() }
                                }
                            };
                            // iterate through Data
                            foreach (var kvp2 in kvp.Value.Data)
                            {
                                eventData.Add(kvp2.Key, kvp2.Value);
                            }
                            if (player.UserId != null) TriggerEvent(new PlayerProgressedChallengeEvent((int)player.UserId, eventData));
                        });
                    }
                    // show challenges gui if enabled
                    if (Config.GUI.ShowOnChallengeUpdate)
                        ShowGui(player, Config.GUI.OnChallengeUpdateDuration);
                }
            }
        }
    }
}
