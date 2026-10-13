using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace Lighthouse.Backend.Tests.API.Integration.Reports
{
    /// <summary>
    /// How long creating a Then &amp; Now report with a 365-day Then takes on a large Team, through the real create
    /// address over a real database, and how long opening that report takes afterwards. A create computes every
    /// capture inside the request, so it is the one interaction whose cost grows with the Team's history, and it is
    /// judged against a budget of ten seconds.
    ///
    /// Two numbers per create: the wall clock of the POST as the caller saw it, and the ElapsedMs the server put on
    /// its "report created" log line, which is the number an operator reads in the field.
    ///
    /// Deliberately not an assertion on time: a wall clock taken on one machine says nothing about another, and a
    /// bound checked in from a developer machine would fail CI for something that is not a defect.
    /// </summary>
    [TestFixture]
    [Category("epic-5878-baseline")]
    public class ReportCreationTimingProbe : ReportsAcceptanceTest
    {
        private const string RunByHand = "Measures wall clock. Run by hand, on one machine.";

        private const int HistoryDays = 400;

        private const int FinishedPerDay = 5;

        private const int InProgress = 150;

        private const int ThenLengthDays = 365;

        private const int Readings = 3;

        private WebApplicationFactory<Program> recordingHost = null!;

        private HttpClient plainClient = null!;

        private readonly CreationTimes creationTimes = new();

        [SetUp]
        public void RecordTheCreationLogLine()
        {
            recordingHost = Factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILoggerFactory>();
                services.AddSingleton<ILoggerFactory>(_ => new SerilogLoggerFactory(
                    new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .WriteTo.Sink(creationTimes)
                        .CreateLogger(),
                    dispose: true));
            }));

            plainClient = Client;
            Client = recordingHost.CreateClient();
        }

        [TearDown]
        public void DisposeTheRecordingHost()
        {
            Client.Dispose();
            Client = plainClient;
            recordingHost.Dispose();
        }

        // @probe @us-01
        [Test]
        [Explicit(RunByHand)]
        public async Task Creating_a_365_day_report_on_a_large_Team_stays_within_ten_seconds()
        {
            TheInstanceHasAPremiumLicence();
            var team = ATeam(Lightspeed, keepsFinishedItemsForDays: HistoryDays);
            GivenAYearAndMoreOfHistory(team);
            TheCallerAdministers(team);

            Report($"Team: {HistoryDays * FinishedPerDay} finished Work Items over {HistoryDays} days, {InProgress} in progress");

            await MeasureOneCreate(team, "warm-up (first create after start, pays for compiling the code)");

            var readings = new List<double>();
            for (var reading = 1; reading <= Readings; reading++)
            {
                readings.Add(await MeasureOneCreate(team, $"reading {reading}"));
            }

            Assert.That(readings, Has.Count.EqualTo(Readings));
        }

        private void GivenAYearAndMoreOfHistory(OwnerUnderTest team)
        {
            var firstDay = TodayOnTheInstance.AddDays(-(HistoryDays - 1));
            var finished = Enumerable.Range(0, HistoryDays)
                .SelectMany(offset => Enumerable.Range(0, FinishedPerDay)
                    .Select(slot => new Finished(firstDay.AddDays(offset), 1 + ((offset + slot * 7) % 30))))
                .ToArray();
            TheTeamFinished(team, finished);

            for (var startedDaysAgo = 1; startedDaysAgo <= 30; startedDaysAgo++)
            {
                TheTeamWorksOn(team, InProgress / 30, TodayOnTheInstance.AddDays(-startedDaysAgo * 4));
            }
        }

        private async Task<double> MeasureOneCreate(OwnerUnderTest team, string label)
        {
            var loadBefore = LoadAverage();
            creationTimes.Clear();

            var createClock = Stopwatch.StartNew();
            using var created = await CreateAReport(team, new NewReport
            {
                Name = $"Then & Now — {label}",
                ThenLengthDays = ThenLengthDays,
                NowLengthDays = ThenLengthDays,
            });
            createClock.Stop();
            var reportId = await ThenItWasCreated(created);

            var openClock = Stopwatch.StartNew();
            using var opened = await AskForTheReport(team, reportId);
            openClock.Stop();

            var loggedMs = creationTimes.Last;
            var thenP85 = ThenValueIn(await TheReport(team, reportId), CycleTime, P85);
            Report($"{label}: POST {createClock.Elapsed.TotalMilliseconds:F0} ms, logged ElapsedMs {loggedMs:F0} ms, " +
                   $"open (GET) {openClock.Elapsed.TotalMilliseconds:F0} ms answering {(int)opened.StatusCode}, " +
                   $"Then's 85th percentile {thenP85.Value} days over {thenP85.SampleSize} finished Work Items, " +
                   $"load average before {loadBefore}, after {LoadAverage()}");

            return loggedMs ?? createClock.Elapsed.TotalMilliseconds;
        }

        private static string LoadAverage()
            => File.Exists("/proc/loadavg")
                ? string.Join(' ', File.ReadAllText("/proc/loadavg").Split(' ').Take(3))
                : "unknown";

        private static void Report(string line) => TestContext.Progress.WriteLine($"[create timing] {line}");

        /// <summary>The ElapsedMs of the last "report created" line, read off the structured property rather than the text.</summary>
        private sealed class CreationTimes : ILogEventSink
        {
            private readonly Lock gate = new();

            private double? last;

            public double? Last
            {
                get
                {
                    lock (gate)
                    {
                        return last;
                    }
                }
            }

            public void Clear()
            {
                lock (gate)
                {
                    last = null;
                }
            }

            public void Emit(LogEvent logEvent)
            {
                if (logEvent.Properties.TryGetValue("ElapsedMs", out var elapsed)
                    && logEvent.Properties.ContainsKey("ThenLengthDays")
                    && elapsed is ScalarValue { Value: var value })
                {
                    lock (gate)
                    {
                        last = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                    }
                }
            }
        }
    }
}
