using System.Diagnostics;
using Challenges.Utils;

namespace Challenges.Tests
{
    /// <summary>Aggregation and report of the debug perf counters (T4.2).</summary>
    public sealed class PerfCounterTests
    {
        [Fact]
        public void CounterSumsCallsTotalAndMax()
        {
            PerfCounter counter = new("x");
            counter.Add(5);
            counter.Add(20);
            counter.Add(1);

            Assert.Equal(3, counter.Calls);
            Assert.Equal(26, counter.TotalTicks);
            Assert.Equal(20, counter.MaxTicks);
        }

        [Fact]
        public void CounterIsCreatedOncePerName()
        {
            PerfCounters counters = new();
            PerfCounter first = counters.Counter("EventPlayerDeath");

            Assert.Same(first, counters.Counter("EventPlayerDeath"));
            Assert.NotSame(first, counters.Counter("EventPlayerHurt"));
        }

        [Fact]
        public void TopSortsByTotalThenNameAndSkipsUnused()
        {
            PerfCounters counters = new();
            counters.Counter("b").Add(10);
            counters.Counter("a").Add(10);
            counters.Counter("c").Add(30);
            counters.Counter("unused");
            counters.Counter("d").Add(1);

            List<PerfCounter> top = counters.Top(3);

            Assert.Equal(["c", "a", "b"], top.Select(c => c.Name));
        }

        [Fact]
        public void ResetZeroesInPlace()
        {
            PerfCounters counters = new();
            PerfCounter held = counters.Counter("x");
            held.Add(7);

            counters.Reset();

            Assert.Equal(0, held.Calls);
            Assert.Equal(0, held.TotalTicks);
            Assert.Equal(0, held.MaxTicks);
            Assert.Same(held, counters.Counter("x"));
            Assert.Empty(counters.Top(PerfCounters.ReportRows));

            held.Add(3);
            Assert.Equal(3, Assert.Single(counters.Top(PerfCounters.ReportRows)).TotalTicks);
        }

        [Fact]
        public void ReportListsRowsAndGauges()
        {
            PerfCounters counters = new();
            long tenMicroseconds = Stopwatch.Frequency / 100_000;
            counters.Counter("EventPlayerDeath").Add(tenMicroseconds);
            counters.Counter("EventPlayerDeath").Add(tenMicroseconds * 3);

            List<string> lines = counters.Report(15, new PerfGauges(1, 2, 3, 4, 5, 6));

            Assert.Equal(4, lines.Count);
            Assert.Contains("top 15", lines[0]);
            string[] row = lines[2].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(["EventPlayerDeath", "2", "20.0", "30.0", "0.04"], row);
            Assert.Equal(
                "gauges: worker_queue=1 patches=2 pending_steamids=3 shadows=4 save_queue=5 saves_in_flight=6",
                lines[3]);
        }

        [Fact]
        public void ReportWithoutSamplesOrGauges()
        {
            List<string> lines = new PerfCounters().Report(15, null);

            Assert.Equal(3, lines.Count);
            Assert.Equal("(no samples)", lines[2]);
        }

        [Fact]
        public void MicrosecondsFollowStopwatchFrequency()
        {
            Assert.Equal(1_000_000.0, PerfCounters.ToMicroseconds(Stopwatch.Frequency), 6);
        }

        [Fact]
        public void StartAndStopMeasureOnlyWhileEnabled()
        {
            bool previous = PerfCounters.Enabled;
            PerfCounter counter = new("x");
            try
            {
                PerfCounters.Enabled = false;
                long off = PerfCounters.Start();
                PerfCounters.Stop(counter, off);
                Assert.Equal(0, off);
                Assert.Equal(0, counter.Calls);

                PerfCounters.Enabled = true;
                long on = PerfCounters.Start();
                Assert.NotEqual(0, on);
                PerfCounters.Stop(counter, on);
                Assert.Equal(1, counter.Calls);
                Assert.True(counter.TotalTicks >= 0);
            }
            finally
            {
                PerfCounters.Enabled = previous;
            }
        }
    }
}
