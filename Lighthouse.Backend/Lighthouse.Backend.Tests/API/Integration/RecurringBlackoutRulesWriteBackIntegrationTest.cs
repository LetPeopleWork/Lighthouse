using System.Net;
using System.Net.Http.Json;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesWriteBackIntegrationTest
    {
        private const int WorkingDaysToCompletion = 10;
        private const string FieldReference = "customfield_forecast";

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private Mock<IWriteBackService> writeBackServiceMock = null!;
        private Mock<ILicenseService> licenseServiceMock = null!;
        private List<WriteBackFieldUpdate> capturedUpdates = null!;

        private static DateTime Today => DateTime.UtcNow.Date;

        [SetUp]
        public void Init()
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            capturedUpdates = [];
            writeBackServiceMock = new Mock<IWriteBackService>();
            writeBackServiceMock
                .Setup(s => s.WriteFieldsToWorkItems(It.IsAny<WorkTrackingSystemConnection>(), It.IsAny<IReadOnlyList<WriteBackFieldUpdate>>()))
                .Callback((WorkTrackingSystemConnection _, IReadOnlyList<WriteBackFieldUpdate> updates) => capturedUpdates.AddRange(updates))
                .ReturnsAsync(new WriteBackResult());

            licenseServiceMock = new Mock<ILicenseService>();
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);

            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory)
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IWriteBackService>();
                        services.AddScoped(_ => writeBackServiceMock.Object);
                        services.RemoveAll<ITeamMetricsService>();
                        services.AddScoped(_ => Mock.Of<ITeamMetricsService>());
                        services.RemoveAll<IPortfolioMetricsService>();
                        services.AddScoped(_ => Mock.Of<IPortfolioMetricsService>());
                        services.RemoveAll<ILicenseService>();
                        services.AddScoped(_ => licenseServiceMock.Object);
                    });
                });

            client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();
        }

        [TearDown]
        public void Cleanup()
        {
            using (var scope = factory.Services.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<LighthouseAppContext>().Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task TriggerForecastWriteBack_FeatureWithRecurringRuleDays_WritesTheShiftedDate()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);

            var portfolio = PortfolioWithForecastedFeature(WorkingDaysToCompletion);
            await TriggerWriteBack(portfolio);

            var written = capturedUpdates.Single().Value;
            Assert.That(written, Is.EqualTo(Today.AddDays(12).ToString("yyyy-MM-dd")));
        }

        [Test]
        public async Task TriggerForecastWriteBack_RecurringRuleDays_WritesSameDateAsEquivalentOneOffPeriod()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));

            ConfigureOneOffBlackoutPeriod(firstBlackoutDay, secondBlackoutDay);
            await TriggerWriteBack(PortfolioWithForecastedFeature(WorkingDaysToCompletion));
            var oneOffWritten = capturedUpdates.Single().Value;

            capturedUpdates.Clear();
            RemoveAllOneOffPeriods();
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);
            await TriggerWriteBack(PortfolioWithForecastedFeature(WorkingDaysToCompletion));
            var recurringWritten = capturedUpdates.Single().Value;

            Assert.That(recurringWritten, Is.EqualTo(oneOffWritten),
                "Write-back must persist the same blackout-shifted date for a recurring-rule day as for an equivalent one-off period (D4 unified evaluation).");
        }

        [Test]
        public async Task TriggerForecastWriteBack_NoRecurringRulesAndNoOneOffPeriods_WritesTheUnchangedDate()
        {
            await TriggerWriteBack(PortfolioWithForecastedFeature(WorkingDaysToCompletion));

            var written = capturedUpdates.Single().Value;
            Assert.That(written, Is.EqualTo(Today.AddDays(WorkingDaysToCompletion).ToString("yyyy-MM-dd")));
        }

        private async Task TriggerWriteBack(Portfolio portfolio)
        {
            using var scope = factory.Services.CreateScope();
            var subject = scope.ServiceProvider.GetRequiredService<IWriteBackTriggerService>();
            await subject.TriggerForecastWriteBackForPortfolio(portfolio);
        }

        private async Task ConfigureRecurringRuleCovering(DateOnly firstDay, DateOnly secondDay)
        {
            client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { firstDay.DayOfWeek.ToString(), secondDay.DayOfWeek.ToString() },
                intervalWeeks = 1,
                start = firstDay.ToString("yyyy-MM-dd"),
                end = secondDay.ToString("yyyy-MM-dd"),
                description = "Equivalent recurring window",
            };

            var response = await client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await response.Content.ReadAsStringAsync());
        }

        private void ConfigureOneOffBlackoutPeriod(DateOnly start, DateOnly end)
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            repository.Add(new BlackoutPeriod { Start = start, End = end, Description = "Company shutdown" });
            repository.Save().GetAwaiter().GetResult();
        }

        private void RemoveAllOneOffPeriods()
        {
            using var scope = factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            foreach (var period in repository.GetAll().ToList())
            {
                repository.Remove(period);
            }
            repository.Save().GetAwaiter().GetResult();
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
