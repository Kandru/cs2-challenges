using System.Diagnostics;

namespace Challenges.Tests
{
    /// <summary>
    /// <see cref="PlayerSaveQueue"/> against a temp directory: budgeted hand-off, the synchronous flush and the
    /// flush after the background writer missed its join (C0.1).
    /// </summary>
    public sealed class SaveQueueTests : IDisposable
    {
        private static readonly TimeSpan Patience = TimeSpan.FromSeconds(10);

        private readonly string _directory = Path.Combine(Path.GetTempPath(), $"challenges-tests-{Guid.NewGuid():N}");
        private readonly PlayerArchive _archive = new();
        private readonly List<string> _warnings = [];

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }

        [Fact]
        public void Pump_hands_off_within_the_budget_and_FlushSync_writes_the_rest()
        {
            PluginConfig config = new();
            config.Persistence.AutosavePerTick = 1;
            PlayerSaveQueue saves = Queue(config);
            PlayerState a = Dirty(1, "a");
            PlayerState b = Dirty(2, "b");

            saves.Enqueue(a);
            saves.Enqueue(b);
            saves.Enqueue(a);
            Assert.Equal(2, saves.Queued);

            saves.Pump();
            Assert.Equal(1, saves.Queued);
            Assert.False(a.Dirty);
            Assert.True(b.Dirty);
            WaitUntil(() => saves.InFlight == 0);
            Assert.Equal("a", OnDisk(a));

            saves.FlushSync();
            Assert.Equal("b", OnDisk(b));
            Assert.False(b.Dirty);

            // Closed: later saves are ignored.
            a.Dirty = true;
            saves.Enqueue(a);
            saves.Pump();
            Assert.Equal(0, saves.Queued);
            Assert.Empty(TempFiles());
            Assert.Empty(_warnings);
        }

        [Fact]
        public void FlushSync_with_a_stalled_writer_shares_no_temp_file_and_ends_with_the_state()
        {
            using Stall stall = new();
            PlayerSaveQueue saves = Queue(new PluginConfig(), stall);
            PlayerState a = Dirty(1, "a");
            PlayerState b = Dirty(2, "b");

            saves.Enqueue(a);
            saves.Pump();
            string stalledTemp = stall.WaitForTemp();
            // Handed off behind the stalled write; the writer never gets to it.
            saves.Enqueue(b);
            saves.Pump();
            Assert.Equal(2, saves.InFlight);

            saves.FlushSync();

            Assert.Contains("player save writer did not finish in time", _warnings);
            Assert.Equal("b", OnDisk(b));
            // The synchronous writes never touched the stalled write's temp file.
            Assert.Equal(stall.Bytes, File.ReadAllBytes(stalledTemp));
            Assert.Equal(0, saves.InFlight);

            stall.Release();
            WaitUntil(() => !File.Exists(stalledTemp));
            Assert.Equal("a", OnDisk(a));
            Assert.Equal("b", OnDisk(b));
            Assert.Empty(TempFiles());
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Stalled_write_never_lands_over_a_newer_synchronous_save(bool handedOffAgain)
        {
            using Stall stall = new();
            PlayerSaveQueue saves = Queue(new PluginConfig(), stall);
            PlayerState a = Dirty(1, "old");
            saves.Enqueue(a);
            saves.Pump();
            string stalledTemp = stall.WaitForTemp();

            // Progress after the hand-off: queued again, or already handed off behind the stalled write.
            a.Username = "new";
            a.Dirty = true;
            saves.Enqueue(a);
            if (handedOffAgain)
            {
                saves.Pump();
            }

            saves.FlushSync();
            Assert.Equal("new", OnDisk(a));
            Assert.False(a.Dirty);

            stall.Release();
            WaitUntil(() => !File.Exists(stalledTemp));
            Assert.Equal("new", OnDisk(a));
            Assert.Empty(TempFiles());
        }

        /// <summary>A stalled writer always misses the join, so its wait is short; otherwise the default applies.</summary>
        private PlayerSaveQueue Queue(PluginConfig config, Stall? stall = null) =>
            stall == null
                ? new(_archive, config, _directory, _warnings.Add, _ => { })
                : new(_archive, config, _directory, _warnings.Add, _ => { })
                {
                    WriterJoinTimeout = TimeSpan.FromMilliseconds(100),
                    WriteTemp = stall.WriteTemp,
                };

        private PlayerState Dirty(int i, string name)
        {
            PlayerState state = _archive.GetOrCreate(TestData.SteamId(i));
            state.Username = name;
            state.Dirty = true;
            return state;
        }

        /// <summary>User name in the player's file.</summary>
        private string? OnDisk(PlayerState state) =>
            PlayerFiles.DeserializeBytes(File.ReadAllBytes(PlayerFiles.FilePath(_directory, state.SteamId)))?.Username;

        private string[] TempFiles() =>
            Directory.Exists(_directory) ? Directory.GetFiles(_directory, "*.tmp") : [];

        private static void WaitUntil(Func<bool> condition)
        {
            Stopwatch watch = Stopwatch.StartNew();
            while (!condition())
            {
                Assert.True(watch.Elapsed < Patience, "condition not reached in time");
                Thread.Sleep(5);
            }
        }

        /// <summary>Stalls the first background write after its temp file is written, until released.</summary>
        private sealed class Stall : IDisposable
        {
            private readonly ManualResetEventSlim _written = new();
            private readonly ManualResetEventSlim _release = new();
            private int _calls;
            private string? _temp;

            /// <summary>Bytes of the stalled write.</summary>
            public byte[] Bytes { get; private set; } = [];

            public string WriteTemp(string path, byte[] bytes, long version)
            {
                string temp = PlayerFiles.WriteTemp(path, bytes, version);
                if (Interlocked.Increment(ref _calls) == 1)
                {
                    Bytes = bytes;
                    _temp = temp;
                    _written.Set();
                    _release.Wait(Patience);
                }

                return temp;
            }

            public string WaitForTemp()
            {
                Assert.True(_written.Wait(Patience), "the writer never started");
                return _temp!;
            }

            public void Release() => _release.Set();

            /// <summary>Never leaves the writer stalled, even when the test fails early.</summary>
            public void Dispose() => _release.Set();
        }
    }
}
