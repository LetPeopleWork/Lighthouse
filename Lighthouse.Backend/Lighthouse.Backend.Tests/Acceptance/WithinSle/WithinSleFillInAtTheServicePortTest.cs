using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics;
using Lighthouse.Backend.Modules.FlowMetrics.Adapters.Background;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.FlowMetrics.Application.Policies;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Forecasting.Contracts;
using Lighthouse.Backend.Modules.Licensing.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Modules.WorkItems.Contracts;
using Lighthouse.Backend.Platform.DatabaseManagement;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Platform.UpdateQueue;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Concurrent;

namespace Lighthouse.Backend.Tests.Acceptance.WithinSle
{
    /// <summary>
    /// Opening the Within SLE Over Time chart for a period with missing past days answers at once with what is
    /// stored, and leaves those days to the background filler that already fills Percentiles Over Time. Once the
    /// filler has caught up, each missing day the stored history reaches is in the record, marked as filled in and
    /// measured against the SLE set now, while a day recorded at the time keeps what it was recorded with. The
    /// application is composed as the product composes Flow Metrics, over a database of the scenario's own and no
    /// web host.
    /// </summary>
    [TestFixture]
    public class WithinSleFillInAtTheServicePortTest
    {
        private static readonly DateOnly FirstOfNovember = new(2026, 11, 1);
        private static readonly DateOnly SecondOfNovember = new(2026, 11, 2);
        private static readonly DateOnly ThirdOfNovember = new(2026, 11, 3);
        private static readonly DateOnly FourthOfNovember = new(2026, 11, 4);
        private static readonly DateOnly FifthOfNovember = new(2026, 11, 5);
        private static readonly DateOnly SixthOfNovember = new(2026, 11, 6);
        private static readonly DateOnly SeventhOfNovember = new(2026, 11, 7);
        private static readonly DateOnly EighthOfNovember = new(2026, 11, 8);
        private static readonly DateOnly NinthOfNovember = new(2026, 11, 9);
        private static readonly DateOnly TenthOfNovember = new(2026, 11, 10);
        private static readonly DateOnly TwentiethOfNovember = new(2026, 11, 20);

