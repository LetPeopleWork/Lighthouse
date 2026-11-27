using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.Forecasting.Application;
using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Platform.Host;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Lighthouse.Backend.Tests.Acceptance.WithinSle
{
    /// <summary>
    /// The Within SLE widget's header compares the share over the chosen dates with the share over the preceding dates of
    /// the same length. When both shares are the same, the header reads flat with no change, rather than a rise or a fall.
    /// The widget's read port is asked with a database of the scenario's own, no web host and no Premium licence.
    /// </summary>
    [TestFixture]
    public class WithinSleTrendFlatAtTheServicePortTest
    {
        private ClosedWork given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new ClosedWork();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_header_trend_reads_flat_with_no_change_when_the_share_is_the_same_as_in_the_preceding_period(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.FiveClosedInOctoberAndFiveInNovemberFourOfEachWithinTenDays();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendReads(widget, direction: "flat", delta: "0.0 pts");
        }

        public enum OwnerKind
        {
            Team,
            Portfolio,
        }

        /// <summary>
        /// A Team or a Portfolio on an instance where it is 5 December 2026, with no Premium licence, and the Work Items
        /// or Features it closed on given days, each measured from the day it started to the day it closed.
        /// </summary>
        private sealed class ClosedWork : IDisposable
        {
            private static readonly DateTime FirstOfNovember = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            private static readonly DateTime LastOfNovember = new(2026, 11, 30, 0, 0, 0, DateTimeKind.Utc);

            private readonly ServicePortHarness harness = new(Register);
            private OwnerKind kind;
            private int ownerId;
            private int itemsAdded;

            public void Dispose() => harness.Dispose();

            public void AnOwnerWithTheSle(OwnerKind ownerKind, int probability, int days)
            {
                kind = ownerKind;
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
                        DoneItemsCutoffDays = 0,
                        ServiceLevelExpectationProbability = probability,
                        ServiceLevelExpectationRange = days,
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
                    DoneItemsCutoffDays = 0,
                    ServiceLevelExpectationProbability = probability,
                    ServiceLevelExpectationRange = days,
                };
                var portfolios = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
                portfolios.Add(portfolio);
                portfolios.Save().GetAwaiter().GetResult();
                ownerId = portfolio.Id;
            }

            /// <summary>
            /// Five closed in October and five in November, each period with Cycle Times of 2, 3, 4, 5 and 20 days: four of
            /// five within a 10-day SLE in both, a share of 80 % in both.
            /// </summary>
            public void FiveClosedInOctoberAndFiveInNovemberFourOfEachWithinTenDays()
            {
                int[] cycleTimes = [2, 3, 4, 5, 20];
                for (var index = 0; index < cycleTimes.Length; index++)
                {
                    ClosedOn(new DateTime(2026, 10, 5 + (5 * index), 9, 0, 0, DateTimeKind.Utc), cycleTimes[index]);
                    ClosedOn(new DateTime(2026, 11, 5 + (5 * index), 9, 0, 0, DateTimeKind.Utc), cycleTimes[index]);
                }
            }

            public void ClosedOn(DateTime closedOn, int cycleTimeInDays) => Add(Closed(closedOn, cycleTimeInDays));

            public WithinSleInfoDto TheWidgetForNovember()
            {
                using var scope = harness.NewScope();
                var widget = scope.ServiceProvider.GetRequiredService<IWithinSleInfoQuery>();

                return kind == OwnerKind.Team
                    ? widget.GetForTeam(scope.ServiceProvider.GetRequiredService<IRepository<Team>>().GetById(ownerId)!, FirstOfNovember, LastOfNovember)
                    : widget.GetForPortfolio(scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>().GetById(ownerId)!, FirstOfNovember, LastOfNovember);
            }

            public static void ThenTheHeaderTrendReads(WithinSleInfoDto widget, string direction, string delta)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Comparison.Direction, Is.EqualTo(direction),
                        $"The header trend read '{widget.Comparison.Direction}', expected '{direction}'. Compare the share with the share over the preceding " +
                        "dates of the same length (the day before the chosen start, going back as many days as the chosen period has) and read up when it rose, down when it fell and flat when it is the same.");
                    Assert.That(widget.Comparison.PercentageDelta, Is.EqualTo(delta),
                        $"The header trend showed a change of '{widget.Comparison.PercentageDelta}', expected '{delta}': the difference of the two shares " +
                        "in percentage points to one decimal, with its sign.");
                    Assert.That(widget.Comparison.MetricLabel, Is.EqualTo("Within SLE"),
                        $"The header trend was labelled '{widget.Comparison.MetricLabel}', expected 'Within SLE'.");
                }
            }

            private void Add(Func<IServiceScope, Action> stage)
            {
                using var scope = harness.NewScope();
                stage(scope)();
            }

            private Func<IServiceScope, Action> Closed(DateTime closedOn, int cycleTimeInDays)
                => Entered(closedOn.AddDays(-(cycleTimeInDays - 1)), closedOn);

            private Func<IServiceScope, Action> Entered(DateTime startedOn, DateTime? closedOn)
            {
                var reference = $"ORCA-{++itemsAdded}";
                var isClosed = closedOn.HasValue;

                if (kind == OwnerKind.Team)
                {
                    return scope => () =>
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
                        }, startedOn, closedOn, isClosed));
                        items.Save().GetAwaiter().GetResult();
                    };
                }

                return scope => () =>
                {
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
                    features.Add(Dated(feature, startedOn, closedOn, isClosed));
                    features.Save().GetAwaiter().GetResult();
                };
            }

            private static TItem Dated<TItem>(TItem item, DateTime startedOn, DateTime? closedOn, bool isClosed)
                where TItem : WorkItemBase
            {
                item.State = isClosed ? "Done" : "In Progress";
                item.StateCategory = isClosed ? StateCategories.Done : StateCategories.Doing;
                item.CreatedDate = startedOn.AddDays(-3);
                item.StartedDate = startedOn;
                item.ClosedDate = closedOn;
                return item;
            }

            private static void Register(IServiceCollection services)
            {
                services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(new DateTimeOffset(2026, 12, 5, 12, 0, 0, TimeSpan.Zero)));
                services.AddSingleton(TimeProvider.System);
                services.AddScoped(_ => NoPremiumLicence());
                services.AddScoped(_ => RefreshSettingsOfTeamsAndPortfolios());
                services.AddSingleton<Cache<string, object>>();
                services.AddSingleton<IDomainEventDispatcher, DomainEventDispatcher>();

                services.AddScoped<IRepository<Team>, TeamRepository>();
                services.AddScoped<IPortfolioRepository, PortfolioRepository>();
                services.AddScoped<IRepository<Portfolio>>(provider => provider.GetRequiredService<IPortfolioRepository>());
                services.AddScoped<IFeatureRepository, FeatureRepository>();
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
                services.AddScoped<ITeamMetricsService, TeamMetricsService>();
                services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
                services.AddScoped<IWithinSleInfoQuery, WithinSleInfoQuery>();
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
    }
}
