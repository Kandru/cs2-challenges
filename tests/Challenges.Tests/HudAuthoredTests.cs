namespace Challenges.Tests
{
    /// <summary>The generated authored-state table the HUD write caches fall back to on a pristine slot.</summary>
    public sealed class HudAuthoredTests
    {
        [Fact]
        public void RootCardsStartHiddenInTheDefaultTheme()
        {
            foreach (string root in new[] { Tracker.Panel, Menu.Panel })
            {
                Assert.True(HudAuthored.HasClass(root, "ph-off"));
                Assert.True(HudAuthored.HasClass(root, "theme-gold"));
                Assert.False(HudAuthored.HasClass(root, "ph-fading"));
            }
        }

        [Fact]
        public void PoolSlotsStartCollapsed()
        {
            for (int row = 0; row < Tracker.MaxRows; row++)
            {
                Assert.True(HudAuthored.HasClass(Tracker.RowId(row), "is-off"));
            }

            Assert.True(HudAuthored.HasClass(Tracker.TimerId, "is-off"));
        }

        [Fact]
        public void LaddersReportTheirAuthoredPercent()
        {
            Assert.Equal(100, HudAuthored.Percent(Tracker.TimerFillId));
            for (int row = 0; row < Tracker.MaxRows; row++)
            {
                Assert.Equal(0, HudAuthored.Percent(Tracker.FillId(row)));
            }
        }

        [Fact]
        public void TimerFillCarriesTheClientDrainClass()
        {
            Assert.True(HudAuthored.HasClass(Tracker.TimerFillId, "TrackerTimerFill"));
            Assert.False(HudAuthored.HasClass(Tracker.TimerFillId, "is-running"));
        }

        [Fact]
        public void CardIndexIsTwoDigitsUpToNinetyNine()
        {
            Assert.Equal("01", ChallengeCardPaint.Index(1));
            Assert.Equal("12", ChallengeCardPaint.Index(12));
            Assert.Equal("100", ChallengeCardPaint.Index(100));
            Assert.Same(ChallengeCardPaint.Index(7), ChallengeCardPaint.Index(7));
        }

        [Fact]
        public void UnknownPanelsHaveNoAuthoredState()
        {
            Assert.False(HudAuthored.HasClass("no-such-panel", "is-off"));
            Assert.Equal(-1, HudAuthored.Percent("no-such-panel"));
            Assert.Equal(-1, HudAuthored.Percent(Tracker.Panel));
        }
    }
}
