using System.Text;

namespace Challenges.Tests
{
    /// <summary>Player tracker settings: server defaults, steps and what lands in the player file.</summary>
    public sealed class HudSettingsTests
    {
        [Fact]
        public void Unset_settings_use_the_server_defaults()
        {
            PlayerState state = new();

            Assert.True(HudSettings.TrackerVisible(state));
            Assert.Equal(TrackerPosition.TopRight, HudSettings.PositionOf(state));
            Assert.Equal(HudSettings.DefaultSize, HudSettings.Sizes[HudSettings.SizeIndexOf(state)]);
        }

        [Fact]
        public void Player_choices_win_and_mark_the_state_dirty()
        {
            PlayerState state = new();

            Assert.True(HudSettings.SetPosition(state, TrackerPosition.Left));
            Assert.False(HudSettings.SetPosition(state, TrackerPosition.Left));
            HudSettings.ToggleVisible(state);

            Assert.True(state.Dirty);
            Assert.Equal(TrackerPosition.Left, HudSettings.PositionOf(state));
            Assert.False(HudSettings.TrackerVisible(state));
        }

        [Fact]
        public void Size_steps_stop_at_both_ends()
        {
            PlayerState state = new();

            while (HudSettings.StepSize(state, -1))
            {
            }

            Assert.Equal(HudSettings.Sizes[0], state.Hud.TrackerSize);
            Assert.False(HudSettings.StepSize(state, -1));

            while (HudSettings.StepSize(state, 1))
            {
            }

            Assert.Equal(HudSettings.Sizes[^1], state.Hud.TrackerSize);
        }

        [Fact]
        public void Off_step_and_unknown_values_snap_to_a_valid_choice()
        {
            PlayerState state = new() { Hud = { TrackerSize = 104, TrackerPosition = "middle" } };

            Assert.Equal(100, HudSettings.Sizes[HudSettings.SizeIndexOf(state)]);
            Assert.Equal(TrackerPosition.TopRight, HudSettings.PositionOf(state));
        }

        [Fact]
        public void Player_file_keeps_only_chosen_settings()
        {
            PlayerState state = new();
            string untouched = Encoding.UTF8.GetString(PlayerFiles.Serialize(state));
            Assert.DoesNotContain("tracker_", untouched);

            HudSettings.SetPosition(state, TrackerPosition.Right);
            HudSettings.StepSize(state, 1);
            PlayerState loaded = PlayerFiles.DeserializeBytes(PlayerFiles.Serialize(state))!;

            Assert.Equal(TrackerPosition.Right, HudSettings.PositionOf(loaded));
            Assert.Equal(110, loaded.Hud.TrackerSize);
            Assert.Null(loaded.Hud.TrackerVisible);
        }

        [Fact]
        public void Old_player_files_without_hud_load()
        {
            PlayerState loaded = PlayerFiles.DeserializeBytes("""{"steamid":"1","hud":null}"""u8)!;

            Assert.NotNull(loaded.Hud);
            Assert.True(HudSettings.TrackerVisible(loaded));
        }
    }
}
