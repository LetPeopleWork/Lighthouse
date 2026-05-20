using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    internal static class FixtureSetupTimer
    {
        private static readonly ConcurrentDictionary<string, FixtureStats> Stats = new();

        public static bool Enabled =>
            Environment.GetEnvironmentVariable("LIGHTHOUSE_FIXTURE_TIMING") == "1";

        public static IDisposable? Measure(string fixtureName, MeasurementKind kind)
        {
            if (!Enabled)
            {
                return null;
            }

            return new Scope(fixtureName, kind);
        }

        public static string WriteReport(string outputPath)
        {
            var rows = Stats
                .Select(kvp => new
                {
                    Fixture = kvp.Key,
                    kvp.Value.SetUpCount,
                    SetUpTotalMs = kvp.Value.SetUpTotalMs,
                    SetUpMeanMs = kvp.Value.SetUpCount == 0 ? 0 : kvp.Value.SetUpTotalMs / kvp.Value.SetUpCount,
                    TearDownTotalMs = kvp.Value.TearDownTotalMs,
                    OneTimeSetUpMs = kvp.Value.OneTimeSetUpMs,
                    OneTimeTearDownMs = kvp.Value.OneTimeTearDownMs,
                })
                .OrderByDescending(r => r.SetUpTotalMs)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("fixture,setup_count,setup_total_ms,setup_mean_ms,teardown_total_ms,one_time_setup_ms,one_time_teardown_ms");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0},{1},{2:F1},{3:F2},{4:F1},{5:F1},{6:F1}",
                    r.Fixture, r.SetUpCount, r.SetUpTotalMs, r.SetUpMeanMs,
                    r.TearDownTotalMs, r.OneTimeSetUpMs, r.OneTimeTearDownMs));
            }

            File.WriteAllText(outputPath, sb.ToString());
            return sb.ToString();
        }

        public enum MeasurementKind
        {
            OneTimeSetUp,
            SetUp,
            TearDown,
            OneTimeTearDown,
        }

        private sealed class FixtureStats
        {
            private long setUpTotalTicks;
            private long tearDownTotalTicks;
            private long oneTimeSetUpTicks;
            private long oneTimeTearDownTicks;
            private int setUpCount;

            public int SetUpCount => setUpCount;
            public double SetUpTotalMs => TicksToMs(setUpTotalTicks);
            public double TearDownTotalMs => TicksToMs(tearDownTotalTicks);
            public double OneTimeSetUpMs => TicksToMs(oneTimeSetUpTicks);
            public double OneTimeTearDownMs => TicksToMs(oneTimeTearDownTicks);

            public void Record(MeasurementKind kind, long ticks)
            {
                switch (kind)
                {
                    case MeasurementKind.SetUp:
                        Interlocked.Add(ref setUpTotalTicks, ticks);
                        Interlocked.Increment(ref setUpCount);
                        break;
                    case MeasurementKind.TearDown:
                        Interlocked.Add(ref tearDownTotalTicks, ticks);
                        break;
                    case MeasurementKind.OneTimeSetUp:
                        Interlocked.Add(ref oneTimeSetUpTicks, ticks);
                        break;
                    case MeasurementKind.OneTimeTearDown:
                        Interlocked.Add(ref oneTimeTearDownTicks, ticks);
                        break;
                }
            }

            private static double TicksToMs(long ticks) =>
                ticks * 1000.0 / Stopwatch.Frequency;
        }

        private sealed class Scope : IDisposable
        {
            private readonly string fixtureName;
            private readonly MeasurementKind kind;
            private readonly long startTicks;

            public Scope(string fixtureName, MeasurementKind kind)
            {
                this.fixtureName = fixtureName;
                this.kind = kind;
                startTicks = Stopwatch.GetTimestamp();
            }

            public void Dispose()
            {
                var elapsed = Stopwatch.GetTimestamp() - startTicks;
                var stats = Stats.GetOrAdd(fixtureName, _ => new FixtureStats());
                stats.Record(kind, elapsed);
            }
        }
    }
}
