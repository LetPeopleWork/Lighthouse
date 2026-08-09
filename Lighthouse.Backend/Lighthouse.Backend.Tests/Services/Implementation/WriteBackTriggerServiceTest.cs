using System.Linq.Expressions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class WriteBackTriggerServiceTest
    {
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<ILogger<WriteBackTriggerService>> loggerMock;

        // Bug #5567 root cause D - the subject's clock, the seeded work items and the expected
        // dates all hang off one fixed instant, so an expectation can no longer agree with the
        // subject just because both read the wall clock.
        private static readonly DateTimeOffset FixedInstant = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);

        private static readonly DateTime FixedNowUtc = new(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc);

        [SetUp]
        public void Setup()
        {
            licenseServiceMock = new Mock<ILicenseService>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            loggerMock = new Mock<ILogger<WriteBackTriggerService>>();

            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(true);
            blackoutPeriodServiceMock
                .Setup(s => s.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns([]);
        }

        [Test]
        public void ResolveWriteBackForTeam_NoMappings_ResolvesNothing()
        {
            var team = CreateTeamWithWorkItems();

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveWriteBackForTeam_NoTeamLevelMappings_ResolvesNothing()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveWriteBackForTeam_NotPremiumLicense_ResolvesNothing()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            // Seeded on purpose: without an item the plan is empty whether the licence gate fires or
            // not, and the assertion below would hold for a build that had no gate at all.
            SetupWorkItemsForTeam(team.Id, [CreateWorkItem("101", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-5))]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveWriteBackForTeam_WorkItemAgeCycleTime_WritesAgeForDoingItems()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var doingItem = CreateWorkItem("101", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-5));
            var todoItem = CreateWorkItem("102", StateCategories.ToDo, team);

            SetupWorkItemsForTeam(team.Id, [doingItem, todoItem]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "101" &&
                        updates[0].TargetFieldReference == "Custom.Age" &&
                        updates[0].Value == "6");
        }

        [Test]
        public void ResolveWriteBackForTeam_WorkItemAgeCycleTime_StaysTodayAnchoredAfterTheAsOfDateFix()
        {
            // Story 5508 CI5: slice 03 makes every Work-Item-Age *dashboard* surface a function of the
            // selected date. Write-back must NOT follow — it keeps emitting the age as of today.
            // Deliberately NOT expressed as `doingItem.WorkItemAge(TestToday.Zone, TestToday.Ambient).ToString()`: that expectation would
            // move with the property and therefore pin nothing. The literal is the point.
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var doingItem = CreateWorkItem("101", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-5));

            SetupWorkItemsForTeam(team.Id, [doingItem]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].Value == "6");
        }

        [Test]
        public void ResolveWriteBackForTeam_WorKItemAgeCycleTime_WritesCycleTimeForDoneItems()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.CycleTime"));

            var toDo = CreateWorkItem("201", StateCategories.ToDo, team, startedDate: FixedNowUtc.AddDays(-3));
            var doneItem = CreateWorkItem("202", StateCategories.Done, team, startedDate: FixedNowUtc.AddDays(-7), closedDate: FixedNowUtc);

            SetupWorkItemsForTeam(team.Id, [toDo, doneItem]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "202" &&
                        updates[0].TargetFieldReference == "Custom.CycleTime" &&
                        updates[0].Value == doneItem.CycleTime(TestToday.Zone).ToString());
        }

        [Test]
        public void ResolveWriteBackForTeam_NoMatchingWorkItems_ResolvesNothing()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var todoItem = CreateWorkItem("401", StateCategories.ToDo, team);
            SetupWorkItemsForTeam(team.Id, [todoItem]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveWriteBackForTeam_ExceptionOccurs_DoesNotThrow()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var doingItem = CreateWorkItem("501", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-3));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            // Resolution no longer performs I/O of its own, so the failure has to come from something it
            // still reads. The promise is unchanged: a broken read must not take the refresh down with it.
            workItemRepositoryMock
                .Setup(r => r.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Throws(new InvalidOperationException("Repository unavailable"));

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.ResolveWriteBackForTeam(team));
        }

        [Test]
        public void ResolveWriteBackForTeam_ExceptionOccurs_LogsError()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var doingItem = CreateWorkItem("601", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-3));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            // Resolution no longer performs I/O of its own, so the failure has to come from something it
            // still reads. The promise is unchanged: a broken read must not take the refresh down with it.
            workItemRepositoryMock
                .Setup(r => r.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()))
                .Throws(new InvalidOperationException("Repository unavailable"));

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            AssertSingleErrorLoggedContaining("Write-back resolution failed for team");
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_NoMappings_ResolvesNothing()
        {
            var portfolio = CreatePortfolioWithFeatures();

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_NoMappings_ResolvesNothing()
        {
            var portfolio = CreatePortfolioWithFeatures();

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_NoPortfolioLevelMappings_ResolvesNothing()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_NotPremiumLicense_ResolvesNothing()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            // See the team case: an empty Portfolio makes this assertion true for the wrong reason.
            portfolio.Features.Add(CreateFeature("F-1", StateCategories.Doing, new Team { Id = 1, Name = "Team 1" }, remainingItems: 5, totalItems: 10));

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_NotPremiumLicense_ResolvesNothing()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            // See the team case: an empty Portfolio makes this assertion true for the wrong reason.
            portfolio.Features.Add(CreateFeatureWithForecast("F-1", StateCategories.Doing, new Team { Id = 1, Name = "Team 1" }, daysAt85: 20));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_FeatureSize_WritesAllFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature1 = CreateFeature("F-1", StateCategories.Doing, team, remainingItems: 5, totalItems: 10);
            var feature2 = CreateFeature("F-2", StateCategories.ToDo, team, remainingItems: 8, totalItems: 8);
            portfolio.Features.Add(feature1);
            portfolio.Features.Add(feature2);

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 2 &&
                        updates.Any(u => u.WorkItemId == "F-1" && u.Value == "10") &&
                        updates.Any(u => u.WorkItemId == "F-2" && u.Value == "8"));
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_ForecastPercentile_WritesDateForOpenFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            var openFeature = CreateFeatureWithForecast("F-10", StateCategories.Doing, team, daysAt85: 14);
            var doneFeature = CreateFeatureWithForecast("F-11", StateCategories.Done, team, daysAt85: 5);
            portfolio.Features.Add(openFeature);
            portfolio.Features.Add(doneFeature);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            const string expectedDate = "2026-03-24";

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-10" &&
                        updates[0].TargetFieldReference == "Custom.Forecast85" &&
                        updates[0].Value == expectedDate);
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_ForecastAsFormattedText_UsesDateFormat()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile50, WriteBackAppliesTo.Portfolio, "Custom.Forecast50", WriteBackTargetValueType.FormattedText, "MM/dd/yyyy"));

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-20", StateCategories.Doing, team, daysAt50: 10);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            const string expectedDate = "03/20/2026";

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].Value == expectedDate);
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_ForecastNotAvailable_SkipsFeature()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            var featureNoForecast = CreateFeature("F-30", StateCategories.Doing, team, remainingItems: 5, totalItems: 10);
            portfolio.Features.Add(featureNoForecast);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_ExceptionOccurs_DoesNotThrow()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeature("F-40", StateCategories.Doing, team, remainingItems: 3, totalItems: 7);
            portfolio.Features.Add(feature);

            // Resolution no longer performs I/O of its own, so the failure has to come from something it
            // still reads. The promise is unchanged: a broken read must not take the refresh down with it.
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Throws(new InvalidOperationException("Licence check unavailable"));

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.ResolveFeatureWriteBackForPortfolio(portfolio));
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_OnlyWritesNonForecastMappings()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-50", StateCategories.Doing, team, remainingItems: 4, totalItems: 12, daysAt85: 20);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].TargetFieldReference == "Custom.Size" &&
                        updates[0].Value == "12");
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_OnlyWritesForecastMappings()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-50", StateCategories.Doing, team, remainingItems: 4, totalItems: 12, daysAt85: 20);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].TargetFieldReference == "Custom.Forecast85");
        }

        [Test]
        public void ResolveWriteBackForTeam_NullConnection_DoesNotThrow()
        {
            var team = new Team
            {
                Id = 1,
                Name = "Team No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.ResolveWriteBackForTeam(team));
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_NullConnection_DoesNotThrow()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Portfolio No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.ResolveFeatureWriteBackForPortfolio(portfolio));
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_NullConnection_DoesNotThrow()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Portfolio No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrow(() => subject.ResolveForecastWriteBackForPortfolio(portfolio));
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_WorkItemAge_WritesForDoingFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Portfolio, "Custom.FeatureAge"));

            var team = new Team { Id = 1, Name = "Team 1" };
            var doingFeature = CreateFeature("F-60", StateCategories.Doing, team, remainingItems: 3, totalItems: 5,
                startedDate: FixedNowUtc.AddDays(-8));
            var todoFeature = CreateFeature("F-61", StateCategories.ToDo, team, remainingItems: 5, totalItems: 5);
            portfolio.Features.Add(doingFeature);
            portfolio.Features.Add(todoFeature);

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-60" &&
                        updates[0].Value == "9");
        }

        [Test]
        public void ResolveWriteBackForTeam_LogsStartAndCompletion()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Team, "Custom.Age"));

            var doingItem = CreateWorkItem("701", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-2));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            var subject = CreateSubject();

            var plan = subject.ResolveWriteBackForTeam(team);

            var infoInvocations = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Information)
                .ToList();

            Assert.That(infoInvocations, Has.Count.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void ResolveWriteBackForTeam_MappingWhoseFieldNeverResolved_IsSkippedWithAWarning()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(UnresolvedMapping(WriteBackAppliesTo.Team));

            SetupWorkItemsForTeam(team.Id, [CreateWorkItem("101", StateCategories.Doing, team, startedDate: FixedNowUtc.AddDays(-5))]);

            var plan = CreateSubject().ResolveWriteBackForTeam(team);

            Assert.That(plan, Is.Empty,
                "A mapping whose field the connection no longer defines has nowhere to write; skipping it beats guessing.");
            AssertWarningLoggedContaining("AdditionalFieldDefinition is not resolved");
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_MappingWhoseFieldNeverResolved_IsSkippedWithAWarning()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(UnresolvedMapping(WriteBackAppliesTo.Portfolio));
            portfolio.Features.Add(CreateFeature("F-90", StateCategories.Doing, new Team { Id = 1, Name = "Team 1" }, remainingItems: 5, totalItems: 10));

            var plan = CreateSubject().ResolveFeatureWriteBackForPortfolio(portfolio);

            Assert.That(plan, Is.Empty);
            AssertWarningLoggedContaining("AdditionalFieldDefinition is not resolved");
        }

        /// <summary>
        /// A mapping that outlived the field it pointed at — the shape left behind when an additional
        /// field is removed from the connection but the mapping row is not.
        /// </summary>
        private static WriteBackMappingDefinition UnresolvedMapping(WriteBackAppliesTo appliesTo) => new()
        {
            ValueSource = appliesTo == WriteBackAppliesTo.Team
                ? WriteBackValueSource.WorkItemAgeCycleTime
                : WriteBackValueSource.FeatureSize,
            AppliesTo = appliesTo,
            AdditionalFieldDefinitionId = 4242,
            AdditionalFieldDefinition = null,
            TargetValueType = WriteBackTargetValueType.Date,
        };

        private void AssertWarningLoggedContaining(string expected)
        {
            var warnings = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Warning)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty);

            Assert.That(warnings, Has.One.Contains(expected));
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_SaysWhichPassItRan()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            CreateSubject().ResolveFeatureWriteBackForPortfolio(portfolio);

            // A Portfolio refresh runs two passes over the same items. The log is where an operator
            // tells them apart, so naming the wrong one is a real defect rather than a typo.
            AssertInformationLoggedContaining("feature write-back for portfolio");
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_SaysWhichPassItRan()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85", WriteBackTargetValueType.Date));

            CreateSubject().ResolveForecastWriteBackForPortfolio(portfolio);

            AssertInformationLoggedContaining("forecast write-back for portfolio");
        }

        private void AssertInformationLoggedContaining(string expected)
        {
            var messages = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Information)
                .Select(i => i.Arguments[2]?.ToString() ?? string.Empty);

            Assert.That(messages, Has.One.Contains(expected));
        }

        [Test]
        public void ResolveWriteBackForTeam_NoMappings_NeverAsksTheRepositoryForWorkItems()
        {
            var team = CreateTeamWithWorkItems();

            var subject = CreateSubject();

            subject.ResolveWriteBackForTeam(team);

            workItemRepositoryMock.Verify(
                r => r.GetAllByPredicate(It.IsAny<Expression<Func<WorkItem, bool>>>()),
                Times.Never,
                "A connection with no team-level mapping has nothing to write back, so it must not cost a query.");
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_WorkItemAgeCycleTime_WritesCycleTimeForDoneFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.WorkItemAgeCycleTime, WriteBackAppliesTo.Portfolio, "Custom.CycleTime"));

            var team = new Team { Id = 1, Name = "Team 1" };
            var doneFeature = CreateFeature("F-70", StateCategories.Done, team, remainingItems: 0, totalItems: 5,
                startedDate: FixedNowUtc.AddDays(-6), closedDate: FixedNowUtc);
            portfolio.Features.Add(doneFeature);

            var subject = CreateSubject();

            var plan = subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                updates.Count == 1 &&
                updates[0].WorkItemId == "F-70" &&
                updates[0].Value == doneFeature.CycleTime(TestToday.Zone).ToString());
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_DateTargetCarryingAFormat_StillWritesTheIsoDate()
        {
            var portfolio = CreatePortfolioWithFeatures();
            // A Date mapping that also carries a format: only FormattedText may honour it, so a build
            // that reads either half of that condition on its own writes the wrong string.
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85",
                    WriteBackTargetValueType.Date, dateFormat: "dd/MM/yyyy"));

            var team = new Team { Id = 1, Name = "Team 1" };
            portfolio.Features.Add(CreateFeatureWithForecast("F-80", StateCategories.Doing, team, daysAt85: 20));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                updates.Count == 1 &&
                updates[0].Value == FixedNowUtc.Date.AddDays(20).ToString("yyyy-MM-dd"));
        }

        [Test]
        public void ResolveForecastWriteBackForPortfolio_FormattedTextCarryingAFormat_WritesThatFormat()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.ForecastPercentile85, WriteBackAppliesTo.Portfolio, "Custom.Forecast85",
                    WriteBackTargetValueType.FormattedText, dateFormat: "dd/MM/yyyy"));

            var team = new Team { Id = 1, Name = "Team 1" };
            portfolio.Features.Add(CreateFeatureWithForecast("F-81", StateCategories.Doing, team, daysAt85: 20));

            var subject = CreateSubject();

            var plan = subject.ResolveForecastWriteBackForPortfolio(portfolio);

            AssertPlanned(plan, updates =>
                updates.Count == 1 &&
                updates[0].Value == FixedNowUtc.Date.AddDays(20).ToString("dd/MM/yyyy"));
        }

        [Test]
        public void ResolveFeatureWriteBackForPortfolio_ExceptionOccurs_LogsWhichPortfolioFailed()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(
                CreateMapping(WriteBackValueSource.FeatureSize, WriteBackAppliesTo.Portfolio, "Custom.Size"));

            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Throws(new InvalidOperationException("Licence check unavailable"));

            var subject = CreateSubject();

            subject.ResolveFeatureWriteBackForPortfolio(portfolio);

            AssertSingleErrorLoggedContaining("Write-back resolution failed for portfolio");
        }

        /// <summary>
        /// The swallowed failure leaves no trace but this line, so the line is the contract.
        /// </summary>
        private void AssertSingleErrorLoggedContaining(string expected)
        {
            var errorInvocations = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .ToList();

            Assert.That(errorInvocations, Has.Count.EqualTo(1));
            Assert.That(errorInvocations[0].Arguments[2]?.ToString(), Does.Contain(expected));
        }

        private static void AssertPlanned(
            IReadOnlyList<WriteBackFieldUpdate> plan,
            Func<IReadOnlyList<WriteBackFieldUpdate>, bool> expectation)
        {
            Assert.That(expectation(plan), Is.True,
                $"Resolved plan did not match: [{string.Join(", ", plan.Select(u => $"{u.WorkItemId}/{u.TargetFieldReference}={u.Value}"))}]");
        }

        private WriteBackTriggerService CreateSubject()
        {
            return new WriteBackTriggerService(
                licenseServiceMock.Object,
                workItemRepositoryMock.Object,
                blackoutPeriodServiceMock.Object,
                new Lighthouse.Backend.Tests.TestDoubles.FakeLighthouseClock(FixedInstant),
                loggerMock.Object);
        }

        private static Team CreateTeamWithWorkItems()
        {
            return new Team
            {
                Id = 1,
                Name = "Test Team",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Id = 10,
                    Name = "Test Connection",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
            };
        }

        private static Portfolio CreatePortfolioWithFeatures()
        {
            return new Portfolio
            {
                Id = 1,
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection
                {
                    Id = 20,
                    Name = "Test Connection",
                    WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
                },
            };
        }

        private static WorkItem CreateWorkItem(string referenceId, StateCategories stateCategory, Team team,
            DateTime? startedDate = null, DateTime? closedDate = null)
        {
            var stateMap = new Dictionary<StateCategories, string>
            {
                { StateCategories.ToDo, "New" },
                { StateCategories.Doing, "Active" },
                { StateCategories.Done, "Closed" },
            };

            return new WorkItem
            {
                ReferenceId = referenceId,
                Name = $"Work Item {referenceId}",
                State = stateMap.GetValueOrDefault(stateCategory, "Unknown"),
                StateCategory = stateCategory,
                Team = team,
                TeamId = team.Id,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CreatedDate = startedDate ?? FixedNowUtc.AddDays(-30),
            };
        }

        private static Feature CreateFeature(string referenceId, StateCategories stateCategory, Team team,
            int remainingItems = 0, int totalItems = 0, DateTime? startedDate = null, DateTime? closedDate = null)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Feature {referenceId}",
                StateCategory = stateCategory,
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CreatedDate = startedDate ?? FixedNowUtc.AddDays(-30),
            };

            if (totalItems > 0)
            {
                feature.AddOrUpdateWorkForTeam(team, remainingItems, totalItems);
            }

            return feature;
        }

        private static Feature CreateFeatureWithForecast(string referenceId, StateCategories stateCategory, Team team,
            int daysAt50 = -1, int daysAt70 = -1, int daysAt85 = -1, int daysAt95 = -1,
            int remainingItems = 5, int totalItems = 10)
        {
            var feature = CreateFeature(referenceId, stateCategory, team, remainingItems, totalItems);

            var simulationResults = new Dictionary<int, int>();

            var targetPercentiles = new[] { (50, daysAt50), (70, daysAt70), (85, daysAt85), (95, daysAt95) };
            foreach (var (percentile, days) in targetPercentiles)
            {
                if (days >= 0)
                {
                    simulationResults[days] = 100;
                }
            }

            if (simulationResults.Count <= 0)
            {
                return feature;
            }

            var forecast = new WhenForecast
            {
                TeamId = team.Id,
                Team = team,
                NumberOfItems = remainingItems,
                FeatureId = feature.Id,
                Feature = feature,
                TotalTrials = 100,
            };

            foreach (var kvp in simulationResults)
            {
                forecast.SimulationResults.Add(new IndividualSimulationResult { Key = kvp.Key, Value = kvp.Value });
            }

            feature.Forecasts.Add(forecast);

            return feature;
        }

        private void SetupWorkItemsForTeam(int teamId, List<WorkItem> workItems)
        {
            workItemRepositoryMock
                .Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<WorkItem, bool>>>()))
                .Returns(workItems.AsQueryable());
        }

        private static WriteBackMappingDefinition CreateMapping(
            WriteBackValueSource valueSource, WriteBackAppliesTo appliesTo, string fieldReference,
            WriteBackTargetValueType targetValueType = WriteBackTargetValueType.Date, string? dateFormat = null)
        {
            var fieldDef = new AdditionalFieldDefinition
            {
                Id = fieldReference.GetHashCode() & 0x7FFFFFFF,
                DisplayName = fieldReference,
                Reference = fieldReference
            };

            return new WriteBackMappingDefinition
            {
                ValueSource = valueSource,
                AppliesTo = appliesTo,
                AdditionalFieldDefinitionId = fieldDef.Id,
                AdditionalFieldDefinition = fieldDef,
                TargetValueType = targetValueType,
                DateFormat = dateFormat,
            };
        }
    }
}
