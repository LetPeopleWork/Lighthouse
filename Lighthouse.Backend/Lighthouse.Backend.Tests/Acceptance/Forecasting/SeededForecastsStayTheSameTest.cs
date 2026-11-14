using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Dependencies;
using Lighthouse.Backend.Services.Implementation.DomainEvents;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Dependencies;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Acceptance.Forecasting
{
    /// <summary>
    /// Moving the forecasting code to another place must not change a single forecast. With the random numbers
    /// pinned, the same history gives the same percentiles every time, so the percentiles are written out here
    /// as numbers: a changed draw, a changed percentile reading or a changed input turns them red, whatever the
    /// code is called and wherever it lives.
    ///
    /// The forecasts are asked of the forecast service as the product builds it, over a database of the
    /// scenario's own, with no web host. The runs are few on purpose: with many runs, neighbouring results are
    /// mostly equal, and a percentile read one run too early or too late would go unseen.
    /// </summary>
    [TestFixture]
    public class SeededForecastsStayTheSameTest
    {
        private const int PinnedRandomNumberSeed = 20_240_917;

        private const long PinnedDrawStartingNumber = 7_310_492_118;

        private const int RunsPerForecast = 20;

        private const int CoresTheInstanceIsGiven = 4;

        private const int SamplingWindowDays = 14;

        private static readonly int[] TheFourPercentiles = [50, 70, 85, 95];

        private static readonly DateTimeOffset Now = new(2026, 10, 18, 9, 30, 0, TimeSpan.Zero);

        private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

        /// <summary>Work Items finished on each of the last fourteen days, oldest first, today last.</summary>
        private static readonly int[] AnUnevenFortnight = [3, 0, 1, 2, 0, 4, 1, 0, 2, 5, 0, 1, 3, 0];

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            harness = new ServicePortHarness(RegisterTheForecastAsTheProductBuildsIt);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
        }

        [Test]
        public void How_many_Work_Items_fit_in_thirty_days_reads_the_same_percentiles_from_the_same_history()
        {
            var history = GivenAThroughputHistoryOf(AnUnevenFortnight);

            var forecast = WhenAskedHowManyWorkItemsFitIn(history, days: 30);

            ThenTheForecastReads(forecast, fiftieth: 53, seventieth: 49, eightyFifth: 44, ninetyFifth: 42);
        }

        [Test]
        public async Task When_twenty_Work_Items_are_done_reads_the_same_percentiles_from_the_same_history()
        {
            var team = GivenATeamThatFinished(AnUnevenFortnight);

            var forecast = await WhenAskedWhenWorkItemsAreDone(team, remainingWorkItems: 20);

            ThenTheForecastReads(forecast, fiftieth: 13, seventieth: 16, eightyFifth: 19, ninetyFifth: 22);
        }

        // --- Given ---

        private static RunChartData GivenAThroughputHistoryOf(int[] finishedPerDay)
            => new(RunChartDataGenerator.GenerateRunChartData(finishedPerDay));

        private Team GivenATeamThatFinished(int[] finishedPerDayOldestFirst)
        {
            int teamId;
            using (var scope = harness.NewScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = "Pinned forecast connection",
                    WorkTrackingSystem = WorkTrackingSystems.Csv,
                };
                dbContext.WorkTrackingSystemConnections.Add(connection);
                dbContext.SaveChanges();

                var team = new Team
                {
                    Name = "Pinned forecast Team",
                    WorkTrackingSystemConnectionId = connection.Id,
                    ThroughputHistory = SamplingWindowDays,
                };
                dbContext.Teams.Add(team);
                dbContext.SaveChanges();
                teamId = team.Id;

                dbContext.WorkItems.AddRange(FinishedWorkItems(teamId, finishedPerDayOldestFirst));
                dbContext.SaveChanges();
            }

            using var readScope = harness.NewScope();
            var stored = readScope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(teamId);
            Assert.That(stored, Is.Not.Null,
                $"The Team the forecast is asked for was not stored (id {teamId}). The scenario cannot ask a forecast for a Team that does not exist; check the set-up writes it.");

            return stored!;
        }

        private static IEnumerable<WorkItem> FinishedWorkItems(int teamId, int[] finishedPerDayOldestFirst)
        {
            var oldestDay = Today.AddDays(1 - finishedPerDayOldestFirst.Length);
            var number = 0;

            for (var dayIndex = 0; dayIndex < finishedPerDayOldestFirst.Length; dayIndex++)
            {
                var closedOn = oldestDay.AddDays(dayIndex);
                for (var item = 0; item < finishedPerDayOldestFirst[dayIndex]; item++)
                {
                    number++;
                    yield return new WorkItem
                    {
                        ReferenceId = $"PIN-{number}",
                        Name = $"Finished Work Item {number}",
                        Type = "User Story",
                        State = "Done",
                        StateCategory = StateCategories.Done,
                        TeamId = teamId,
                        Url = string.Empty,
                        ParentReferenceId = string.Empty,
                        Order = $"{number}",
                        StartedDate = closedOn.AddDays(-3).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
                        ClosedDate = closedOn.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc),
                    };
                }
            }
        }

        // --- When ---

        private HowManyForecast WhenAskedHowManyWorkItemsFitIn(RunChartData history, int days)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IForecastService>().HowMany(history, days);
        }

        private async Task<WhenForecast> WhenAskedWhenWorkItemsAreDone(Team team, int remainingWorkItems)
        {
            using var scope = harness.NewScope();
            return await scope.ServiceProvider.GetRequiredService<IForecastService>().When(team, remainingWorkItems);
        }

        // --- Then ---

        private static void ThenTheForecastReads(ForecastBase forecast, int fiftieth, int seventieth, int eightyFifth, int ninetyFifth)
        {
            var read = TheFourPercentiles.Select(forecast.GetProbability).ToArray();
            int[] expected = [fiftieth, seventieth, eightyFifth, ninetyFifth];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forecast.TotalTrials, Is.EqualTo(RunsPerForecast),
                    $"The forecast counted {forecast.TotalTrials} runs, not the {RunsPerForecast} it was set to simulate. Every simulated run counts towards the percentiles, so a run lost or added shifts them; the simulation must run exactly the runs its limits give it.");
                Assert.That(read, Is.EqualTo(expected),
                    $"The 50th, 70th, 85th and 95th percentiles read [{string.Join(", ", read)}], not [{string.Join(", ", expected)}]. The random numbers are pinned and the history is the same, so a forecast that changes here changed what the product forecasts: a draw, the way a percentile is read off the runs, or the history the forecast is given. Find the change and undo it; a behaviour-free move must leave every forecast as it was.");
            }
        }

        // --- The forecast as the product builds it, with its random numbers pinned ---

        private static void RegisterTheForecastAsTheProductBuildsIt(IServiceCollection services)
        {
            var clock = new FakeLighthouseClock(Now);
            services.AddSingleton<ILighthouseClock>(clock);
            services.AddSingleton<TimeProvider>(new Microsoft.Extensions.Time.Testing.FakeTimeProvider(Now));
            services.AddScoped(_ => PortDoubles.PremiumLicence());
            services.AddScoped(_ => PortDoubles.SettingsAsSeeded());
            services.AddSingleton<Cache<string, object>>();
            services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IFeatureOrdering, FeatureOrdering>();
            services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            services.AddScoped<IPortfolioRepository, PortfolioRepository>();
            services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            services.AddScoped<IForecastFilterRuleService, ForecastFilterRuleService>();
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            services.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            services.AddScoped<IDependencyDecision, DependencyDecision>();
            services.AddScoped<IWhatTheForecastWaitsFor, WhatTheForecastWaitsFor>();

            services.AddSingleton<IRandomNumberService>(new SeededRandomNumberService(PinnedRandomNumberSeed));
            services.AddSingleton<IDrawStreamFactory>(new DrawsFromAPinnedStartingNumber(PinnedDrawStartingNumber));
            services.AddSingleton(new ForecastSimulationLimits(RunsPerForecast, ForecastSimulationLimits.Default.MostDaysOneSimulatedRunMayCover)
            {
                CoresSeen = CoresTheInstanceIsGiven,
            });
            services.AddSingleton<ForecastSimulation>();
            services.AddScoped<IForecastService, ForecastService>();
        }
    }
}
