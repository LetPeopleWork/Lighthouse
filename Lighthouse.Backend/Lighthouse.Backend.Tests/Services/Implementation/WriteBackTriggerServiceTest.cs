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
        private Mock<IWriteBackService> writeBackServiceMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private Mock<ILogger<WriteBackTriggerService>> loggerMock;

        [SetUp]
        public void Setup()
        {
            writeBackServiceMock = new Mock<IWriteBackService>();
            licenseServiceMock = new Mock<ILicenseService>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();
            loggerMock = new Mock<ILogger<WriteBackTriggerService>>();

            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            writeBackServiceMock
                .Setup(w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ReturnsAsync(new WriteBackResult());
        }

        [Test]
        public async Task TriggerWriteBackForTeam_NoMappings_DoesNotCallWriteBackService()
        {
            var team = CreateTeamWithWorkItems();

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_NoTeamLevelMappings_DoesNotCallWriteBackService()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_NotPremiumLicense_DoesNotCallWriteBackService()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_WorkItemAge_WritesOnlyDoingItems()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var doingItem = CreateWorkItem("101", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-5));
            var todoItem = CreateWorkItem("102", StateCategories.ToDo, team);
            var doneItem = CreateWorkItem("103", StateCategories.Done, team, startedDate: DateTime.UtcNow.AddDays(-10), closedDate: DateTime.UtcNow);

            SetupWorkItemsForTeam(team.Id, [doingItem, todoItem, doneItem]);

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    team.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "101" &&
                        updates[0].TargetFieldReference == "Custom.Age" &&
                        updates[0].Value == doingItem.WorkItemAge.ToString())),
                Times.Once);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_CycleTime_WritesOnlyDoneItems()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.CycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.CycleTime",
            });

            var doingItem = CreateWorkItem("201", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-3));
            var doneItem = CreateWorkItem("202", StateCategories.Done, team, startedDate: DateTime.UtcNow.AddDays(-7), closedDate: DateTime.UtcNow);

            SetupWorkItemsForTeam(team.Id, [doingItem, doneItem]);

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    team.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "202" &&
                        updates[0].TargetFieldReference == "Custom.CycleTime" &&
                        updates[0].Value == doneItem.CycleTime.ToString())),
                Times.Once);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_MultipleMappings_WritesUpdatesForAll()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.CycleTime,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.CycleTime",
            });

            var doingItem = CreateWorkItem("301", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-4));
            var doneItem = CreateWorkItem("302", StateCategories.Done, team, startedDate: DateTime.UtcNow.AddDays(-6), closedDate: DateTime.UtcNow);

            SetupWorkItemsForTeam(team.Id, [doingItem, doneItem]);

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    team.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 2 &&
                        updates.Any(u => u.WorkItemId == "301" && u.TargetFieldReference == "Custom.Age") &&
                        updates.Any(u => u.WorkItemId == "302" && u.TargetFieldReference == "Custom.CycleTime"))),
                Times.Once);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_NoMatchingWorkItems_DoesNotCallWriteBackService()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var todoItem = CreateWorkItem("401", StateCategories.ToDo, team);
            SetupWorkItemsForTeam(team.Id, [todoItem]);

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_ExceptionOccurs_DoesNotThrow()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var doingItem = CreateWorkItem("501", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-3));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            writeBackServiceMock
                .Setup(w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ThrowsAsync(new InvalidOperationException("Connection failed"));

            var subject = CreateSubject();

            Assert.DoesNotThrowAsync(async () => await subject.TriggerWriteBackForTeam(team));
        }

        [Test]
        public async Task TriggerWriteBackForTeam_ExceptionOccurs_LogsError()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var doingItem = CreateWorkItem("601", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-3));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            writeBackServiceMock
                .Setup(w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ThrowsAsync(new InvalidOperationException("Connection failed"));

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            var errorInvocations = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Error)
                .ToList();

            Assert.That(errorInvocations, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_NoMappings_DoesNotCallWriteBackService()
        {
            var portfolio = CreatePortfolioWithFeatures();

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_NoMappings_DoesNotCallWriteBackService()
        {
            var portfolio = CreatePortfolioWithFeatures();

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_NoPortfolioLevelMappings_DoesNotCallWriteBackService()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_NotPremiumLicense_DoesNotCallWriteBackService()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_NotPremiumLicense_DoesNotCallWriteBackService()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(false);

            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_FeatureSize_WritesAllFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature1 = CreateFeature("F-1", StateCategories.Doing, team, remainingItems: 5, totalItems: 10);
            var feature2 = CreateFeature("F-2", StateCategories.ToDo, team, remainingItems: 8, totalItems: 8);
            portfolio.Features.Add(feature1);
            portfolio.Features.Add(feature2);

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 2 &&
                        updates.Any(u => u.WorkItemId == "F-1" && u.Value == "10") &&
                        updates.Any(u => u.WorkItemId == "F-2" && u.Value == "8"))),
                Times.Once);
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_ForecastPercentile_WritesDateForOpenFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var openFeature = CreateFeatureWithForecast("F-10", StateCategories.Doing, team, daysAt85: 14);
            var doneFeature = CreateFeatureWithForecast("F-11", StateCategories.Done, team, daysAt85: 5);
            portfolio.Features.Add(openFeature);
            portfolio.Features.Add(doneFeature);

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            var expectedDate = DateTime.UtcNow.Date.AddDays(14).ToString("yyyy-MM-dd");

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-10" &&
                        updates[0].TargetFieldReference == "Custom.Forecast85" &&
                        updates[0].Value == expectedDate)),
                Times.Once);
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_ForecastAsFormattedText_UsesDateFormat()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile50,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast50",
                TargetValueType = WriteBackTargetValueType.FormattedText,
                DateFormat = "MM/dd/yyyy",
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-20", StateCategories.Doing, team, daysAt50: 10);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            var expectedDate = DateTime.UtcNow.Date.AddDays(10).ToString("MM/dd/yyyy");

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].Value == expectedDate)),
                Times.Once);
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_ForecastNotAvailable_SkipsFeature()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var featureNoForecast = CreateFeature("F-30", StateCategories.Doing, team, remainingItems: 5, totalItems: 10);
            portfolio.Features.Add(featureNoForecast);

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()),
                Times.Never);
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_ExceptionOccurs_DoesNotThrow()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeature("F-40", StateCategories.Doing, team, remainingItems: 3, totalItems: 7);
            portfolio.Features.Add(feature);

            writeBackServiceMock
                .Setup(w => w.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .ThrowsAsync(new InvalidOperationException("Connection failed"));

            var subject = CreateSubject();

            Assert.DoesNotThrowAsync(async () => await subject.TriggerFeatureWriteBackForPortfolio(portfolio));
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_OnlyWritesNonForecastMappings()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-50", StateCategories.Doing, team, remainingItems: 4, totalItems: 12, daysAt85: 20);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].TargetFieldReference == "Custom.Size" &&
                        updates[0].Value == "12")),
                Times.Once);
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_OnlyWritesForecastMappings()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.FeatureSize,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Size",
            });
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.Forecast85",
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var feature = CreateFeatureWithForecast("F-50", StateCategories.Doing, team, remainingItems: 4, totalItems: 12, daysAt85: 20);
            portfolio.Features.Add(feature);

            var subject = CreateSubject();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].TargetFieldReference == "Custom.Forecast85")),
                Times.Once);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_NullConnection_DoesNotThrow()
        {
            var team = new Team
            {
                Id = 1,
                Name = "Team No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrowAsync(async () => await subject.TriggerWriteBackForTeam(team));
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_NullConnection_DoesNotThrow()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Portfolio No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrowAsync(async () => await subject.TriggerFeatureWriteBackForPortfolio(portfolio));
        }

        [Test]
        public async Task TriggerForecastWriteBackForPortfolio_NullConnection_DoesNotThrow()
        {
            var portfolio = new Portfolio
            {
                Id = 1,
                Name = "Portfolio No Connection",
                WorkTrackingSystemConnection = null!,
            };

            var subject = CreateSubject();

            Assert.DoesNotThrowAsync(async () => await subject.TriggerForecastWriteBackForPortfolio(portfolio));
        }

        [Test]
        public async Task TriggerFeatureWriteBackForPortfolio_WorkItemAge_WritesForDoingFeatures()
        {
            var portfolio = CreatePortfolioWithFeatures();
            portfolio.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                TargetFieldReference = "Custom.FeatureAge",
            });

            var team = new Team { Id = 1, Name = "Team 1" };
            var doingFeature = CreateFeature("F-60", StateCategories.Doing, team, remainingItems: 3, totalItems: 5,
                startedDate: DateTime.UtcNow.AddDays(-8));
            var todoFeature = CreateFeature("F-61", StateCategories.ToDo, team, remainingItems: 5, totalItems: 5);
            portfolio.Features.Add(doingFeature);
            portfolio.Features.Add(todoFeature);

            var subject = CreateSubject();

            await subject.TriggerFeatureWriteBackForPortfolio(portfolio);

            writeBackServiceMock.Verify(
                w => w.WriteFieldsToWorkItems(
                    portfolio.WorkTrackingSystemConnection,
                    It.Is<IReadOnlyList<WriteBackFieldUpdate>>(updates =>
                        updates.Count == 1 &&
                        updates[0].WorkItemId == "F-60" &&
                        updates[0].Value == doingFeature.WorkItemAge.ToString())),
                Times.Once);
        }

        [Test]
        public async Task TriggerWriteBackForTeam_LogsStartAndCompletion()
        {
            var team = CreateTeamWithWorkItems();
            team.WorkTrackingSystemConnection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.WorkItemAge,
                AppliesTo = WriteBackAppliesTo.Team,
                TargetFieldReference = "Custom.Age",
            });

            var doingItem = CreateWorkItem("701", StateCategories.Doing, team, startedDate: DateTime.UtcNow.AddDays(-2));
            SetupWorkItemsForTeam(team.Id, [doingItem]);

            var subject = CreateSubject();

            await subject.TriggerWriteBackForTeam(team);

            var infoInvocations = loggerMock.Invocations
                .Where(i => (LogLevel)i.Arguments[0] == LogLevel.Information)
                .ToList();

            Assert.That(infoInvocations, Has.Count.GreaterThanOrEqualTo(1));
        }

        private WriteBackTriggerService CreateSubject()
        {
            return new WriteBackTriggerService(
                writeBackServiceMock.Object,
                licenseServiceMock.Object,
                workItemRepositoryMock.Object,
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
                CreatedDate = startedDate ?? DateTime.UtcNow.AddDays(-30),
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
                CreatedDate = startedDate ?? DateTime.UtcNow.AddDays(-30),
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

            if (simulationResults.Count > 0)
            {
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
            }

            return feature;
        }

        private void SetupWorkItemsForTeam(int teamId, List<WorkItem> workItems)
        {
            workItemRepositoryMock
                .Setup(r => r.GetAllByPredicate(It.IsAny<System.Linq.Expressions.Expression<Func<WorkItem, bool>>>()))
                .Returns(workItems.AsQueryable());
        }
    }
}
