using ChallengesShared.Events;
using CounterStrikeSharp.API.Core;

namespace Challenges
{
    public class RunningChallengeSchedule
    {
        public string Title { get; set; } = "";
        public string Key { get; set; } = "";
        public string StartDate { get; set; } = "2025-01-01 00:00:00";
        public string EndDate { get; set; } = "2025-02-01 00:00:00";
        public Dictionary<string, RunningChallengeBlueprints> Challenges { get; set; } = [];
    }

    public class RunningChallengeBlueprints
    {
        public string Title { get; set; } = "";
        public string Type { get; set; } = "";
        public int Points { get; set; } = 0;
        public int Amount { get; set; } = 0;
        public List<ChallengesBlueprintRules> Rules { get; set; } = [];
    }

    public partial class Challenges : BasePlugin
    {
        private RunningChallengeSchedule _currentChallenge = new();
        private Dictionary<string, CPointWorldText> _playerHudPersonalChallenges = [];

        private void CheckForRunningChallenge()
        {
            DebugPrint("checking for running challenge");
            // reset current challenge
            _currentChallenge = new RunningChallengeSchedule();
            // check if we have a new challenge
            if (_playerChallenges.Schedule.Count == 0
                || _playerChallenges.Blueprints.Count == 0) return;
            // iterate through all schedules
            foreach (var kvp in _playerChallenges.Schedule)
            {
                if (DateTime.TryParse(kvp.Value.StartDate, out DateTime startDate)
                    && DateTime.TryParse(kvp.Value.EndDate, out DateTime endDate)
                    && startDate <= DateTime.UtcNow
                    && endDate >= DateTime.UtcNow)
                {
                    DebugPrint($"found running challenge {kvp.Key}");
                    // set current challenge
                    _currentChallenge.Title = kvp.Value.Title;
                    _currentChallenge.Key = kvp.Key;
                    // use unique key combinations to avoid having the same key for different challenges
                    // this will reset the challenge on change of date or title which is intentional
                    using (var sha256 = System.Security.Cryptography.SHA256.Create())
                    {
                        var hashInput = $"{_currentChallenge.Title}{_currentChallenge.StartDate}{_currentChallenge.EndDate}";
                        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashInput));
                        _currentChallenge.Key = Convert.ToBase64String(hashBytes);
                    }
                    _currentChallenge.StartDate = kvp.Value.StartDate;
                    _currentChallenge.EndDate = kvp.Value.EndDate;
                    // find blueprints for challenge
                    foreach (var challenge in kvp.Value.Challenges)
                    {
                        if (_playerChallenges.Blueprints.ContainsKey(challenge))
                        {
                            DebugPrint($"found blueprint {challenge} for challenge {kvp.Key}");
                            _currentChallenge.Challenges.Add(
                                challenge,
                                new RunningChallengeBlueprints
                                {
                                    Title = _playerChallenges.Blueprints[challenge].Title,
                                    Type = _playerChallenges.Blueprints[challenge].Type,
                                    Points = _playerChallenges.Blueprints[challenge].Points,
                                    Amount = _playerChallenges.Blueprints[challenge].Amount,
                                    Rules = _playerChallenges.Blueprints[challenge].Rules
                                }
                            );
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
            if (_currentChallenge.Challenges.Count == 0)
            {
                // delete all challenges from the user
                _playerConfigs[player.NetworkIDString].Challenges.Clear();
                return;
            }
            // check player for outdated challenges
            foreach (var kvp in _playerConfigs[player.NetworkIDString].Challenges.ToList())
            {
                if (kvp.Value.ScheduleKey != _currentChallenge.Key)
                {
                    DebugPrint($"deleting outdated challenge {kvp.Key} for user {player.NetworkIDString}");
                    _playerConfigs[player.NetworkIDString].Challenges.Remove(kvp.Key);
                }
            }
            DebugPrint($"CheckChallengeGoal for {player.NetworkIDString} -> {type}");
            // check for running challenges of the specified type
            var challenges = _currentChallenge.Challenges.Where(x => x.Value.Type == type).ToList();
            if (challenges.Count == 0) return;
            foreach (var kvp in challenges)
            {
                DebugPrint($"found challenge {kvp.Key}");
                // check if the user has already completed the challenge
                if (_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(kvp.Key)
                    && _playerConfigs[player.NetworkIDString].Challenges[kvp.Key].Amount >= kvp.Value.Amount)
                {
                    DebugPrint($"user {player.NetworkIDString} has already completed challenge {kvp.Key}");
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
                    DebugPrint($"user {player.NetworkIDString} has mastered a step of challenge {kvp.Key}");
                    // add challenge to user or update challenge
                    if (!_playerConfigs[player.NetworkIDString].Challenges.ContainsKey(kvp.Key))
                    {
                        _playerConfigs[player.NetworkIDString].Challenges.Add(kvp.Key, new PlayerConfigChallenges
                        {
                            ScheduleKey = _currentChallenge.Key,
                            Amount = 1
                        });
                    }
                    else
                    {
                        _playerConfigs[player.NetworkIDString].Challenges[kvp.Key].Amount++;
                    }
                    // check if the user has completed the challenge
                    if (_playerConfigs[player.NetworkIDString].Challenges[kvp.Key].Amount >= kvp.Value.Amount)
                    {
                        DebugPrint($"user {player.NetworkIDString} has completed challenge {kvp.Key}");
                        // notify user about completion
                        if (Config.Notifications.NotifyPlayerOnChallengeComplete)
                            player.PrintToChat(
                                Localizer["challenges.completed.user"]
                                    .Value
                                    .Replace("{challenge}", kvp.Value.Title)
                            );
                        // notify other players about completion
                        if (Config.Notifications.NotifyOtherOnChallengeComplete)
                            SendGlobalChatMessage(
                                message: Localizer["challenges.completed.other"]
                                    .Value
                                    .Replace("{challenge}", kvp.Value.Title),
                                player: player
                            );
                        // send event to other plugins
                        TriggerEvent(new PlayerCompletedChallengeEvent(player, new Dictionary<string, string>
                        {
                            { "title", kvp.Value.Title },
                            { "type", kvp.Value.Type },
                            { "points", kvp.Value.Points.ToString() },
                            { "amount", kvp.Value.Amount.ToString() },
                        }));
                    }
                    else
                    {
                        // notify user about progress
                        if (Config.Notifications.NotifyPlayerOnChallengeProgress)
                            player.PrintToChat(
                                Localizer["challenges.progress"]
                                    .Value
                                    .Replace("{challenge}", kvp.Value.Title
                                        .Replace("{total}", kvp.Value.Amount.ToString())
                                        .Replace("{count}", _playerConfigs[player.NetworkIDString].Challenges[kvp.Key].Amount.ToString()))
                            );
                        // send event to other plugins
                        TriggerEvent(new PlayerProgressedChallengeEvent(player, new Dictionary<string, string>
                        {
                            { "title", kvp.Value.Title },
                            { "type", kvp.Value.Type },
                            { "points", kvp.Value.Points.ToString() },
                            { "current_amount", _playerConfigs[player.NetworkIDString].Challenges[kvp.Key].Amount.ToString() },
                            { "total_amount", kvp.Value.Amount.ToString() },
                        }));
                    }
                    // show challenges gui if enabled
                    if (Config.GUI.ShowOnChallengeUpdate)
                        ShowGui(player, Config.GUI.OnChallengeUpdateDuration);
                }
            }
        }
    }
}