        private FillIn given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new FillIn();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void Opening_the_chart_answers_only_the_recorded_day_at_once_and_writes_nothing_while_the_reader_waits(OwnerKind ownerKind)
        {
            GivenTheFifthWasRecordedAtSixtyWithinEightAndTheSleIsNowSeventyWithinTen(ownerKind);

            var opened = given.TheChartIsOpenedFor(horizon: 30, from: FirstOfNovember, to: TenthOfNovember);

            FillIn.ThenOnlyTheseDaysAreAnswered(opened.Chart, FifthOfNovember);
            FillIn.ThenTheDayWasRecorded(opened.Chart, FifthOfNovember, closed: 5, within: 3, slePercentile: 60, sleDays: 8);
            FillIn.ThenOpeningTheChartWroteNothing(opened.Writes);
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void After_the_filler_catches_up_the_missing_past_days_are_filled_in_against_todays_SLE_and_the_recorded_day_is_kept(OwnerKind ownerKind)
        {
            GivenTheFifthWasRecordedAtSixtyWithinEightAndTheSleIsNowSeventyWithinTen(ownerKind);
            given.TheChartIsOpenedFor(horizon: 30, from: FirstOfNovember, to: TenthOfNovember);

            given.TheHistoryFillerCatchesUp();

            var chart = given.TheChartFor(horizon: 30, from: FirstOfNovember, to: TenthOfNovember);
            FillIn.ThenOnlyTheseDaysAreAnswered(chart,
                ThirdOfNovember, FourthOfNovember, FifthOfNovember, SixthOfNovember, SeventhOfNovember, EighthOfNovember, NinthOfNovember, TenthOfNovember);
            FillIn.ThenTheDayWasRecorded(chart, FifthOfNovember, closed: 5, within: 3, slePercentile: 60, sleDays: 8);
            FillIn.ThenTheDayWasFilledIn(chart, ThirdOfNovember, closed: 3, within: 3, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, FourthOfNovember, closed: 5, within: 4, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, SixthOfNovember, closed: 6, within: 5, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, SeventhOfNovember, closed: 7, within: 5, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, EighthOfNovember, closed: 7, within: 5, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, NinthOfNovember, closed: 8, within: 6, slePercentile: 70, sleDays: 10);
            FillIn.ThenTheDayWasFilledIn(chart, TenthOfNovember, closed: 8, within: 6, slePercentile: 70, sleDays: 10);
            given.ThenTheFillerReportedNoFailure();
        }

        [Test]
        public void A_day_whose_longer_window_reaches_past_the_work_still_kept_is_filled_in_only_for_the_windows_inside_it()
        {
            given.AnOwnerLastRefreshedOn(OwnerKind.Team, TwentiethOfNovember, sleProbability: 70, sleDays: 10, keepsFinishedWorkForDays: 40);
            given.ClosedFromTheThirdOfNovember();
            given.TheChartIsOpenedFor(horizon: 30, from: TenthOfNovember, to: TenthOfNovember);

            given.TheHistoryFillerCatchesUp();

            FillIn.ThenTheDayWasFilledIn(given.TheChartFor(horizon: 30, from: TenthOfNovember, to: TenthOfNovember),
                TenthOfNovember, closed: 8, within: 6, slePercentile: 70, sleDays: 10);
            FillIn.ThenOnlyTheseDaysAreAnswered(given.TheChartFor(horizon: 90, from: TenthOfNovember, to: TenthOfNovember));
            given.ThenTheFillerReportedNoFailure();
        }

        /// <summary>
        /// Closed Work Items (or Features) from the 3rd of November on, last refreshed on the 20th. The 5th was
        /// recorded by a refresh that day while the SLE was 60 % within 8 days; the SLE is now 70 % within 10 days
        /// and today is the 20th, so the 1st to the 10th, apart from the 5th, were never recorded.
        /// </summary>
        private void GivenTheFifthWasRecordedAtSixtyWithinEightAndTheSleIsNowSeventyWithinTen(OwnerKind ownerKind)
        {
            given.AnOwnerLastRefreshedOn(ownerKind, TwentiethOfNovember, sleProbability: 60, sleDays: 8);
            given.ClosedFromTheThirdOfNovember();
            given.TheDataIsRefreshedOn(FifthOfNovember);
            given.TheSleIsChangedTo(probability: 70, days: 10);
            given.TodayIs(TwentiethOfNovember);
        }

        public enum OwnerKind
        {
            Team,
            Portfolio,
        }

        /// <summary>The chart's answer, and what the database saw written while it was being worked out.</summary>
        private sealed record OpenedChart(WithinSleOverTimeDto Chart, WritesSeen Writes);

        /// <summary>
        /// A Team or a Portfolio with its closed Work Items or Features, an instance clock the scenario moves, and the
        /// Within SLE daily record as a refresh, the chart and the background filler reach it.
        /// </summary>
        private sealed class FillIn : IDisposable
        {
            private readonly FakeLighthouseClock clock = new(new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero));
            private readonly FailuresLogged failures = new();
            private readonly ServicePortHarness harness;
            private OwnerKind kind;
            private int ownerId;
            private int itemsAdded;

            public FillIn() => harness = new ServicePortHarness(Register);

            public void Dispose() => harness.Dispose();

            private OwnerType OwnerType => kind == OwnerKind.Team ? OwnerType.Team : OwnerType.Portfolio;

            /// <summary>
            /// An owner that keeps finished work for 40 days after its last refresh on the 20th still has everything from
            /// the 11th of October on: a 30-day window ending on the 10th of November lies inside that, a 90-day one does not.
            /// </summary>
            public void AnOwnerLastRefreshedOn(OwnerKind ownerKind, DateOnly lastRefreshedOn, int sleProbability, int sleDays, int keepsFinishedWorkForDays = 0)
            {
                kind = ownerKind;
                var lastRefresh = lastRefreshedOn.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
                using var scope = harness.NewScope();
                var connection = new WorkTrackingSystemConnection
                {
                    Name = $"Connection {Guid.NewGuid():N}",
                    WorkTrackingSystem = WorkTrackingSystems.Jira,
                };

                if (kind == OwnerKind.Team)
                {
                    var team = new Team
                    {
                        Name = "Team Orca",
                        WorkTrackingSystemConnection = connection,
                        DataRetrievalValue = "project = ORCA",
                        WorkItemTypes = ["User Story"],
                        ToDoStates = ["New"],
                        DoingStates = ["In Progress"],
                        DoneStates = ["Done"],
                        DoneItemsCutoffDays = keepsFinishedWorkForDays,
                        ServiceLevelExpectationProbability = sleProbability,
                        ServiceLevelExpectationRange = sleDays,
                        UpdateTime = lastRefresh,
                    };
                    var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                    teams.Add(team);
                    teams.Save().GetAwaiter().GetResult();
                    ownerId = team.Id;
                    return;
                }

                var portfolio = new Portfolio
                {
                    Name = "Portfolio Orca",
                    WorkTrackingSystemConnection = connection,
                    DataRetrievalValue = "project = ORCA",
                    ToDoStates = ["New"],
                    DoingStates = ["In Progress"],
                    DoneStates = ["Done"],
                    DoneItemsCutoffDays = keepsFinishedWorkForDays,
                    ServiceLevelExpectationProbability = sleProbability,
                    ServiceLevelExpectationRange = sleDays,
                    UpdateTime = lastRefresh,
                };
                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                portfolios.Add(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            /// <summary>
            /// Nothing closed before the 3rd of November. Closed on the 3rd with Cycle Times of 2, 4 and 9 days, on
            /// the 4th with 12 and 6, on the 6th with 10, on the 7th with 15 and on the 9th with 8 days.
            /// </summary>
            public void ClosedFromTheThirdOfNovember()
            {
                ClosedOn(ThirdOfNovember, 2, 4, 9);
                ClosedOn(FourthOfNovember, 12, 6);
                ClosedOn(SixthOfNovember, 10);
                ClosedOn(SeventhOfNovember, 15);
                ClosedOn(NinthOfNovember, 8);
            }

            public void TodayIs(DateOnly day) => clock.SetInstant(new DateTimeOffset(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

            public void TheDataIsRefreshedOn(DateOnly day)
            {
                TodayIs(day);
                using var scope = harness.NewScope();

                if (kind == OwnerKind.Team)
                {
                    foreach (var handler in scope.ServiceProvider.GetServices<IDomainEventHandler<TeamDataRefreshed>>())
                    {
                        handler.HandleAsync(new TeamDataRefreshed(ownerId), CancellationToken.None).GetAwaiter().GetResult();
                    }

                    return;
                }

                foreach (var handler in scope.ServiceProvider.GetServices<IDomainEventHandler<PortfolioFeaturesRefreshed>>())
                {
                    handler.HandleAsync(new PortfolioFeaturesRefreshed(ownerId), CancellationToken.None).GetAwaiter().GetResult();
                }
            }

            public void TheSleIsChangedTo(int probability, int days)
            {
                using var scope = harness.NewScope();

                if (kind == OwnerKind.Team)
                {
                    var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
                    var team = teams.GetById(ownerId)!;
                    team.ServiceLevelExpectationProbability = probability;
                    team.ServiceLevelExpectationRange = days;
                    teams.Update(team);
                    teams.Save().GetAwaiter().GetResult();
                    return;
                }

                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                var portfolio = portfolios.GetById(ownerId)!;
                portfolio.ServiceLevelExpectationProbability = probability;
                portfolio.ServiceLevelExpectationRange = days;
                portfolios.Update(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
            }

            public OpenedChart TheChartIsOpenedFor(int horizon, DateOnly from, DateOnly to)
            {
                WithinSleOverTimeDto chart = null!;
                var writes = harness.Writes.During(() => chart = TheChartFor(horizon, from, to));

                return new OpenedChart(chart, writes);
            }

            public WithinSleOverTimeDto TheChartFor(int horizon, DateOnly from, DateOnly to)
            {
                using var scope = harness.NewScope();

                return scope.ServiceProvider.GetRequiredService<IWithinSleOverTimeSeriesQuery>().GetSeries(ownerId, OwnerType, horizon, from, to);
            }

            /// <summary>Works through every day the filler was asked for, as the running application does in the background.</summary>
            public void TheHistoryFillerCatchesUp()
            {
                using var scope = harness.NewScope();
                scope.ServiceProvider.GetRequiredService<OverTimeHistoryFiller>().DrainAsync(CancellationToken.None).GetAwaiter().GetResult();
            }

            public static void ThenOpeningTheChartWroteNothing(WritesSeen writes)
                => Assert.That(writes.StatementsThatWrote, Is.Empty,
                    $"Opening the chart wrote to the database: [{string.Join(" | ", writes.StatementsThatWrote)}]. The chart answers while " +
                    "the reader waits, so it only reads what is stored and hands the missing days to the background filler, which writes them.");

            public void ThenTheFillerReportedNoFailure()
                => Assert.That(failures.Messages, Is.Empty,
                    $"The background filler reported failures: [{string.Join(" | ", failures.Messages)}]. One pass fills every chart of the " +
                    "owner, and a pass that fails leaves its days missing for the next visit; it must get through every day of this range.");

            public static void ThenTheDayWasRecorded(WithinSleOverTimeDto chart, DateOnly day, int closed, int within, int slePercentile, int sleDays)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Origin.ToString(), Is.EqualTo("Recorded"),
                        $"{day} reads as '{answered.Origin}'. It was recorded by a refresh on that day, and filling in the days around it " +
                        "must leave it as it was recorded.");
                    Assert.That((answered.ClosedCount, answered.WithinCount), Is.EqualTo((closed, within)),
                        $"{day} counts {answered.WithinCount} of {answered.ClosedCount} within the SLE, expected {within} of {closed}: the counts " +
                        "it was recorded with against the SLE in force that day, not worked out again against today's SLE.");
                    Assert.That((answered.SlePercentile, answered.SleDays), Is.EqualTo((slePercentile, sleDays)),
                        $"{day} is held to {answered.SlePercentile} % within {answered.SleDays} days, expected {slePercentile} % within {sleDays} days, " +
                        "the SLE in force when it was recorded. Filling in other days must not rewrite a recorded one.");
                }
            }

            public static void ThenTheDayWasFilledIn(WithinSleOverTimeDto chart, DateOnly day, int closed, int within, int slePercentile, int sleDays)
            {
                var answered = TheDay(chart, day);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(answered.Origin.ToString(), Is.EqualTo("FilledIn"),
                        $"{day} reads as '{answered.Origin}'. Nobody recorded it at the time; it was worked out afterwards from the stored " +
                        "history, so mark it filled in for the chart to draw it differently.");
                    Assert.That((answered.ClosedCount, answered.WithinCount), Is.EqualTo((closed, within)),
                        $"{day} counts {answered.WithinCount} of {answered.ClosedCount} within the SLE, expected {within} of {closed}: the Work " +
                        "Items closed in the 30 days up to that day, measured against today's SLE, never carried over from a neighbouring day.");
                    Assert.That((answered.SlePercentile, answered.SleDays), Is.EqualTo((slePercentile, sleDays)),
                        $"{day} is held to {answered.SlePercentile} % within {answered.SleDays} days, expected the SLE set when it was filled in, " +
                        $"{slePercentile} % within {sleDays} days.");
                }
            }

            public static void ThenOnlyTheseDaysAreAnswered(WithinSleOverTimeDto chart, params DateOnly[] days)
                => Assert.That(chart.Days.Select(answered => answered.RecordedAt), Is.EquivalentTo(days),
                    $"The chart answered the days [{string.Join(", ", chart.Days.Select(answered => answered.RecordedAt))}], expected " +
                    $"[{string.Join(", ", days)}]. A window reaching back past the finished work the owner still keeps is not filled in, so " +
                    "it shows a gap rather than a count of partly deleted work. Every past day the stored history reaches is in the record once the filler has caught up, " +
                    "and a day before the first closed Work Item is left out, so the chart draws no point there rather than a share of 0.");

            private static WithinSleDayDto TheDay(WithinSleOverTimeDto chart, DateOnly day)
            {
                var matching = chart.Days.Where(answered => answered.RecordedAt == day).ToList();
                Assert.That(matching, Has.Count.EqualTo(1),
                    $"The chart answered {matching.Count} entries for {day}, expected exactly one. " +
                    $"Days answered: [{string.Join(", ", chart.Days.Select(answered => answered.RecordedAt))}].");
                return matching[0];
            }

            private void ClosedOn(DateOnly day, params int[] cycleTimesInDays)
            {
                foreach (var cycleTimeInDays in cycleTimesInDays)
                {
                    using var scope = harness.NewScope();
                    Closed(scope, day.ToDateTime(new TimeOnly(9, 0), DateTimeKind.Utc), cycleTimeInDays);
                }
            }

            private void Closed(IServiceScope scope, DateTime closedOn, int cycleTimeInDays)
            {
                var reference = $"ORCA-{++itemsAdded}";
                var startedOn = closedOn.AddDays(-(cycleTimeInDays - 1));

                if (kind == OwnerKind.Team)
                {
                    var team = scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)!;
                    var items = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
                    items.Add(Dated(new WorkItem
                    {
                        Team = team,
                        TeamId = team.Id,
                        ReferenceId = reference,
                        Name = $"Story {reference}",
                        Type = "User Story",
                        Url = $"https://example.test/items/{reference}",
                        Order = reference,
                    }, startedOn, closedOn));
                    items.Save().GetAwaiter().GetResult();
                    return;
                }

                var portfolio = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId)!;
                var feature = new Feature
                {
                    ReferenceId = reference,
                    Name = $"Feature {reference}",
                    Type = "Epic",
                    Url = $"https://example.test/features/{reference}",
                    Order = reference,
                };
                feature.Portfolios.Add(portfolio);
                var features = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
                features.Add(Dated(feature, startedOn, closedOn));
                features.Save().GetAwaiter().GetResult();
            }

