using System.Linq.Expressions;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    [TestFixture]
    public class ForecastFilterFeatureForecastIntegrationTest
    {
        private const string BugExclusionWarning = "Filter excluded all throughput; showing unfiltered forecast";

        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<IForecastService> forecastServiceMock;
        private Mock<IRepository<BlackoutPeriod>> blackoutPeriodRepositoryMock;
        private Mock<ILicenseService> licenseServiceMock;
        private List<WorkItem> workItems;
        private TeamMetricsService subject;

        [SetUp]
        public void Setup()
        {
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            forecastServiceMock = new Mock<IForecastService>();
            blackoutPeriodRepositoryMock = new Mock<IRepository<BlackoutPeriod>>();
            blackoutPeriodRepositoryMock.Setup(r => r.GetAll()).Returns(Enumerable.Empty<BlackoutPeriod>().AsQueryable());

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            var appSettingsServiceMock = new Mock<IAppSettingService>();
            appSettingsServiceMock.Setup(x => x.GetTeamDataRefreshSettings())
                .Returns(new RefreshSettings { Interval = 1 });

            var forecastFilterRuleService = new ForecastFilterRuleService(
                new RuleEvaluator<WorkItem>(),
                new WorkItemFieldProvider(),
                licenseServiceMock.Object);

            var serviceProvider = new Mock<IServiceProvider>();
            serviceProvider.Setup(sp => sp.GetService(typeof(IForecastService)))
                .Returns(forecastServiceMock.Object);

            workItems = new List<WorkItem>();
            workItemRepositoryMock.Setup(x => x.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Returns((Expression<Func<WorkItem, bool>> predicate) => workItems.Where(predicate.Compile()).AsQueryable());

            subject = new TeamMetricsService(
                Mock.Of<ILogger<TeamMetricsService>>(),
                workItemRepositoryMock.Object,
                featureRepositoryMock.Object,
                appSettingsServiceMock.Object,
                serviceProvider.Object,
                blackoutPeriodRepositoryMock.Object,
                forecastFilterRuleService);
        }

        [TearDown]
        public void TearDown()
        {
            subject.InvalidateTeamMetrics(BuildTeam(teamId: 9001));
            subject.InvalidateTeamMetrics(BuildTeam(teamId: 9002));
        }

        [Test]
        public void FeatureForecast_TeamWithBugExclusionRule_DrawsThroughputFromNonBugClosesOnly()
        {
            var team = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-2", daysAgo: 7);

            var status = subject.GetForecastThroughputStatus(team);

            var closedItems = status.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).ToList();
            Assert.That(closedItems.Select(i => i.ReferenceId), Does.Not.Contain("BUG-1"));
            Assert.That(closedItems.Select(i => i.ReferenceId), Is.EquivalentTo(new[] { "US-1", "US-2" }));
        }

        [Test]
        public void FeatureForecast_ResponseAfterFilterApplied_IncludesFilterAppliedTrueAndExcludedSummary()
        {
            var team = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);

            var status = subject.GetForecastThroughputStatus(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.FilterApplied, Is.True);
                Assert.That(status.ExcludedSummary, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void FeatureForecast_TeamWithoutRuleSet_ReturnsFilterAppliedFalseAndIdenticalDatesToToday()
        {
            var team = BuildTeam(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);

            var withSetting = subject.GetForecastThroughputStatus(team);
            var skip = subject.GetForecastThroughputStatus(team, ThroughputFilterMode.SkipFilter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withSetting.FilterApplied, Is.False);
                Assert.That(withSetting.ExcludedSummary, Is.Null);
                Assert.That(withSetting.Throughput.Total, Is.EqualTo(skip.Throughput.Total));
                Assert.That(withSetting.Throughput.History, Is.EqualTo(skip.Throughput.History));
            }
        }

        [Test]
        public void FeatureForecast_MultiTeamFeature_AppliesEachTeamsFilterIndependently()
        {
            var teamWithFilter = BuildTeamWithBugExclusion(teamId: 9001);
            var teamWithoutFilter = BuildTeam(teamId: 9002);

            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-T1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-T1", daysAgo: 5);
            AddClosedWorkItem(teamId: 9002, type: "Bug", referenceId: "BUG-T2", daysAgo: 3);
            AddClosedWorkItem(teamId: 9002, type: "User Story", referenceId: "US-T2", daysAgo: 5);

            var statusFiltered = subject.GetForecastThroughputStatus(teamWithFilter);
            var statusUnfiltered = subject.GetForecastThroughputStatus(teamWithoutFilter);

            var filteredRefs = statusFiltered.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).Select(i => i.ReferenceId).ToList();
            var unfilteredRefs = statusUnfiltered.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).Select(i => i.ReferenceId).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(statusFiltered.FilterApplied, Is.True);
                Assert.That(filteredRefs, Is.EquivalentTo(new[] { "US-T1" }));
                Assert.That(statusUnfiltered.FilterApplied, Is.False);
                Assert.That(unfilteredRefs, Is.EquivalentTo(new[] { "BUG-T2", "US-T2" }));
            }
        }

        [Test]
        public void FeatureForecast_RuleSetExcludesAllThroughput_FallsBackToUnfilteredWithLockedWarningSummary()
        {
            var team = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-2", daysAgo: 5);

            var status = subject.GetForecastThroughputStatus(team);

            var refs = status.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).Select(i => i.ReferenceId).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(status.FilterApplied, Is.False);
                Assert.That(status.ExcludedSummary, Is.EqualTo(BugExclusionWarning));
                Assert.That(refs, Is.EquivalentTo(new[] { "BUG-1", "BUG-2" }));
            }
        }

        [Test]
        public void FeatureForecast_RequestDoesNotCarryApplyFilterOverride_StillRespectsTeamSettingViaDefaultMode()
        {
            var team = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);

            var defaultMode = subject.GetForecastThroughputStatus(team);
            var explicitApply = subject.GetForecastThroughputStatus(team, ThroughputFilterMode.ApplyFilter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(defaultMode.FilterApplied, Is.True);
                Assert.That(defaultMode.Throughput.Total, Is.EqualTo(explicitApply.Throughput.Total));
            }
        }

        [Test]
        public void FeatureForecast_CacheKey_FilteredAndUnfilteredSeriesCacheIndependently()
        {
            var team = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);

            var filtered = subject.GetForecastThroughputStatus(team, ThroughputFilterMode.ApplyFilter);
            var unfiltered = subject.GetForecastThroughputStatus(team, ThroughputFilterMode.SkipFilter);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(filtered.Throughput.Total, Is.EqualTo(1));
                Assert.That(unfiltered.Throughput.Total, Is.EqualTo(2));
            }
        }

        [Test]
        public void FeatureForecast_IdenticalTeamStateAndRuleSet_ProducesIdenticalThroughputAndChipData()
        {
            var teamFirstRun = BuildTeamWithBugExclusion(teamId: 9001);
            AddClosedWorkItem(teamId: 9001, type: "Bug", referenceId: "BUG-1", daysAgo: 3);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-1", daysAgo: 5);
            AddClosedWorkItem(teamId: 9001, type: "User Story", referenceId: "US-2", daysAgo: 7);

            var firstRun = subject.GetForecastThroughputStatus(teamFirstRun);

            subject.InvalidateTeamMetrics(teamFirstRun);
            var teamSecondRun = BuildTeamWithBugExclusion(teamId: 9001);
            var secondRun = subject.GetForecastThroughputStatus(teamSecondRun);

            var firstRefs = firstRun.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).Select(i => i.ReferenceId).OrderBy(r => r).ToList();
            var secondRefs = secondRun.Throughput.WorkItemsPerUnitOfTime.Values.SelectMany(items => items).Select(i => i.ReferenceId).OrderBy(r => r).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondRun.FilterApplied, Is.EqualTo(firstRun.FilterApplied));
                Assert.That(secondRun.ExcludedSummary, Is.EqualTo(firstRun.ExcludedSummary));
                Assert.That(secondRun.Throughput.Total, Is.EqualTo(firstRun.Throughput.Total));
                Assert.That(secondRun.Throughput.History, Is.EqualTo(firstRun.Throughput.History));
                Assert.That(secondRefs, Is.EqualTo(firstRefs));
            }
        }

        private static Team BuildTeam(int teamId)
        {
            return new Team
            {
                Id = teamId,
                Name = $"Team-{teamId}",
                ThroughputHistory = 30,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = $"Conn-{teamId}" },
            };
        }

        private static Team BuildTeamWithBugExclusion(int teamId)
        {
            var team = BuildTeam(teamId);
            var ruleSet = new WorkItemRuleSet
            {
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = "workitem.type", Operator = "equals", Value = "Bug" }
                ]
            };
            team.ForecastFilterRuleSetJson = JsonSerializer.Serialize(ruleSet);
            return team;
        }

        private WorkItem AddClosedWorkItem(int teamId, string type, string referenceId, int daysAgo)
        {
            var workItem = new WorkItem
            {
                Id = workItems.Count + 1,
                StateCategory = StateCategories.Done,
                TeamId = teamId,
                ParentReferenceId = string.Empty,
                ReferenceId = referenceId,
                Name = referenceId,
                Type = type,
                StartedDate = DateTime.UtcNow.AddDays(-(daysAgo + 5)),
                ClosedDate = DateTime.UtcNow.Date.AddDays(-daysAgo),
            };
            workItems.Add(workItem);
            return workItem;
        }
    }
}
