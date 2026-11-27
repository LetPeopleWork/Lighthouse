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
    /// The Within SLE widget's header compares the share within the SLE over the chosen dates with the share over the
    /// preceding dates of the same length, and compares nothing when either period is too few to judge or no SLE is
    /// set. Each scenario asks the read port of the widget, with a database of its own, no web host and no Premium licence.
    /// </summary>
    [TestFixture]
    public class WithinSleInfoVerdictsAtTheServicePortTest
    {
        private ClosedWork given = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase() => given = new ClosedWork();

        [TearDown]
        public void RemoveTheScenariosDatabase() => given.Dispose();

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_share_is_compared_with_the_preceding_period_of_the_same_length_and_reads_up_when_it_improved(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();
            given.FiveClosedInOctoberThreeOfThemWithinTenDaysTheFirstAndLastOnTheEdgesOfThePrecedingPeriod();
            given.TwoWithinTenDaysClosedJustBeforeThePrecedingPeriod();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendReads(widget, direction: "up", delta: "+12.7 pts");
        }

        [TestCase(OwnerKind.Team)]
        [TestCase(OwnerKind.Portfolio)]
        public void The_header_trend_reads_down_when_the_share_fell_since_the_preceding_period(OwnerKind ownerKind)
        {
            given.AnOwnerWithTheSle(ownerKind, probability: 70, days: 10);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();
            given.FiveClosedInOctoberAllWithinTenDays();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendReads(widget, direction: "down", delta: "-27.3 pts");
        }

        [Test]
        public void The_header_trend_compares_nothing_when_the_preceding_period_is_too_few_to_judge()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();
            given.FourClosedInOctober();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendComparesNothing(widget);
        }

        [Test]
        public void The_header_trend_compares_nothing_when_the_chosen_period_is_too_few_to_judge()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 70, days: 10);
            given.FourClosedInNovemberOfThreeDaysEach();
            given.FiveClosedInOctoberAllWithinTenDays();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendComparesNothing(widget);
        }

        [Test]
        public void The_header_trend_compares_nothing_when_no_SLE_is_set()
        {
            given.AnOwnerWithTheSle(OwnerKind.Team, probability: 0, days: 0);
            given.ElevenClosedInNovemberEightOfThemWithinTenDays();
            given.FiveClosedInOctoberAllWithinTenDays();

            var widget = given.TheWidgetForNovember();

            ClosedWork.ThenTheHeaderTrendComparesNothing(widget);
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

            /// <summary>Cycle Times of 1, 2, 3, 4, 5, 6, 10 and 10 days are within a 10-day SLE; 11, 14 and 30 days are late.</summary>
            public void ElevenClosedInNovemberEightOfThemWithinTenDays()
                => ClosedInNovemberWithCycleTimes([1, 2, 3, 4, 5, 6, 10, 10, 11, 14, 30]);

            public void FourClosedInNovemberOfThreeDaysEach() => ClosedInNovemberWithCycleTimes([3, 3, 3, 3]);

            public void FiveClosedInOctoberThreeOfThemWithinTenDaysTheFirstAndLastOnTheEdgesOfThePrecedingPeriod()
            {
                ClosedOn(new DateTime(2026, 10, 2, 9, 0, 0, DateTimeKind.Utc), 2);
                ClosedOn(new DateTime(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc), 20);
                ClosedOn(new DateTime(2026, 10, 15, 9, 0, 0, DateTimeKind.Utc), 3);
                ClosedOn(new DateTime(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc), 20);
                ClosedOn(new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc), 4);
            }

            public void TwoWithinTenDaysClosedJustBeforeThePrecedingPeriod()
            {
                ClosedOn(new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc), 2);
                ClosedOn(new DateTime(2026, 9, 20, 9, 0, 0, DateTimeKind.Utc), 3);
            }

            public void FiveClosedInOctoberAllWithinTenDays()
            {
                for (var day = 3; day <= 23; day += 5)
                {
                    ClosedOn(new DateTime(2026, 10, day, 9, 0, 0, DateTimeKind.Utc), 4);
                }
            }

            public void FourClosedInOctober()
            {
                for (var day = 3; day <= 18; day += 5)
                {
                    ClosedOn(new DateTime(2026, 10, day, 9, 0, 0, DateTimeKind.Utc), 4);
                }
            }

            public void ClosedInNovemberWithCycleTimes(IReadOnlyList<int> cycleTimesInDays)
            {
                for (var index = 0; index < cycleTimesInDays.Count; index++)
                {
                    ClosedOn(new DateTime(2026, 11, 2 + (index % 27), 9, 0, 0, DateTimeKind.Utc), cycleTimesInDays[index]);
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
                        "dates of the same length (the day before the chosen start, going back as many days as the chosen period has) and read up when it rose.");
                    Assert.That(widget.Comparison.PercentageDelta, Is.EqualTo(delta),
                        $"The header trend showed a change of '{widget.Comparison.PercentageDelta}', expected '{delta}': the difference of the two shares " +
                        "in percentage points to one decimal, with its sign.");
                    Assert.That(widget.Comparison.MetricLabel, Is.EqualTo("Within SLE"),
                        $"The header trend was labelled '{widget.Comparison.MetricLabel}', expected 'Within SLE'.");
                }
            }

            public static void ThenTheHeaderTrendComparesNothing(WithinSleInfoDto widget)
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(widget.Comparison.Direction, Is.EqualTo("none"),
                        $"The header trend read '{widget.Comparison.Direction}' although one of the two periods is not judged. A share that is too few to judge " +
                        "or has no SLE cannot be compared: answer none and show no change.");
                    Assert.That(widget.Comparison.PercentageDelta, Is.Null,
                        $"A change of '{widget.Comparison.PercentageDelta}' was shown although nothing can be compared. Show no change.");
                }
            }

            private void Add(Func<IServiceScope, Action> stage)
            {
                using var scope = harness.NewScope();
                stage(scope)();
            }

            private Func<IServiceScope, Action> Closed(DateTime closedOn, int cycleTimeInDays)
                => Entered(closedOn.AddDays(-(cycleTimeInDays - 1)), closedOn);

            private Func<IServiceScope, Action> StillOpen(DateTime startedOn) => Entered(startedOn, closedOn: null);

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
