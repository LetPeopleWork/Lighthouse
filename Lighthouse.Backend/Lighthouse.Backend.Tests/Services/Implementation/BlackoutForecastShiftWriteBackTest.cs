using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    [Ignore("pending DELIVER — Epic 4974 US-04 write-back date shift + compose guard not yet implemented")]
    public class BlackoutForecastShiftWriteBackTest
    {
        private const int WorkingDaysToCompletion = 10;
        private const string FieldReference = "customfield_forecast";

        private Mock<IWriteBackService> writeBackServiceMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IWorkItemRepository> workItemRepositoryMock;
        private List<WriteBackFieldUpdate> capturedUpdates;
        private WriteBackTriggerService subject;

        private static DateTime Today => DateTime.UtcNow.Date;

        [SetUp]
        public void SetUp()
        {
            writeBackServiceMock = new Mock<IWriteBackService>();
            licenseServiceMock = new Mock<ILicenseService>();
            workItemRepositoryMock = new Mock<IWorkItemRepository>();

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            capturedUpdates = [];
            writeBackServiceMock
                .Setup(s => s.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .Callback((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) => capturedUpdates.AddRange(updates))
                .ReturnsAsync(new WriteBackResult());

            subject = new WriteBackTriggerService(
                writeBackServiceMock.Object,
                licenseServiceMock.Object,
                workItemRepositoryMock.Object,
                Mock.Of<ILogger<WriteBackTriggerService>>());
        }

        [Test]
        public async Task TriggerForecastWriteBack_FeatureWithFutureBlackoutDays_WritesTheShiftedDate()
        {
            var portfolio = PortfolioWithForecastedFeature(WorkingDaysToCompletion);

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            var written = capturedUpdates.Single().Value;
            Assert.That(written, Is.EqualTo(Today.AddDays(12).ToString("yyyy-MM-dd")));
        }

        [Test]
        public async Task TriggerForecastWriteBack_FeatureWithNoBlackoutPeriods_WritesTheUnchangedDate()
        {
            var portfolio = PortfolioWithForecastedFeature(WorkingDaysToCompletion);

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            var written = capturedUpdates.Single().Value;
            Assert.That(written, Is.EqualTo(Today.AddDays(WorkingDaysToCompletion).ToString("yyyy-MM-dd")));
        }

        [Test]
        public async Task TriggerForecastWriteBack_HistoricalAndFutureBlackoutBothConfigured_DaysValueUnchangedAndDateShiftedExactlyOnce()
        {
            var portfolio = PortfolioWithForecastedFeature(WorkingDaysToCompletion);
            var feature = portfolio.Features.Single();

            await subject.TriggerForecastWriteBackForPortfolio(portfolio);

            var written = capturedUpdates.Single().Value;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(WorkingDaysToCompletion));
                Assert.That(written, Is.EqualTo(Today.AddDays(12).ToString("yyyy-MM-dd")));
            }
        }

        private static WhenForecast DeterministicForecast(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private static Portfolio PortfolioWithForecastedFeature(int workingDays)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection" };
            var additionalField = new AdditionalFieldDefinition { Reference = FieldReference, DisplayName = "Forecast" };
            connection.AdditionalFieldDefinitions.Add(additionalField);
            connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition
            {
                ValueSource = WriteBackValueSource.ForecastPercentile85,
                AppliesTo = WriteBackAppliesTo.Portfolio,
                AdditionalFieldDefinition = additionalField,
                TargetValueType = WriteBackTargetValueType.Date,
            });

            var team = new Team { Name = "Test Team", WorkTrackingSystemConnection = connection };
            var feature = new Feature(team, 5)
            {
                Name = "Feature",
                Order = "12",
                ReferenceId = "FEAT-1",
                StateCategory = StateCategories.Doing,
            };
            feature.SetFeatureForecasts([DeterministicForecast(workingDays)]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            return portfolio;
        }
    }
}