            private static TItem Dated<TItem>(TItem item, DateTime startedOn, DateTime closedOn)
                where TItem : WorkItemBase
            {
                item.State = "Done";
                item.StateCategory = StateCategories.Done;
                item.CreatedDate = startedOn.AddDays(-3);
                item.StartedDate = startedOn;
                item.ClosedDate = closedOn;
                return item;
            }

            /// <summary>
            /// Flow Metrics as the product composes it, with the singletons the product's composition root adds beside
            /// it: the note of days already worked out and the database maintenance gate the filler stands down for.
            /// </summary>
            private void Register(IServiceCollection services)
            {
                services.AddSingleton<ILighthouseClock>(clock);
                services.AddSingleton<Microsoft.Extensions.Logging.ILoggerProvider>(failures);
                services.AddSingleton(TimeProvider.System);
                services.AddScoped(_ => NoPremiumLicence());
                services.AddScoped(_ => RefreshSettingsOfTeamsAndPortfolios());
                services.AddSingleton<Cache<string, object>>();
                services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();
                services.AddSingleton<IUpdateStatusStore>(provider =>
                    new InProcessUpdateStatusStore(new(), provider.GetRequiredService<ILighthouseClock>()));
                services.AddSingleton<DatabaseMaintenanceGate>();
                services.AddSingleton<ReconstructionMemo>();

                services.AddScoped<IRepository<Team>, TeamRepository>();
                services.AddScoped<IPortfolioRepository, PortfolioRepository>();
                services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
                services.AddScoped<IFeatureRepository, FeatureRepository>();
                services.AddScoped<IRepository<Feature>>(provider => provider.GetRequiredService<IFeatureRepository>());
                services.AddScoped<IFeatureOrdering, FeatureOrdering>();
                services.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
                services.AddScoped<IRepository<OptionalFeature>, OptionalFeatureRepository>();
                services.AddScoped<IFeatureStateTransitionRepository, FeatureStateTransitionRepository>();
                services.AddScoped<IWorkItemRepository, WorkItemRepository>();
                services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
                services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
                services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
                services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
                services.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
                services.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
                services.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
                services.AddScoped<ForecastFilterRuleService>();
                services.AddScoped<IForecastFilterRuleService>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
                services.AddScoped<IThroughputFilter>(provider => provider.GetRequiredService<ForecastFilterRuleService>());
                services.AddScoped<ForecastingAnswersToFlowMetrics>();
                services.AddScoped<IForecastDataSufficiency>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());
                services.AddScoped<IHowManyForecaster>(provider => provider.GetRequiredService<ForecastingAnswersToFlowMetrics>());

