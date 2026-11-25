using Lighthouse.Backend.Modules.FlowMetrics.Domain;
using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
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
using Lighthouse.Backend.Modules.OrganisationSetup.Application;

namespace Lighthouse.Backend.Tests.Acceptance.CycleTimeOutliers
{
    /// <summary>
    /// The Work Items the Cycle Time scatterplot sets apart as outliers are exactly the closed Work Items whose Cycle Time
    /// is above the upper natural process limit of the Cycle Time process behaviour chart of the same Team and date range,
    /// taken from its configured baseline and compared without rounding. A named Cycle Time is judged the same way, over
    /// the named durations.
    ///
    /// The Team and Portfolio metrics services are asked as the product builds them, over a database of the scenario's own, with no web
    /// host. Each baseline below was chosen so its limit is an exact number: 20.5 + 2.66 x 25/7 is 30 and
    /// 23 + 2.66 x 20/7 is 30.6, which the process behaviour chart shows rounded to 31.
    /// </summary>
    [TestFixture]
    public class CycleTimeOutliersFollowTheProcessBehaviourChartTest
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        private static readonly DateTime BaselineStart = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime BaselineEnd = new(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime RangeStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime RangeEnd = new(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Average 20.5, average moving range 25/7: the upper limit is exactly 30 days, the lower one 11.</summary>
        private static readonly int[] BaselineWithALimitOf30 = [18, 13, 12, 24, 26, 25, 25, 21];

        /// <summary>Average 23, average moving range 20/7: the upper limit is 30.6 days, shown as 31 on the chart.</summary>
        private static readonly int[] BaselineWithALimitOf30Point6 = [17, 23, 18, 25, 25, 25, 26, 25];

        /// <summary>Every baseline Work Item took 20 days, so the moving range is 0 and the limit is 20.</summary>
        private static readonly int[] BaselineThatNeverVaries = [20, 20, 20, 20];

        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const int ImplementationToDone = 4711;

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
        public void The_Work_Items_above_a_limit_of_30_days_are_the_outliers_and_the_process_behaviour_chart_marks_the_same_ones_above_its_limit()
        {
            var team = GivenATeamWithItsBaseline(BaselineWithALimitOf30);
            var shown = GivenClosedWorkItemsInTheRange(team, 12, 30, 31, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, definitionId: null);

            ThenTheOutliersAre(outliers, shown[31], shown[90]);
            ThenTheProcessBehaviourChartShowsAboveItsLimit(team, limit: 30, shown[31], shown[90]);
        }

        [Test]
        public void A_31_day_Work_Item_is_an_outlier_when_the_limit_is_30_point_6_days_even_though_the_chart_shows_the_limit_as_31()
        {
            var team = GivenATeamWithItsBaseline(BaselineWithALimitOf30Point6);
            var shown = GivenClosedWorkItemsInTheRange(team, 20, 30, 31);

            var outliers = WhenTheOutliersAreAskedFor(team, definitionId: null);

            ThenTheOutliersAre(outliers, shown[31]);
            ThenTheProcessBehaviourChartShowsItsLimitAs(team, 31);
        }

        [Test]
        public void When_every_baseline_Work_Item_took_the_same_time_only_the_Work_Items_longer_than_that_are_outliers()
        {
            var team = GivenATeamWithItsBaseline(BaselineThatNeverVaries);
            var shown = GivenClosedWorkItemsInTheRange(team, 19, 20, 21);

            var outliers = WhenTheOutliersAreAskedFor(team, definitionId: null);

            ThenTheOutliersAre(outliers, shown[21]);
        }

        [Test]
        public void A_Work_Item_far_below_the_lower_limit_is_not_an_outlier_although_the_chart_marks_it_as_a_large_change()
        {
            var team = GivenATeamWithItsBaseline(BaselineWithALimitOf30);
            var shown = GivenClosedWorkItemsInTheRange(team, 3, 12, 31);

            var outliers = WhenTheOutliersAreAskedFor(team, definitionId: null);

            ThenTheOutliersAre(outliers, shown[31]);
            ThenTheProcessBehaviourChartMarksAsALargeChange(team, shown[3]);
        }

        [Test]
        public void The_Features_of_a_Portfolio_above_a_limit_of_30_days_are_the_outliers_and_its_process_behaviour_chart_marks_the_same_ones()
        {
            var portfolio = GivenAPortfolioWithItsBaselineAndClosedFeatures(BaselineWithALimitOf30, 12, 30, 31, 90);

            var outliers = WhenThePortfoliosOutliersAreAskedFor(portfolio);

            ThenTheOutliersAre(outliers, 503, 504);
            ThenThePortfoliosProcessBehaviourChartShowsAboveItsLimit(portfolio, 30, 503, 504);
        }

        [Test]
        public void A_named_Cycle_Time_sets_apart_the_Work_Items_whose_named_duration_is_above_the_limit_of_the_named_durations()
        {
            var team = GivenATeamWithItsBaseline(BaselineWithALimitOf30);
            var shown = GivenClosedWorkItemsInTheRange(team, measuredFromImplementation: true, 12, 30, 31, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, definitionId: ImplementationToDone);

            ThenTheOutliersAre(outliers, shown[31], shown[90]);
        }

        private Team GivenATeamWithItsBaseline(int[] baselineCycleTimes)
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
                ProcessBehaviourChartBaselineStartDate = BaselineStart,
                ProcessBehaviourChartBaselineEndDate = BaselineEnd,
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

            AddClosedWorkItems(scope.ServiceProvider, team, BaselineStart.AddDays(1), false, baselineCycleTimes);
            return team;
        }

        private Dictionary<int, int> GivenClosedWorkItemsInTheRange(Team team, params int[] cycleTimes)
        {
            return GivenClosedWorkItemsInTheRange(team, false, cycleTimes);
        }

        private Dictionary<int, int> GivenClosedWorkItemsInTheRange(Team team, bool measuredFromImplementation, params int[] cycleTimes)
        {
            using var scope = harness.NewScope();
            return AddClosedWorkItems(scope.ServiceProvider, team, RangeStart.AddDays(1), measuredFromImplementation, cycleTimes);
        }

        /// <summary>
        /// One Work Item per Cycle Time, closed a day apart in the order given. Measured from Implementation, each Work
        /// Item sat in the Backlog for 60 days first with its started date at the start of that wait, so its default
        /// Cycle Time is 60 days longer than its named one: the baseline is not, so a rule that ignores the named Cycle Time sets every
        /// shown Work Item apart.
        /// </summary>
        private static Dictionary<int, int> AddClosedWorkItems(IServiceProvider services, Team team, DateTime firstClosedOn, bool measuredFromImplementation, int[] cycleTimes)
        {
            const int DaysInTheBacklog = 60;
            var workItems = services.GetRequiredService<IWorkItemRepository>();
            var transitions = services.GetRequiredService<IWorkItemStateTransitionRepository>();
            var idByCycleTime = new Dictionary<int, int>();

            for (var index = 0; index < cycleTimes.Length; index++)
            {
                var closedOn = firstClosedOn.AddDays(index).AddHours(10);
                var enteredImplementation = closedOn.Date.AddDays(1 - cycleTimes[index]).AddHours(9);
                var startedOn = measuredFromImplementation ? enteredImplementation.AddDays(-DaysInTheBacklog) : enteredImplementation;
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

                transitions.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Backlog, ToState = Implementation, TransitionedAt = enteredImplementation });
                transitions.Add(new WorkItemStateTransition { WorkItemId = item.Id, FromState = Implementation, ToState = Done, TransitionedAt = closedOn });
                idByCycleTime[cycleTimes[index]] = item.Id;
            }

