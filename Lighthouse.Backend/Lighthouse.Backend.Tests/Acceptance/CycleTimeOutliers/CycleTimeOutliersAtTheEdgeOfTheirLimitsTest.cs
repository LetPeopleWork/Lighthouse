using Lighthouse.Backend.Cache;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Modules.FlowMetrics.Contracts;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
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
    /// Where the outlier limits start and stop: a baseline needs at least 14 days, so 13 days have no limits and 14 days
    /// have them, for a configured baseline and for the selected range standing in for one; and a named Cycle Time that
    /// exists but names a state outside the Team's workflow has no limits, even when the default Cycle Time has them.
    ///
    /// The Team metrics service is asked as the product builds it, over a database of the scenario's own, with no web host.
    /// </summary>
    [TestFixture]
    public class CycleTimeOutliersAtTheEdgeOfTheirLimitsTest
    {
        private static readonly DateTimeOffset Now = new(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);

        private static readonly DateTime RangeStart = new(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime BaselineStart = new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Average 20.5, average moving range 25/7: the upper limit is exactly 30 days.</summary>
        private static readonly int[] BaselineWithALimitOf30 = [18, 13, 12, 24, 26, 25, 25, 21];

        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const string AStateOutsideTheWorkflow = "Waiting for Review";
        private const int ReviewToDone = 4712;

        private ServicePortHarness harness = null!;

        [SetUp]
        public void GiveTheScenarioItsOwnDatabase()
        {
            harness = new ServicePortHarness(RegisterTheMetricsAsTheProductBuildsThem);
        }

        [TearDown]
        public void RemoveTheScenariosDatabase()
        {
            harness.Dispose();
        }

        [TestCase(13, false)]
        [TestCase(14, true)]
        public void A_range_without_a_configured_baseline_has_limits_from_14_days_on(int rangeDays, bool hasLimits)
        {
            var team = GivenATeam(null, null);
            GivenClosedWorkItems(team, RangeStart.AddDays(1), 10, 12);

            var outliers = WhenTheOutliersAreAskedFor(team, RangeStart.AddDays(rangeDays), definitionId: null);

            ThenTheLimitsAre(outliers, hasLimits, $"a selected range of {rangeDays} days with 2 closed Work Items");
        }

        [TestCase(13, false)]
        [TestCase(14, true)]
        public void A_configured_baseline_has_limits_from_14_days_on(int baselineDays, bool hasLimits)
        {
            var team = GivenATeam(BaselineStart, BaselineStart.AddDays(baselineDays));
            GivenClosedWorkItems(team, BaselineStart.AddDays(1), BaselineWithALimitOf30);
            GivenClosedWorkItems(team, RangeStart.AddDays(1), 12, 31, 90);

            var outliers = WhenTheOutliersAreAskedFor(team, RangeStart.AddDays(28), definitionId: null);

            ThenTheLimitsAre(outliers, hasLimits, $"a configured baseline of {baselineDays} days holding 8 closed Work Items");
        }

        [Test]
        public void A_named_Cycle_Time_that_starts_in_a_state_outside_the_workflow_has_no_limits_while_the_default_has_them()
        {
            var team = GivenATeam(BaselineStart, BaselineStart.AddDays(24));
            GivenClosedWorkItems(team, BaselineStart.AddDays(1), BaselineWithALimitOf30);
            GivenClosedWorkItems(team, RangeStart.AddDays(1), 12, 31, 90);

            var withTheDefault = WhenTheOutliersAreAskedFor(team, RangeStart.AddDays(28), definitionId: null);
            var withTheInvalidNamedOne = WhenTheOutliersAreAskedFor(team, RangeStart.AddDays(28), ReviewToDone);

            ThenTheLimitsAre(withTheDefault, true, "the default Cycle Time over a 24-day baseline of 8 closed Work Items");
            ThenTheLimitsAre(withTheInvalidNamedOne, false, $"a named Cycle Time that starts in '{AStateOutsideTheWorkflow}', a state the Team's workflow does not have");
        }

        private static void ThenTheLimitsAre(OutlierVerdict verdict, bool hasLimits, string situation)
        {
            var expected = hasLimits ? typeof(OutlierVerdict.Limited) : typeof(OutlierVerdict.NoLimits);
            Assert.That(verdict, Is.TypeOf(expected),
                $"WHAT: {situation} gave {verdict}, expected {expected.Name}. " +
                "WHY: the scatterplot sets items apart only where the process behaviour chart has limits, and that needs a baseline of at least 14 days " +
                "with 2 positive Cycle Times, measured with a Cycle Time whose states are in the workflow. " +
                "HOW: answer NoLimits below 14 baseline days and for a named Cycle Time the Team cannot measure; answer Limited otherwise.");
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
                        Id = ReviewToDone,
                        Name = "Review to Done",
                        StartState = AStateOutsideTheWorkflow,
                        EndState = Done,
                    },
                ],
            };
            teams.Add(team);
            teams.Save().GetAwaiter().GetResult();
            return team;
        }

        /// <summary>One Work Item per Cycle Time, closed a day apart in the order given.</summary>
        private void GivenClosedWorkItems(Team team, DateTime firstClosedOn, params int[] cycleTimes)
        {
            using var scope = harness.NewScope();
            var workItems = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
            var transitions = scope.ServiceProvider.GetRequiredService<IWorkItemStateTransitionRepository>();

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

        private OutlierVerdict WhenTheOutliersAreAskedFor(Team team, DateTime rangeEnd, int? definitionId)
        {
            using var scope = harness.NewScope();
            return scope.ServiceProvider.GetRequiredService<ITeamMetricsService>().GetCycleTimeOutliers(team, RangeStart, rangeEnd, definitionId);
        }

        private static void RegisterTheMetricsAsTheProductBuildsThem(IServiceCollection services)
        {
            services.AddSingleton<ILighthouseClock>(new FakeLighthouseClock(Now, TimeZoneInfo.Utc));
            services.AddSingleton<Cache<string, object>>();
            services.AddScoped(_ => RefreshSettingsAsSeeded());
            services.AddScoped(_ => Mock.Of<IDomainEventDispatcher>());
            services.AddScoped(_ => Mock.Of<IThroughputFilter>());
            services.AddScoped(_ => Mock.Of<IForecastDataSufficiency>());
            services.AddScoped(_ => Mock.Of<IFeatureRepository>());
            services.AddScoped<IRepository<Team>, TeamRepository>();
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemStateTransitionRepository, WorkItemStateTransitionRepository>();
            services.AddScoped<IRepository<BlackoutPeriod>, BlackoutPeriodRepository>();
            services.AddScoped<IRepository<RecurringBlackoutRule>, RecurringBlackoutRuleRepository>();
            services.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            services.AddSingleton<ICycleTimeOutlierDefinition, UpperNaturalProcessLimitOutlierDefinition>();
            services.AddScoped<ITeamMetricsService, TeamMetricsService>();
        }

        private static IAppSettingService RefreshSettingsAsSeeded()
        {
            var settings = new Mock<IAppSettingService>();
            settings.Setup(service => service.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 60, RefreshAfter = 180, StartDelay = 10 });
            return settings.Object;
        }
    }
}
