using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Platform.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestDoubles;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Tests.Acceptance.CycleTimeOutliers
{
    /// <summary>
    /// When the Cycle Time process behaviour chart has no limits for the date range, the scatterplot sets no Work Item
    /// apart. The chart has no limits when its baseline is invalid (the configured baseline, or without one the selected
    /// range, is shorter than 14 days) or holds fewer than 2 closed Work Items with a Cycle Time. There is no fallback to
    /// 5 x the median: the Work Items of 5, 5, 5 and 90 days would set the 90-day one apart under that rule.
    ///
    /// The Team and Portfolio metrics services are asked as the product builds them, over a database of the scenario's
    /// own, with no web host.
    /// </summary>
    [TestFixture]
    public class CycleTimeOutliersWithoutProcessLimitsTest
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        private static readonly DateTime RangeStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime TenDaysLater = RangeStart.AddDays(10);

        private static readonly DateTime FourteenDaysLater = RangeStart.AddDays(14);

        private static readonly DateTime ThirtyDaysLater = RangeStart.AddDays(28);

        private static readonly DateTime BaselineStart = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Average 20.5, average moving range 25/7: the upper limit is exactly 30 days.</summary>
        private static readonly int[] BaselineWithALimitOf30 = [18, 13, 12, 24, 26, 25, 25, 21];

        /// <summary>Under 5 x the median (5 days) the 90-day Work Item would be set apart.</summary>
        private static readonly int[] OneLongWorkItemAmongShortOnes = [5, 5, 5, 90];

        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const int ImplementationToDone = 4711;
        private const int UnknownCycleTimeDefinition = 9999;

        private ServicePortHarness harness = null!;
        private Mock<IFeatureRepository> featuresOfThePortfolio = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            featuresOfThePortfolio = new Mock<IFeatureRepository>();
            harness = new ServicePortHarness(RegisterTheMetricsAsTheProductBuildsThem);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
        }

        [Test]
        public void A_10_day_range_without_a_configured_baseline_has_no_limits_so_no_Work_Item_is_set_apart()
        {
            var team = GivenATeamWithoutAConfiguredBaseline();
            GivenClosedWorkItemsInTheRange(team, OneLongWorkItemAmongShortOnes);

            var outliers = WhenTheOutliersAreAskedFor(team, TenDaysLater, definitionId: null);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        [Test]
        public void A_configured_baseline_of_10_days_has_no_limits_even_with_enough_closed_Work_Items_in_it()
        {
            var team = GivenATeamWithAConfiguredBaseline(BaselineStart, BaselineStart.AddDays(10));
            GivenClosedWorkItemsInTheBaseline(team, BaselineWithALimitOf30);
            GivenClosedWorkItemsInTheRange(team, 12, 31, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, ThirtyDaysLater, definitionId: null);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        [Test]
        public void A_30_day_range_with_a_single_closed_Work_Item_has_no_limits()
        {
            var team = GivenATeamWithoutAConfiguredBaseline();
            GivenClosedWorkItemsInTheRange(team, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, ThirtyDaysLater, definitionId: null);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        [Test]
        public void A_configured_baseline_without_any_closed_Work_Item_has_no_limits_so_no_Work_Item_of_the_range_is_set_apart()
        {
            var team = GivenATeamWithAConfiguredBaseline(BaselineStart, BaselineStart.AddDays(24));
            GivenClosedWorkItemsInTheRange(team, OneLongWorkItemAmongShortOnes);

            var outliers = WhenTheOutliersAreAskedFor(team, ThirtyDaysLater, definitionId: null);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        [Test]
        public void A_14_day_range_with_2_closed_Work_Items_has_limits()
        {
            var team = GivenATeamWithoutAConfiguredBaseline();
            GivenClosedWorkItemsInTheRange(team, 10, 12);

            var outliers = WhenTheOutliersAreAskedFor(team, FourteenDaysLater, definitionId: null);

            ThenThereAreLimits(outliers);
        }

        [Test]
        public void An_unknown_named_Cycle_Time_has_no_limits_even_when_the_default_Cycle_Time_has_them()
        {
            var team = GivenATeamWithAConfiguredBaseline(BaselineStart, BaselineStart.AddDays(24));
            GivenClosedWorkItemsInTheBaseline(team, BaselineWithALimitOf30);
            GivenClosedWorkItemsInTheRange(team, 12, 31, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, ThirtyDaysLater, UnknownCycleTimeDefinition);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        [Test]
        public void A_Portfolio_whose_configured_baseline_is_10_days_has_no_limits_so_no_Feature_is_set_apart()
        {
            var portfolio = GivenAPortfolioWithAConfiguredBaselineOf10DaysAndClosedFeatures(BaselineWithALimitOf30, 12, 31, 90);

            var outliers = WhenThePortfoliosOutliersAreAskedFor(portfolio, ThirtyDaysLater);

            ThenThereAreNoLimitsAndNoOutliers(outliers);
        }

        private Team GivenATeamWithoutAConfiguredBaseline()
        {
            return GivenATeam(null, null);
        }

        private Team GivenATeamWithAConfiguredBaseline(DateTime start, DateTime end)
        {
            return GivenATeam(start, end);
        }

        private Team GivenATeam(DateTime? baselineStart, DateTime? baselineEnd)
        {
            using var scope = harness.NewScope();
            var teams = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            var team = new Team
            {
                Name = "Ocean Explorer",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Name = "Ocean Explorer tracker",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
                DoneItemsCutoffDays = 0,
                ToDoStates = [Backlog],
                DoingStates = [Implementation],
                DoneStates = [Done],
                ProcessBehaviourChartBaselineStartDate = baselineStart,
                ProcessBehaviourChartBaselineEndDate = baselineEnd,
                CycleTimeDefinitions =
                [
                    new CycleTimeDefinition
                    {
                        Id = ImplementationToDone,
                        Name = "Implementation to Done",
                        StartState = Implementation,
                        EndState = Done,
                    },
                ],
            };
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();
            return team;
        }

        private void GivenClosedWorkItemsInTheBaseline(Team team, params int[] cycleTimes)
        {
            using var scope = harness.NewScope();
            AddClosedWorkItems(scope.ServiceProvider, team, BaselineStart.AddDays(1), cycleTimes);
        }

        private void GivenClosedWorkItemsInTheRange(Team team, params int[] cycleTimes)
        {
            using var scope = harness.NewScope();
            AddClosedWorkItems(scope.ServiceProvider, team, RangeStart.AddDays(1), cycleTimes);
        }

        /// <summary>One Work Item per Cycle Time, closed a day apart in the order given.</summary>
        private static void AddClosedWorkItems(IServiceProvider services, Team team, DateTime firstClosedOn, int[] cycleTimes)
        {
            var workItems = services.GetRequiredService<IWorkItemRepository>();
            var transitions = services.GetRequiredService<IWorkItemStateTransitionRepository>();

            for (var index = 0; index < cycleTimes.Length; index++)
            {
                var closedOn = firstClosedOn.AddDays(index).AddHours(10);
                var startedOn = closedOn.Date.AddDays(1 - cycleTimes[index]).AddHours(9);
                var item = new WorkItem
                {
                    TeamId = team.Id,
                    ReferenceId = $"OE-{firstClosedOn:MMdd}-{index}",
                    Name = $"Closed after {cycleTimes[index]} days",
                    Type = "Story",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    Url = null,
                    CreatedDate = startedOn.AddDays(-1),
                    StartedDate = startedOn,
                    ClosedDate = closedOn,
                    Order = $"{index:D3}",
                };
                workItems.Add(item);
                workItems.Save().GetAwaiter().GetResult();

                transitions.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = startedOn });
                transitions.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Implementation, ToState = Done, TransitionedAt = closedOn });
            }

            transitions.Save().GetAwaiter().GetResult();
        }

        private Portfolio GivenAPortfolioWithAConfiguredBaselineOf10DaysAndClosedFeatures(int[] baselineCycleTimes, params int[] shownCycleTimes)
        {
            var portfolio = new Portfolio
            {
                Id = 31,
                Name = "Ocean Explorer roadmap",
                DoneItemsCutoffDays = 0,
                ProcessBehaviourChartBaselineStartDate = BaselineStart,
                ProcessBehaviourChartBaselineEndDate = BaselineStart.AddDays(10),
            };
            var closedFeatures = ClosedFeatures(BaselineStart.AddDays(1), 1, baselineCycleTimes)
                .Concat(ClosedFeatures(RangeStart.AddDays(1), 501, shownCycleTimes))
                .ToList();
            featuresOfThePortfolio.Setup(repository => repository.DoneFeaturesOfPortfolio(portfolio.Id)).Returns(closedFeatures);
            return portfolio;
        }

        /// <summary>One Feature per Cycle Time, closed a day apart in the order given; ids count up from the first id.</summary>
        private static IEnumerable<Feature> ClosedFeatures(DateTime firstClosedOn, int firstId, int[] cycleTimes)
        {
            return cycleTimes.Select((days, index) =>
            {
                var closedOn = firstClosedOn.AddDays(index).AddHours(10);
                var startedOn = closedOn.Date.AddDays(1 - days).AddHours(9);
                return new Feature
                {
                    Id = firstId + index,
                    ReferenceId = $"OE-F{firstId + index}",
                    Name = $"Feature closed after {days} days",
                    Type = "Epic",
                    State = Done,
                    StateCategory = StateCategories.Done,
                    CreatedDate = startedOn.AddDays(-1),
                    StartedDate = startedOn,
                    ClosedDate = closedOn,
                };
            });
        }

        private OutlierVerdict WhenTheOutliersAreAskedFor(Team team, DateTime rangeEnd, int? definitionId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().GetCycleTimeOutliers(team, RangeStart, rangeEnd, definitionId);
        }

        private OutlierVerdict WhenThePortfoliosOutliersAreAskedFor(Portfolio portfolio, DateTime rangeEnd)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>().GetCycleTimeOutliers(portfolio, RangeStart, rangeEnd, null);
        }

        private static void ThenThereAreNoLimitsAndNoOutliers(OutlierVerdict verdict)
        {
            Assert.That(verdict, Is.TypeOf<OutlierVerdict.NoLimits>(),
                $"WHAT: the verdict is {verdict} although the process behaviour chart has no limits for this range. " +
                "WHY: without limits no Work Item may be set apart, and the reader must be told the limits are missing. " +
                "HOW: answer NoLimits when the baseline is invalid (shorter than 14 days, or an unknown named Cycle Time) or holds fewer than 2 positive Cycle Times; never fall back to 5 x the median.");
        }

        private static void ThenThereAreLimits(OutlierVerdict verdict)
        {
            Assert.That(verdict, Is.TypeOf<OutlierVerdict.Limited>(),
                $"WHAT: the verdict is {verdict} although the 14-day range holds 2 closed Work Items. " +
                "WHY: a baseline of at least 14 days with 2 positive Cycle Times gives the process behaviour chart its limits. " +
                "HOW: answer NoLimits only below 14 days or below 2 baseline Cycle Times.");
        }

        private void RegisterTheMetricsAsTheProductBuildsThem(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Now, TimeZoneInfo.Utc));
            services.AddSingleton<Cache<string, object>>();
            services.AddScoped(_ => RefreshSettingsAsSeeded());
            services.AddScoped(_ => Mock.Of<IDomainEventDispatcher>());
            services.AddScoped(_ => Mock.Of<IThroughputFilter>());
            services.AddScoped(_ => Mock.Of<IForecastDataSufficiency>());
            services.AddScoped(_ => Mock.Of<IFeatureStateTransitionRepository>());
            services.AddScoped(_ => featuresOfThePortfolio.Object);

            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddSingleton<ICycleTimeOutlierDefinition, UpperNaturalProcessLimitOutlierDefinition>();
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
            services.AddScoped<IPortfolioMetricsService, PortfolioMetricsService>();
        }

        private static IAppSettingService RefreshSettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>();
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            settings.Setup(service => service.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 15 });
            return settings.Object;
        }
    }
}