            transitions.Save().GetAwaiter().GetResult();
            return idByCycleTime;
        }

        private OutlierVerdict WhenTheOutliersAreAskedFor(Team team, int? definitionId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().GetCycleTimeOutliers(team, RangeStart, RangeEnd, definitionId);
        }

        private Portfolio GivenAPortfolioWithItsBaselineAndClosedFeatures(int[] baselineCycleTimes, params int[] shownCycleTimes)
        {
            var portfolio = new Portfolio
            {
                Id = 31,
                Name = "Ocean Explorer roadmap",
                DoneItemsCutoffDays = 0,
                ProcessBehaviourChartBaselineStartDate = BaselineStart,
                ProcessBehaviourChartBaselineEndDate = BaselineEnd,
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

        private OutlierVerdict WhenThePortfoliosOutliersAreAskedFor(Portfolio portfolio)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>().GetCycleTimeOutliers(portfolio, RangeStart, RangeEnd, null);
        }

        private void ThenThePortfoliosProcessBehaviourChartShowsAboveItsLimit(Portfolio portfolio, int limit, params int[] expectedFeatureIds)
        {
            using var scope = harness.NewScope();
            var chart = scope.ServiceProvider.GetRequiredService<IPortfolioMetricsService>().GetCycleTimeProcessBehaviourChart(portfolio, RangeStart, RangeEnd);
            ThenTheChartShowsAboveItsLimit(chart, limit, expectedFeatureIds);
        }

        private static void ThenTheOutliersAre(OutlierVerdict verdict, params int[] expectedWorkItemIds)
        {
            // A verdict without limits names no outliers, which this baseline must never give.
            var outlierIds = (verdict as OutlierVerdict.Limited)?.OutlierWorkItemIds ?? [];

            Assert.That(outlierIds, Is.EquivalentTo(expectedWorkItemIds),
                $"WHAT: the verdict ({verdict.GetType().Name}) does not name exactly the items whose Cycle Time is strictly above the unrounded upper natural process limit. " +
                "WHY: the scatterplot must set apart exactly the items the Cycle Time process behaviour chart shows above its limit. " +
                "HOW: compare each Cycle Time with the unrounded limit of the baseline Cycle Times (days > limit), never with 5 x the median, a rounded limit or >=.");
        }

        private void ThenTheProcessBehaviourChartShowsAboveItsLimit(Team team, int limit, params int[] expectedWorkItemIds)
        {
            ThenTheChartShowsAboveItsLimit(TheCycleTimeProcessBehaviourChart(team), limit, expectedWorkItemIds);
        }

        private static void ThenTheChartShowsAboveItsLimit(ProcessBehaviourChart chart, int limit, int[] expectedWorkItemIds)
        {
            var aboveTheLimit = chart.DataPoints.Where(point => point.YValue > chart.UpperNaturalProcessLimit).SelectMany(point => point.WorkItemIds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chart.UpperNaturalProcessLimit, Is.EqualTo(limit),
                    "WHAT: the process behaviour chart shows another upper limit than the one this baseline gives. " +
                    "WHY: the outliers are compared with the chart, so the chart must be the one of the same baseline. " +
                    "HOW: leave the process behaviour chart's limits as they were.");
                Assert.That(aboveTheLimit, Is.EquivalentTo(expectedWorkItemIds),
                    "WHAT: the process behaviour chart shows other Work Items above its limit than the outliers. " +
                    "WHY: a reader comparing the two charts must see the same unusual Work Items. " +
                    "HOW: leave the process behaviour chart's points and limits as they were.");
            }
        }

        private void ThenTheProcessBehaviourChartShowsItsLimitAs(Team team, int roundedLimit)
        {
            Assert.That(TheCycleTimeProcessBehaviourChart(team).UpperNaturalProcessLimit, Is.EqualTo(roundedLimit),
                "WHAT: the process behaviour chart no longer shows its upper limit rounded to whole days. " +
                "WHY: the chart's answer stays as it was; only the outlier rule compares without rounding. " +
                "HOW: leave the process behaviour chart's rounding as it was.");
        }

        private void ThenTheProcessBehaviourChartMarksAsALargeChange(Team team, int workItemId)
        {
            var point = TheCycleTimeProcessBehaviourChart(team).DataPoints.Single(candidate => candidate.WorkItemIds.Contains(workItemId));

            Assert.That(point.SpecialCauses, Does.Contain(SpecialCauseType.LargeChange),
                "WHAT: the Work Item below the lower limit is not marked as a large change on the process behaviour chart. " +
                "WHY: this scenario shows that a large change below the lower limit is not an outlier, so the chart must mark it. " +
                "HOW: leave the process behaviour chart's special-cause rules as they were.");
        }

        private ProcessBehaviourChart TheCycleTimeProcessBehaviourChart(Team team)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().GetCycleTimeProcessBehaviourChart(team, RangeStart, RangeEnd);
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
