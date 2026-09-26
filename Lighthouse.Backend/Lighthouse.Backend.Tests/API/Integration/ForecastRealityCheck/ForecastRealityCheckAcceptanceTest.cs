using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace Lighthouse.Backend.Tests.API.Integration.ForecastRealityCheck
{
    /// <summary>
    /// Feature-wide acceptance harness for the Forecast Reality Check: one press replays a Team's own
    /// forecast against its own finished work at every sampling window and every horizon, and answers
    /// with a region of windows that behaved alike rather than a winner.
    ///
    /// The app is the production app: the real ASP.NET host over a real SQLite file, real EF, real DI,
    /// the real metrics service reading real finished Work Items. The clock is pinned, because every
    /// check ends today and a drifting today would move every date these scenarios name. The licence is
    /// granted, because it is external.
    ///
    /// The forecast is the one thing a scenario usually scripts, and it is scripted per check rather than
    /// replaced wholesale. A scenario about which levels held has to choose the forecast a check produced,
    /// or it is asserting sampling noise; and the simulation is also the one cost that grows several times
    /// over under the coverage run CI uses. Where a scenario is about the check running end to end, it asks
    /// for the shipped engine instead, with its starting number pinned and fewer runs than production uses.
    ///
    /// Nothing here names a type of the check's own code. Every scenario talks to the check over the wire
    /// and reads the answer as JSON, so the scenarios pin what a caller receives, and a change that breaks
    /// that fails on the answer rather than on the build.
    /// </summary>
    public abstract class ForecastRealityCheckAcceptanceTest
    {
        protected const string LatestRoute = "/api/latest/forecast/reality-check/";

        protected const string VersionedRoute = "/api/v1/forecast/reality-check/";

        protected const string NoOptions = "{}";

        /// <summary>The Team has no rolling window for the check to test, so its setting sits neither inside nor outside anything.</summary>
        protected const string NotTested = "NotTested";

        /// <summary>A level's reading when no check could be evaluated at all.</summary>
        protected const string NotEvaluated = "NotEvaluated";

        /// <summary>Why a setting was not tested: the Team forecasts from a fixed pair of dates. Wins when both reasons apply.</summary>
        protected const string UsesFixedDates = "UsesFixedDates";

        /// <summary>Why a setting was not tested: the stored window is zero days or fewer.</summary>
        protected const string NotAPositiveLength = "NotAPositiveLength";

        /// <summary>
        /// The sampling windows every Team is checked against, whatever its own setting is.
        /// </summary>
        protected static readonly int[] StandardWindowDays = [14, 30, 60, 90];

        /// <summary>One, two, four and eight weeks, each ending today.</summary>
        protected static readonly int[] HorizonDays = [7, 14, 28, 56];

        protected static readonly int[] ConfidenceLevels = [50, 70, 85, 95];

        /// <summary>
        /// The day the instance believes it is, for every scenario. Arbitrary; that it never moves is the
        /// point, since every check this feature makes is measured back from it.
        /// </summary>
        protected static readonly DateTimeOffset Today = new(2026, 9, 22, 9, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// The forecast value a level is given when the scenario wants it NOT to hold. No scenario seeds
        /// anywhere near this many finished Work Items in one period, so the Team can never reach it.
        /// </summary>
        private const int MoreThanAnyTeamHereDelivers = 500;

        /// <summary>Far fewer runs than production's ten thousand; a scenario needs a spread, not precision.</summary>
        private const int SimulatedRunsWhenTheEngineRuns = 1_000;

        private const long PinnedStartingNumber = 4172;

        private TestWebApplicationFactory<Program> rootFactory = null!;

        protected ForecastScript Forecasts { get; private set; } = null!;

        protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected static DateOnly TodayDay => DateOnly.FromDateTime(Today.UtcDateTime);

        /// <summary>How far one scripted check held: from none of its four levels up to all four.</summary>
        public enum HeldUpTo
        {
            NoLevel,
            NinetyFive,
            EightyFive,
            Seventy,
            EveryLevel,
        }

        [SetUp]
        public void StartTheApplication()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            Forecasts = new ForecastScript();
            var script = Forecasts;

            Factory = TestWebApplicationFactory<Program>
                .WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<ILighthouseClock>();
                        services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Today));

                        var license = new Mock<ILicenseService>();
                        license.Setup(service => service.CanUsePremiumFeatures()).Returns(true);
                        services.RemoveAll<ILicenseService>();
                        services.AddSingleton(license.Object);

                        services.RemoveAll<IDrawStreamFactory>();
                        services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(PinnedStartingNumber));
                        services.RemoveAll<ForecastSimulationLimits>();
                        services.AddSingleton(new ForecastSimulationLimits(
                            SimulatedRunsWhenTheEngineRuns,
                            ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover));

                        services.RemoveAll<IForecastService>();
                        services.AddScoped<ForecastService>();
                        services.AddScoped<IForecastService>(provider =>
                            new ForecastWithScriptedChecks(provider.GetRequiredService<ForecastService>(), script));
                    });
                });

            Client = Factory.CreateClient();

            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in scope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void StopTheApplication()
        {
            using (var scope = Factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            Client.Dispose();
            Factory.Dispose();
            rootFactory.Dispose();
        }

        // --- Teams and their finished work ---

        protected int GivenATeam(string name, int samplingWindowDays)
        {
            var teamId = SeedTeam(name, team => team.ThroughputHistory = samplingWindowDays);
            Forecasts.AlsoSweeps(samplingWindowDays);
            return teamId;
        }

        /// <summary>
        /// A Team that forecasts from a fixed pair of dates. Its sampling window length is still stored,
        /// and deliberately off the standard ladder, so a check that wrongly added it would show a fifth
        /// window rather than hiding the mistake behind a standard one.
        /// </summary>
        protected int GivenATeamForecastingFromFixedDates(string name, int inertSamplingWindowDays)
        {
            return SeedTeam(name, team =>
            {
                team.ThroughputHistory = inertSamplingWindowDays;
                team.UseFixedDatesForThroughput = true;
                team.ThroughputHistoryStartDate = TodayDay.AddDays(-200).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                team.ThroughputHistoryEndDate = TodayDay.AddDays(-100).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            });
        }

        protected int GivenAnotherTeam(string name) => SeedTeam(name, _ => { });

        /// <summary>One finished Work Item on every day, today included, reaching back far enough for every check.</summary>
        protected void GivenTheTeamFinishedAWorkItemEveryDay(int teamId) => GivenTheTeamFinishedAWorkItemEvery(teamId, 1);

        /// <summary>
        /// Work finished in bursts: one Work Item every <paramref name="days"/> days. Every fifth day leaves
        /// a two-week window holding two to four days with finished work - below the bar of five - while a
        /// thirty-day window holds six or more.
        /// </summary>
        protected void GivenTheTeamFinishedAWorkItemEvery(int teamId, int days)
        {
            const int FurtherBackThanTheLongestCheckReaches = 160;

            var closedOn = new List<DateOnly>();
            for (var daysAgo = 0; daysAgo < FurtherBackThanTheLongestCheckReaches; daysAgo += days)
            {
                closedOn.Add(TodayDay.AddDays(-daysAgo));
            }

            GivenTheTeamFinishedWorkItemsOn(teamId, closedOn);
        }

        protected void GivenTheTeamFinishedWorkItemsOn(int teamId, List<DateOnly> days)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var existing = dbContext.WorkItems.Count(item => item.TeamId == teamId);
            var items = days.Select((day, index) => new WorkItem
            {
                ReferenceId = $"RC-{teamId}-{existing + index + 1}",
                Name = $"Finished Work Item {existing + index + 1}",
                Type = "User Story",
                State = "Done",
                StateCategory = StateCategories.Done,
                TeamId = teamId,
                Url = string.Empty,
                ParentReferenceId = string.Empty,
                Order = $"{existing + index + 1}",
                StartedDate = day.AddDays(-3).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
                ClosedDate = day.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
            });

            dbContext.WorkItems.AddRange(items);
            dbContext.SaveChanges();
        }

        // --- Asking ---

        protected async Task<HttpResponseMessage> WhenTheCheckIsRunBy(Func<HttpClient, HttpClient> identity, int teamId, string body = NoOptions, string route = LatestRoute)
        {
            identity(Client);
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            return await Client.PostAsync($"{route}{teamId}", content);
        }

        protected Task<HttpResponseMessage> WhenSomebodyWhoCanReadTheTeamRunsTheCheck(int teamId, string body = NoOptions, string route = LatestRoute)
            => WhenTheCheckIsRunBy(client => client.AsTeamViewer(teamId, "maria-santos"), teamId, body, route);

        protected async Task<RealityCheckAnswer> TheAnswerTo(int teamId)
        {
            using var response = await WhenSomebodyWhoCanReadTheTeamRunsTheCheck(teamId);
            return await ReadTheAnswer(response);
        }

        /// <summary>
        /// Every scenario reads the answer through here, so a check that did not answer fails on this assertion
        /// rather than on a JSON parser choking on an empty body. It is also where every check is held to
        /// learning from exactly its own window: no scenario here has a blackout day, so a history one day
        /// longer or shorter than a swept window means a check read days that were not its own.
        /// </summary>
        protected async Task<RealityCheckAnswer> ReadTheAnswer(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    $"The reality check did not answer. Body: {body}");
                Assert.That(Forecasts.HistoriesNoSweptWindowIsAsLongAs, Is.Empty,
                    "every check learns from exactly as many days as its window holds; these history lengths match no window swept");
            }

            using var document = JsonDocument.Parse(body);
            return new RealityCheckAnswer(document.RootElement.Clone());
        }

        // --- Reading what is stored ---

        protected TeamAsStored WhatIsStoredFor(int teamId)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            var team = dbContext.Teams.AsNoTracking().Single(candidate => candidate.Id == teamId);
            var workItems = dbContext.WorkItems.AsNoTracking()
                .Where(item => item.TeamId == teamId)
                .Select(item => $"{item.ReferenceId}|{item.State}|{item.ClosedDate:O}")
                .ToList();

            return new TeamAsStored(
                team.Name,
                team.ThroughputHistory,
                team.UseFixedDatesForThroughput,
                team.ThroughputHistoryStartDate,
                team.ThroughputHistoryEndDate,
                team.FeatureWIP,
                string.Join(";", workItems.Order(StringComparer.Ordinal)),
                dbContext.Teams.Count());
        }

        protected sealed record TeamAsStored(
            string Name,
            int SamplingWindowDays,
            bool UsesFixedDates,
            DateTime? FixedStart,
            DateTime? FixedEnd,
            int FeatureWip,
            string WorkItems,
            int TeamsInTheInstance);

        private int SeedTeam(string name, Action<Team> configure)
        {
            using var scope = Factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            var connection = new WorkTrackingSystemConnection
            {
                Name = $"{name} connection",
                WorkTrackingSystem = WorkTrackingSystems.Csv,
            };
            dbContext.WorkTrackingSystemConnections.Add(connection);
            dbContext.SaveChanges();

            var team = new Team { Name = name, WorkTrackingSystemConnectionId = connection.Id };
            configure(team);
            dbContext.Teams.Add(team);
            dbContext.SaveChanges();

            return team.Id;
        }

        // --- The answer, read off the wire ---

        /// <summary>
        /// The check's answer as a caller receives it. Every field is looked up by its wire name and a
        /// missing one fails as an assertion naming the field, so a partly built answer reads as a partly
        /// built answer.
        /// </summary>
        protected sealed class RealityCheckAnswer(JsonElement root)
        {
            public JsonElement Root { get; } = root;

            public DateOnly AnchorDate => AsDay(Field(Root, "anchorDate"));

            public List<int> StandardWindowDays => Numbers(Field(Root, "standardWindowDays"));

            public List<int> SampledWindowDays => Numbers(Field(Root, "sampledWindowDays"));

            public List<int> SampledHorizonDays => Numbers(Field(Root, "sampledHorizonDays"));

            public List<int> ConfidenceLevels => Numbers(Field(Root, "confidenceLevels"));

            public int MinimumActiveDays => Field(Root, "minimumActiveDays").GetInt32();

            public int RunsAttempted => Field(Field(Root, "denominator"), "runsAttempted").GetInt32();

            public int RunsEvaluated => Field(Field(Root, "denominator"), "runsEvaluated").GetInt32();

            public int LevelsPerRun => Field(Field(Root, "denominator"), "levelsPerRun").GetInt32();

            public int ScoresEvaluated => Field(Field(Root, "denominator"), "scoresEvaluated").GetInt32();

            public List<int> SoundWindowDays => Numbers(Field(Field(Root, "soundWindow"), "soundWindowDays"));

            public List<int> UnevaluatedWindowDays => Numbers(Field(Field(Root, "soundWindow"), "unevaluatedWindowDays"));

            public string Determination => Text(Field(Field(Root, "soundWindow"), "determination"));

            public string? CurrentSettingNotTestedReason
            {
                get
                {
                    var reason = Field(Field(Root, "soundWindow"), "currentSettingNotTestedReason");
                    return reason.ValueKind == JsonValueKind.Null ? null : Text(reason);
                }
            }

            public int CurrentSettingDays => Field(Field(Root, "soundWindow"), "currentSettingDays").GetInt32();

            public bool CurrentSettingWasTested => Field(Field(Root, "soundWindow"), "currentSettingWasTested").GetBoolean();

            public string CurrentSettingStanding => Text(Field(Field(Root, "soundWindow"), "currentSettingStanding"));

            public List<CellReading> Cells =>
                [.. Field(Root, "cells").EnumerateArray().Select(CellReading.From)];

            public CellReading Cell(int samplingWindowDays, int horizonDays)
            {
                var matching = Cells
                    .Where(cell => cell.SamplingWindowDays == samplingWindowDays && cell.HorizonDays == horizonDays)
                    .ToList();

                Assert.That(matching, Has.Count.EqualTo(1),
                    $"expected exactly one check for the {samplingWindowDays}-day window over {horizonDays} days");

                return matching[0];
            }

            public List<CellReading> CellsFor(int samplingWindowDays)
                => [.. Cells.Where(cell => cell.SamplingWindowDays == samplingWindowDays)];

            public LevelCoverageReading Coverage(int confidenceLevel)
            {
                var matching = Field(Root, "levelCoverage").EnumerateArray()
                    .Select(LevelCoverageReading.From)
                    .Where(coverage => coverage.ConfidenceLevel == confidenceLevel)
                    .ToList();

                Assert.That(matching, Has.Count.EqualTo(1),
                    $"expected exactly one line for the {confidenceLevel}% level");

                return matching[0];
            }

            private static List<int> Numbers(JsonElement array)
                => array.ValueKind == JsonValueKind.Array ? [.. array.EnumerateArray().Select(number => number.GetInt32())] : [];
        }

        protected sealed record CellReading(
            int HorizonDays,
            int SamplingWindowDays,
            DateOnly ScoredPeriodStart,
            DateOnly ScoredPeriodEnd,
            DateOnly HistoryWindowStart,
            DateOnly HistoryWindowEnd,
            bool IsSufficient,
            string Reason,
            int DaysWithCompletedWork,
            Dictionary<int, int> ForecastByLevel,
            int? ActualCompleted,
            string? Outcome,
            Dictionary<int, bool> HeldByLevel)
        {
            public static CellReading From(JsonElement cell)
            {
                var sufficiency = Field(cell, "sufficiency");
                var forecast = Field(cell, "forecast");
                var levelOutcomes = Field(cell, "levelOutcomes");
                var actual = Field(cell, "actualCompleted");
                var outcome = Field(cell, "outcome");

                return new CellReading(
                    Field(cell, "horizonDays").GetInt32(),
                    Field(cell, "samplingWindowDays").GetInt32(),
                    AsDay(Field(cell, "scoredPeriodStart")),
                    AsDay(Field(cell, "scoredPeriodEnd")),
                    AsDay(Field(cell, "historyWindowStart")),
                    AsDay(Field(cell, "historyWindowEnd")),
                    Field(sufficiency, "isSufficient").GetBoolean(),
                    Text(Field(sufficiency, "reason")),
                    Field(sufficiency, "daysWithCompletedWork").GetInt32(),
                    forecast.ValueKind == JsonValueKind.Array
                        ? forecast.EnumerateArray().ToDictionary(
                            level => Field(level, "probability").GetInt32(),
                            level => Field(level, "value").GetInt32())
                        : [],
                    actual.ValueKind == JsonValueKind.Number ? actual.GetInt32() : null,
                    outcome.ValueKind == JsonValueKind.String ? outcome.GetString() : null,
                    levelOutcomes.ValueKind == JsonValueKind.Array
                        ? levelOutcomes.EnumerateArray().ToDictionary(
                            level => Field(level, "confidenceLevel").GetInt32(),
                            level => Field(level, "held").GetBoolean())
                        : []);
            }
        }

        protected sealed record LevelCoverageReading(int ConfidenceLevel, int HeldCount, double ExpectedHeldCount, string Reading)
        {
            public static LevelCoverageReading From(JsonElement line) => new(
                Field(line, "confidenceLevel").GetInt32(),
                Field(line, "heldCount").GetInt32(),
                Field(line, "expectedHeldCount").GetDouble(),
                Text(Field(line, "reading")));
        }

        protected static JsonElement Field(JsonElement element, string name)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value))
            {
                return value;
            }

            Assert.Fail($"The answer carries no '{name}' where the contract puts one: {element}");
            return default;
        }

        private static string Text(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetInt32().ToString(CultureInfo.InvariantCulture),
            _ => element.ToString(),
        };

        private static DateOnly AsDay(JsonElement element)
            => DateOnly.ParseExact(element.GetString() ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        // --- The scripted forecast ---

        /// <summary>
        /// Which forecast each check produces, chosen by the scenario. A check is recognised by the length
        /// of the history it was handed - exactly its sampling window - and by the number of days it forecast -
        /// its horizon. Nothing else about how the check asks is assumed. Every history length handed over is
        /// kept, the shipped engine's included, so a scenario can tell a check that read a day too many.
        /// </summary>
        protected sealed class ForecastScript
        {
            private readonly Dictionary<(int Window, int Horizon), HeldUpTo?> checks = [];

            private readonly SortedSet<int> windows = [.. StandardWindowDays];

            private readonly List<int> historyLengthsHandedOver = [];

            private readonly Lock handingOver = new();

            private bool shippedEngine;

            /// <summary>How every check the scenario did not script behaves: the 95% and 85% levels hold.</summary>
            public HeldUpTo Otherwise { get; set; } = HeldUpTo.EightyFive;

            public void AlsoSweeps(int samplingWindowDays)
            {
                if (samplingWindowDays > 0)
                {
                    windows.Add(samplingWindowDays);
                }
            }

            public void Check(int samplingWindowDays, int horizonDays, HeldUpTo held) => checks[(samplingWindowDays, horizonDays)] = held;

            public void EveryCheckOf(int samplingWindowDays, HeldUpTo held)
            {
                foreach (var horizon in HorizonDays)
                {
                    Check(samplingWindowDays, horizon, held);
                }
            }

            /// <summary>The engine comes back with nothing it can read a level from, for this check only.</summary>
            public void CannotBeWorkedOutFor(int samplingWindowDays, int horizonDays) => checks[(samplingWindowDays, horizonDays)] = null;

            public void RunAsShipped() => shippedEngine = true;

            public List<int> HistoriesNoSweptWindowIsAsLongAs
            {
                get
                {
                    lock (handingOver)
                    {
                        return [.. historyLengthsHandedOver.Where(length => !windows.Contains(length))];
                    }
                }
            }

            public HowManyForecast? ForecastFor(RunChartData history, int days)
            {
                lock (handingOver)
                {
                    historyLengthsHandedOver.Add(history.History);
                }

                if (shippedEngine)
                {
                    return null;
                }

                var horizon = HorizonDays.MinBy(candidate => Math.Abs(candidate - days));

                if (!checks.TryGetValue((history.History, horizon), out var held))
                {
                    held = Otherwise;
                }

                return held is null
                    ? new HowManyForecast([], days)
                    : AForecastWhere(held.Value, days);
            }

            /// <summary>
            /// A simulation whose four readings are exactly the values this check was scripted to have. A
            /// level meant to hold is read at nothing, which any Team reaches; a level meant not to hold is
            /// read at more than any Team here ever delivers. Readings stay ordered from the cautious 95%
            /// up to the optimistic 50%, as the engine's own are.
            /// </summary>
            private static HowManyForecast AForecastWhere(HeldUpTo held, int days)
            {
                var at95 = held >= HeldUpTo.NinetyFive ? 0 : MoreThanAnyTeamHereDelivers;
                var at85 = held >= HeldUpTo.EightyFive ? 0 : MoreThanAnyTeamHereDelivers + 1;
                var at70 = held >= HeldUpTo.Seventy ? 0 : MoreThanAnyTeamHereDelivers + 2;
                var at50 = held >= HeldUpTo.EveryLevel ? 0 : MoreThanAnyTeamHereDelivers + 3;

                var trials = new Dictionary<int, int>();
                AddTrials(trials, at50, 50);
                AddTrials(trials, at70, 20);
                AddTrials(trials, at85, 15);
                AddTrials(trials, at95, 10);
                AddTrials(trials, Math.Max(0, at95 - 1), 5);

                return new HowManyForecast(trials, days);
            }

            private static void AddTrials(Dictionary<int, int> trials, int itemsCompleted, int count)
                => trials[itemsCompleted] = trials.GetValueOrDefault(itemsCompleted) + count;
        }

        /// <summary>
        /// The shipped forecast engine with one change: a check the scenario scripted gets the scripted
        /// forecast instead of a simulated one. Every other forecast the application makes runs as shipped.
        /// </summary>
        private sealed class ForecastWithScriptedChecks(IForecastService shipped, ForecastScript script) : IForecastService
        {
            public Task UpdateForecastsForPortfolio(Portfolio portfolio) => shipped.UpdateForecastsForPortfolio(portfolio);

            public HowManyForecast HowMany(RunChartData throughput, int days)
                => script.ForecastFor(throughput, days) ?? shipped.HowMany(throughput, days);

            public HowManyForecast PredictWorkItemCreation(Team team, string[] workItemTypes, DateTime startDate, DateTime endDate, int daysToForecast)
                => shipped.PredictWorkItemCreation(team, workItemTypes, startDate, endDate, daysToForecast);

            public Task<WhenForecast> When(Team team, int remainingItems, ThroughputFilterMode mode = ThroughputFilterMode.RespectTeamSetting)
                => shipped.When(team, remainingItems, mode);
        }
    }
}
