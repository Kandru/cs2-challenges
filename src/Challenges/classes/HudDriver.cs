using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Challenges.Configs;
using Challenges.Enums;
using Challenges.Huds;
using Challenges.Utils;
using Microsoft.Extensions.Localization;

namespace Challenges.Classes
{
    /// <summary>Round-lifecycle driver for the tracker HUD (freeze-time mode) and per-round HUD reset.</summary>
    public class HudDriver : ClassesBlueprint
    {
        private bool _destroyed;

        public HudDriver(Dictionary<GlobalStates, object> globalState, IStringLocalizer localizer, bool isHotReloaded)
            : base(globalState, localizer, isHotReloaded)
        {
            Context.Bind(globalState, localizer);
            if (isHotReloaded && Utilities.GetPlayers().Count > 0)
            {
                _globalStates[GlobalStates.DuringRound] = true;
            }
        }

        public override List<string> Events =>
        [
            "EventRoundStart",
            "EventRoundFreezeEnd",
            "EventRoundEnd",
        ];

        public override List<string> Listeners => ["OnCustomHudClicked"];

        public void OnCustomHudClicked(CCSPlayerController player, CCSCustomHudLayout customLayout, string buttonId)
        {
            if (_destroyed || !Players.IsHumanViewer(player))
            {
                return;
            }

            _ = HudMenu.OnCustomHudClicked(player, customLayout, buttonId);
        }

        public HookResult EventRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _globalStates[GlobalStates.DuringRound] = true;
            GetClass<ChallengeEngine>().InvalidateRoundCache();

            int freezeTime = Tracker.FreezeTimeSeconds;
            if (freezeTime <= 0)
            {
                _globalStates[GlobalStates.FreezeActive] = false;
            }
            else
            {
                _globalStates[GlobalStates.FreezeActive] = true;
            }

            try
            {
                CustomHud.ResetRound();
            }
            catch (Exception ex)
            {
                DebugPrint($"ResetRound failed: {ex.Message}");
            }

            foreach (CCSPlayerController player in Players.GetHumans())
            {
                PlayerState state = GetPlayerState(player);
                state.ActiveMenu = ActiveMenu.None;
                state.TrackerFreezeVisible = false;
                state.TrackerFreezeUntil = null;
                state.TrackerFreezeDuration = 0;
                state.TrackerProgressUntil = null;
                state.TrackerFadeUntil = null;
                state.TrackerUpNextPending = false;
                state.TrackerShowingUpNext = false;
                state.TrackerProgressItems.Clear();
                state.TrackerFingerprint = null;
            }

            if (GlobalConfig.Gui.ShowOnRoundStart && freezeTime > 0)
            {
                Server.NextFrame(() =>
                {
                    if (_destroyed || !(bool)_globalStates[GlobalStates.FreezeActive])
                    {
                        return;
                    }

                    foreach (CCSPlayerController player in Players.GetHumans())
                    {
                        if (Players.IsPlayingTeam(player))
                        {
                            Tracker.ShowFreeze(player);
                        }
                    }
                });
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundFreezeEnd(EventRoundFreezeEnd @event, GameEventInfo info)
        {
            _globalStates[GlobalStates.FreezeActive] = false;
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                Tracker.EndFreeze(player);
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            _globalStates[GlobalStates.DuringRound] = false;
            _globalStates[GlobalStates.FreezeActive] = false;
            return HookResult.Continue;
        }

        public override void Destroy()
        {
            _destroyed = true;
            Context.Unbind();
        }
    }
}
