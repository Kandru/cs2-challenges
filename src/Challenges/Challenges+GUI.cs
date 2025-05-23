using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Utils;
using System.Drawing;

namespace Challenges
{
    public partial class Challenges : BasePlugin
    {
        private void ShowGuiOnRoundStart()
        {
            if (!Config.Enabled
                || !_isDuringRound
                || !Config.GUI.ShowOnRoundStart) return;
            int freezeTime = 0;
            ConVar? mpFreezeTime = ConVar.Find("mp_freezetime");
            if (mpFreezeTime != null)
            {
                freezeTime = mpFreezeTime.GetPrimitiveValue<int>();
            }
            foreach (CCSPlayerController player in Utilities.GetPlayers().Where(p => p.IsValid && !p.IsBot))
            {
                if (!_playerConfigs.ContainsKey(player.NetworkIDString)
                    || (player.TeamNum != (int)CsTeam.CounterTerrorist && player.TeamNum != (int)CsTeam.Terrorist)) continue;
                AddTimer(0.3f, () =>
                {
                    if (player == null
                    || !player.IsValid
                    || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                    // check for user preferences
                    float duration = _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways
                        ? 0
                        : freezeTime + Config.GUI.OnRoundStartDuration;
                    // show GUI
                    ShowGui(player, duration);
                });
            }
        }

        private void ShowGuiOnSpawn(CCSPlayerController player)
        {
            if (!Config.Enabled || !Config.GUI.ShowAfterRespawn) return;
            if (player == null
                || !player.IsValid
                || player.IsBot
                || player.NetworkIDString == null
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || (player.TeamNum != (int)CsTeam.CounterTerrorist && player.TeamNum != (int)CsTeam.Terrorist)) return;
            AddTimer(0.3f, () =>
            {
                if (player == null
                    || !player.IsValid
                    || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                // check for user preferences
                float duration = _playerConfigs[player.NetworkIDString].Settings.Challenges.ShowAlways
                    ? 0
                    : Config.GUI.AfterRespawnDuration;
                // show GUI
                ShowGui(player, duration);
            });
        }

        private void ShowGui(CCSPlayerController player, float duration = 10.0f)
        {
            if (!Config.Enabled
                || player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)
                || player.Pawn == null
                || !player.Pawn.IsValid
                || player.Pawn.Value == null
                || player.Pawn.Value.LifeState != (byte)LifeState_t.LIFE_ALIVE) return;
            // create gui message
            string? message = BuildGuiMessage(player);
            if (message == null) return;
            // use our entity if it still exists
            if (_playerHudPersonalChallenges.ContainsKey(player.NetworkIDString))
            {
                if (_playerHudPersonalChallenges[player.NetworkIDString] != null
                    && _playerHudPersonalChallenges[player.NetworkIDString].IsValid)
                {
                    UpdateGui(player, message);
                    return;
                }
                else
                {
                    _playerHudPersonalChallenges.Remove(player.NetworkIDString);
                }
            }
            // create hud
            CPointWorldText? hudText = WorldTextManager.Create(
                    player,
                    message,
                    Config.GUI.FontSize,
                    ColorTranslator.FromHtml(Config.GUI.FontColor),
                    Config.GUI.FontName,
                    Config.GUI.PositionX,
                    Config.GUI.PositionY,
                    Config.GUI.Background,
                    Config.GUI.BackgroundFactor
                );
            if (hudText == null) return;
            _playerHudPersonalChallenges.Add(player.NetworkIDString, hudText);
            // remove hud after duration
            if (duration > 0)
                AddTimer(duration, () =>
                {
                    if (player == null
                    || !player.IsValid
                    || !_playerConfigs.ContainsKey(player.NetworkIDString)) return;
                    HideGui(player);
                });
        }

        private void UpdateGui(CCSPlayerController player, string message)
        {
            if (!Config.Enabled
                || player == null
                || !player.IsValid
                || !_playerHudPersonalChallenges.ContainsKey(player.NetworkIDString)
                || _playerHudPersonalChallenges[player.NetworkIDString] == null
                || !_playerHudPersonalChallenges[player.NetworkIDString].IsValid) return;

            // set new message
            _playerHudPersonalChallenges[player.NetworkIDString].AcceptInput(
                "SetMessage",
                _playerHudPersonalChallenges[player.NetworkIDString],
                _playerHudPersonalChallenges[player.NetworkIDString],
                message
            );
        }

        private void HideGui(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || !_playerHudPersonalChallenges.ContainsKey(player.NetworkIDString)) return;
            // do not kill if entity is no longer valid
            if (_playerHudPersonalChallenges[player.NetworkIDString] != null
                && _playerHudPersonalChallenges[player.NetworkIDString].IsValid)
            {
                _playerHudPersonalChallenges[player.NetworkIDString].AcceptInput("kill");
                _playerHudPersonalChallenges[player.NetworkIDString].Remove();
            }
            // remove hud from list
            _playerHudPersonalChallenges.Remove(player.NetworkIDString);
        }

