using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Challenges.Utils
{
    /// <summary>Calls, total and maximum handler time of one measured path, in <see cref="Stopwatch"/> ticks.</summary>
    internal sealed class PerfCounter(string name)
    {
        public string Name { get; } = name;
        public long Calls { get; private set; }
        public long TotalTicks { get; private set; }
        public long MaxTicks { get; private set; }

        public void Add(long elapsedTicks)
        {
            Calls++;
            TotalTicks += elapsedTicks;
            if (elapsedTicks > MaxTicks)
            {
                MaxTicks = elapsedTicks;
            }
        }

        public void Reset()
        {
            Calls = 0;
            TotalTicks = 0;
            MaxTicks = 0;
        }
    }

    /// <summary>Queue depths sampled when a report is built.</summary>
    internal readonly record struct PerfGauges(
        int WorkerQueue,
        int Patches,
        int PendingSteamIds,
        int Shadows,
        int SaveQueue,
        int SavesInFlight);

    /// <summary>
    /// Handler timings while <c>development.enable</c> is on. Callers take <see cref="Start"/> and hand it to
    /// <see cref="Stop(PerfCounter, long)"/>: with <see cref="Enabled"/> off neither reads the clock nor looks a
    /// counter up. Counters are created once per name and only reset afterwards. Game thread only.
    /// </summary>
    internal sealed class PerfCounters
    {
        /// <summary>Rows of <c>challenges perf</c>.</summary>
        public const int ReportRows = 15;

        /// <summary>Mirrors <c>development.enable</c>; written when the config is applied.</summary>
        public static bool Enabled;

        public static PerfCounters Global { get; } = new();

        public static readonly PerfCounter HudTick = Global.Counter("CustomHud.OnTick");
        public static readonly PerfCounter HudTransmit = Global.Counter("CustomHud.OnCheckTransmit");
        public static readonly PerfCounter DrainPatches = Global.Counter("ProgressPipeline.DrainPatches");
        public static readonly PerfCounter SavePump = Global.Counter("PlayerSaveQueue.Pump");

        private readonly Dictionary<string, PerfCounter> _byName = new(StringComparer.Ordinal);

        /// <summary>Clock reading to pass to <c>Stop</c>; 0 (nothing measured) while disabled.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Start() => Enabled ? Stopwatch.GetTimestamp() : 0;

        /// <summary>Adds the time since <paramref name="start"/>; nothing when <see cref="Start"/> returned 0.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Stop(PerfCounter counter, long start)
        {
            if (start != 0)
            {
                counter.Add(Stopwatch.GetTimestamp() - start);
            }
        }

        /// <summary>Same for an event or listener name; the counter is looked up only when measured.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Stop(string name, long start)
        {
            if (start != 0)
            {
                Global.Counter(name).Add(Stopwatch.GetTimestamp() - start);
            }
        }

        public static double ToMicroseconds(long ticks) => ticks * 1_000_000.0 / Stopwatch.Frequency;

        /// <summary>The counter for <paramref name="name"/>; created on first use and kept across resets.</summary>
        public PerfCounter Counter(string name)
        {
            if (!_byName.TryGetValue(name, out PerfCounter? counter))
            {
                _byName[name] = counter = new PerfCounter(name);
            }

            return counter;
        }

        /// <summary>Zeroes every counter in place, so references held by callers stay valid.</summary>
        public void Reset()
        {
            foreach (PerfCounter counter in _byName.Values)
            {
                counter.Reset();
            }
        }

        /// <summary>Counters with at least one call, by total time descending, then name; at most <paramref name="count"/>.</summary>
        public List<PerfCounter> Top(int count)
        {
            List<PerfCounter> used = [];
            foreach (PerfCounter counter in _byName.Values)
            {
                if (counter.Calls > 0)
                {
                    used.Add(counter);
                }
            }

            used.Sort(static (a, b) =>
            {
                int byTotal = b.TotalTicks.CompareTo(a.TotalTicks);
                return byTotal != 0 ? byTotal : string.CompareOrdinal(a.Name, b.Name);
            });
            if (used.Count > count)
            {
                used.RemoveRange(count, used.Count - count);
            }

            return used;
        }

        /// <summary>
        /// Report lines: a header, one row per counter of <see cref="Top"/> (calls, average and maximum µs, total
        /// ms), then the gauges when known.
        /// </summary>
        public List<string> Report(int rows, PerfGauges? gauges)
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            List<PerfCounter> top = Top(rows);
            List<string> lines = new(top.Count + 3)
            {
                string.Format(culture, "[Challenges] perf: top {0} by total time", rows),
                string.Format(culture, "{0,-36} {1,10} {2,10} {3,10} {4,10}", "name", "calls", "avg us", "max us", "total ms"),
            };
            if (top.Count == 0)
            {
                lines.Add("(no samples)");
            }

            foreach (PerfCounter counter in top)
            {
                lines.Add(string.Format(
                    culture,
                    "{0,-36} {1,10} {2,10:F1} {3,10:F1} {4,10:F2}",
                    counter.Name,
                    counter.Calls,
                    ToMicroseconds(counter.TotalTicks) / counter.Calls,
                    ToMicroseconds(counter.MaxTicks),
                    ToMicroseconds(counter.TotalTicks) / 1000.0));
            }

            if (gauges is { } g)
            {
                lines.Add(string.Format(
                    culture,
                    "gauges: worker_queue={0} patches={1} pending_steamids={2} shadows={3} save_queue={4} saves_in_flight={5}",
                    g.WorkerQueue,
                    g.Patches,
                    g.PendingSteamIds,
                    g.Shadows,
                    g.SaveQueue,
                    g.SavesInFlight));
            }

            return lines;
        }
    }
}