                services.AddFlowMetrics();
                services.AddScoped<IDomainEventHandler<TeamDataRefreshed>, WithinSleRecordingHandler>();
                services.AddScoped<IDomainEventHandler<PortfolioFeaturesRefreshed>, WithinSleRecordingHandler>();
            }

            private static IAppSettingService RefreshSettingsOfTeamsAndPortfolios()
            {
                var refresh = new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 };
                var settings = new Mock<IAppSettingService>(MockBehavior.Strict);
                settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(refresh);
                settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(refresh);
                return settings.Object;
            }

            private static ILicenseService NoPremiumLicence()
            {
                var licence = new Mock<ILicenseService>();
                licence.Setup(service => service.CanUsePremiumFeatures()).Returns(false);
                return licence.Object;
            }
        }

        /// <summary>Every error the application logs, so a pass that failed reads as a failure rather than as days not yet filled.</summary>
        private sealed class FailuresLogged : Microsoft.Extensions.Logging.ILoggerProvider
        {
            private readonly ConcurrentQueue<string> messages = new();

            public IReadOnlyList<string> Messages => [.. messages];

            public Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName) => new Logger(messages, categoryName);

            public void Dispose()
            {
                // Holds nothing that needs releasing.
            }

            private sealed class Logger(ConcurrentQueue<string> messages, string category) : Microsoft.Extensions.Logging.ILogger
            {
                public IDisposable? BeginScope<TState>(TState state)
                    where TState : notnull => null;

                public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => logLevel >= Microsoft.Extensions.Logging.LogLevel.Error;

                public void Log<TState>(
                    Microsoft.Extensions.Logging.LogLevel logLevel,
                    Microsoft.Extensions.Logging.EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter)
                {
                    if (IsEnabled(logLevel))
                    {
                        messages.Enqueue($"{category}: {formatter(state, exception)} {exception?.Message}");
                    }
                }
            }
        }
    }
}