        private void HideAllGui()
        {
            foreach (var kvp in _playerHudPersonalChallenges)
            {
                if (kvp.Value != null
                    && kvp.Value.IsValid)
                    kvp.Value.AcceptInput("kill");
            }
            _playerHudPersonalChallenges.Clear();
        }

        private string? BuildGuiMessage(CCSPlayerController player)
        {
            if (player == null
                || !player.IsValid
                || !_playerConfigs.ContainsKey(player.NetworkIDString)) return null;
            // check for running challenges which can be completed
            var challenges = _currentSchedule.Challenges
                .Where(kvp => CanChallengeBeCompleted(kvp.Value, player.NetworkIDString)
                    && IsChallengeAllowedOnThisMap(kvp.Value)
                    && kvp.Value.Visible)
                .ToList();
            if (challenges.Count == 0) return null;
            // build challenges message
            string message = "{challenges_title}";
            // iterate through all challenges and list them
            int displayedChallenges = 0;
            int finishedChallenges = 0;
            if (!_playerConfigs.TryGetValue(player.NetworkIDString, out var playerConfig) ||
                !playerConfig.Challenges.TryGetValue(_currentSchedule.Key, out var playerChallenges))
                playerChallenges = new Dictionary<string, PlayerConfigChallenges>();

            foreach (var kvp in challenges.OrderByDescending(c => playerChallenges.TryGetValue(c.Key, out var challenge) ? challenge.Amount : 0))
            {
                // check if player already has progressed or completed this challenge
                if (playerChallenges.TryGetValue(kvp.Key, out var challenge))
                {
                    bool isFinished = challenge.Amount >= kvp.Value.Amount;
                    if (isFinished) finishedChallenges++;

                    // display only unfinished challenges
                    if (displayedChallenges < Config.GUI.DisplayMaximum && !isFinished)
                    {
                        string tmpMessage = $"\n{GetChallengeTitle(kvp.Value, player)}"
                            .Replace("{total}", kvp.Value.Amount.ToString("N0"))
                            .Replace("{count}", challenge.Amount.ToString("N0"));
                        message += tmpMessage;
                        displayedChallenges++;
                    }
                }
                else
                {
                    // display not started challenges
                    if (displayedChallenges < Config.GUI.DisplayMaximum)
                    {
                        string tmpMessage = $"\n{GetChallengeTitle(kvp.Value, player)}"
                            .Replace("{total}", kvp.Value.Amount.ToString("N0"))
                            .Replace("{count}", "0");
                        message += tmpMessage;
                        displayedChallenges++;
                    }
                }
            }
            if (displayedChallenges == 0) message += "\n" + LocalizerExtensions.ForPlayer(Localizer, player, "challenges.completed.all");
            // replace title with actual values
            return message.Replace(
                "{challenges_title}", GetScheduleTitle(_currentSchedule, player)
                .Replace("{playerName}", player.PlayerName.Length > 12 ? player.PlayerName.Substring(0, 12) : player.PlayerName)
                .Replace("{total}", challenges.Count.ToString())
                .Replace("{count}", finishedChallenges.ToString()));
        }
    }
}
