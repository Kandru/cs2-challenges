using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Localization;

namespace Challenges
{
    /// <summary>Round-lifecycle driver for the tracker HUD: freeze-time show, progress reset, intermission hide.</summary>
    public class HudDriver : Module
    {
        private bool _destroyed;

        public HudDriver(State state, IStringLocalizer localizer, bool isHotReloaded)
            : base(state, localizer)
        {
            Context.Bind(state, localizer);
            if (isHotReloaded && Utilities.GetPlayers().Count > 0)
            {
                State.DuringRound = true;
            }
        }

        public override List<string> Events =>
        [
            "EventRoundStart",
            "EventRoundFreezeEnd",
            "EventRoundEnd",
            "EventCsIntermission",
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
            State.DuringRound = true;
            GetModule<ChallengeEngine>().InvalidateRoundCache();

            int freezeTime = Tracker.FreezeTimeSeconds;
            if (freezeTime <= 0)
            {
                State.FreezeActive = false;
            }
            else
            {
                State.FreezeActive = true;
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
                state.ClearTrackerUi();
            }

            if (GlobalConfig.Gui.ShowOnRoundStart && freezeTime > 0)
            {
                Server.NextFrame(() =>
                {
                    if (_destroyed || !State.FreezeActive)
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
            State.FreezeActive = false;
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                Tracker.EndFreeze(player);
            }

            return HookResult.Continue;
        }

        public HookResult EventRoundEnd(EventRoundEnd @event, GameEventInfo info)
        {
            State.DuringRound = false;
            State.FreezeActive = false;
            return HookResult.Continue;
        }

        public HookResult EventCsIntermission(EventCsIntermission @event, GameEventInfo info)
        {
            State.FreezeActive = false;
            foreach (CCSPlayerController player in Players.GetHumans())
            {
                Tracker.Hide(player);
            }

            return HookResult.Continue;
        }

        public override void Destroy()
        {
            _destroyed = true;
            Context.Unbind();
        }
    }
}
