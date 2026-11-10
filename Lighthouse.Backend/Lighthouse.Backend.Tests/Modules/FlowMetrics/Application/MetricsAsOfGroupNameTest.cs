using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Modules.FlowMetrics.Application;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Modules.FlowMetrics.Application
{
    /// <summary>
    /// A stored state change carries the name of the group its state belongs to ("Review"), not the tracker's own state.
    /// Looking back at a past day must still say which category that group was in.
    /// </summary>
    [TestFixture]
    public class MetricsAsOfGroupNameTest
    {
        private static readonly DateTime Day1 = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day5 = new(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Day19 = new(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc);

        private static IServiceProvider ACacheOnly(IForecastService forecastService)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(Lighthouse.Backend.Cache.Cache<string, object>)))
                .Returns(new Lighthouse.Backend.Cache.Cache<string, object>());
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService))).Returns(forecastService);
            return serviceProvider.Object;
        }

        [Test]
        public void TeamLookingBack_ItemWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing()
        {
            var team = new Team { Id = 1, Name = "Test Team", ThroughputHistory = 30, DoingStates = ["Code Review", "Testing"], DoneStates = ["Done"] };
            team.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            var workItems = new List<WorkItem> { new() { Id = 1, StateCategory = StateCategories.Doing, TeamId = 1, StartedDate = Day1 } };
            var transitions = new List<WorkItemStateTransition> { new() { Id = 1, WorkItemId = 1, ToState = "Review", TransitionedAt = Day5 } };

            var workItemRepository = new Mock<IWorkItemRepository>();
            workItemRepository.AnswersTeamReadsFrom(workItems);
            var transitionRepository = new Mock<IWorkItemStateTransitionRepository>();
            transitionRepository.AnswersTransitionReadsFrom(transitions);
            var appSettings = new Mock<IAppSettingService>();
            appSettings.Setup(x => x.GetTeamDataRefreshSettings()).Returns(new RefreshSettings { Interval = 1 });
            var blackouts = new Mock<IBlackoutPeriodService>();
            blackouts.Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>())).Returns([]);
            var filters = new Mock<IForecastFilterRuleService>();
            filters.Setup(s => s.GetEffectiveRuleSet(It.IsAny<Team>())).Returns((Lighthouse.Backend.Models.WorkItemRules.WorkItemRuleSet?)null);

            var subject = new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepository.Object,
                Mock.Of<IFeatureRepository>(),
                appSettings.Object,
                ACacheOnly(Mock.Of<IForecastService>()),
                blackouts.Object,
                filters.Object,
                transitionRepository.Object,
                TestToday.Clock);

            try
            {
                var snapshot = subject.GetWipSnapshotForTeam(team, Day19).Single();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(snapshot.State, Is.EqualTo("Review"));
                    Assert.That(snapshot.StateCategory, Is.EqualTo(StateCategories.Doing));
                }
            }
            finally
            {
                subject.InvalidateTeamMetrics(team);
            }
        }

        [Test]
        public void PortfolioLookingBack_FeatureWasInAGroupWhoseNameIsNoStateOfTheTracker_WasStillInDoing()
        {
            var portfolio = new Portfolio { Id = 3, Name = "Test Portfolio", DoingStates = ["Code Review", "Testing"], DoneStates = ["Done"] };
            portfolio.StateMappings.Add(new StateMapping { Name = "Review", States = ["Code Review", "Testing"] });

            var feature = new Feature { Id = 7, ReferenceId = "F-7", Name = "A feature", StateCategory = StateCategories.Doing, Type = string.Empty, State = string.Empty, Url = string.Empty, Order = string.Empty };
            var transitions = new List<FeatureStateTransition> { new() { Id = 1, FeatureId = 7, ToState = "Review", TransitionedAt = Day5 } };

            var transitionRepository = new Mock<IFeatureStateTransitionRepository>();
            transitionRepository.AnswersTransitionReadsFrom(transitions);
            var appSettings = new Mock<IAppSettingService>();
            appSettings.Setup(m => m.GetFeatureRefreshSettings()).Returns(new RefreshSettings { Interval = 30 });

            var subject = new PortfolioMetricsService(
                Mock.Of<ILogger<PortfolioMetricsService>>(),
                Mock.Of<IFeatureRepository>(),
                appSettings.Object,
                ACacheOnly(Mock.Of<IForecastService>()),
                transitionRepository.Object,
                TestToday.Clock);

            var states = subject.GetFeatureStatesAsOf(portfolio, [feature], Day19);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(states[7].State, Is.EqualTo("Review"));
                Assert.That(states[7].StateCategory, Is.EqualTo(StateCategories.Doing));
            }
        }
    }
}
