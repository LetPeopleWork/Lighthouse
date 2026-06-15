using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [Category("recurring-blackout-events")]
    public class RecurringBlackoutRulesDeliveryIntegrationTest : RecurringBlackoutRulesTestBase
    {
        private const int WorkingDaysToCompletion = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        [SetUp]
        public void Init()
        {
            StartApplicationWithDeterministicForecast();
        }

        [TearDown]
        public void Cleanup()
        {
            StopApplication();
        }

        [Test]
        public async Task GetDelivery_FeatureWithRecurringRuleDays_FeaturePercentileDateStepsOverTheRecurringSpan()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);

            var portfolioId = await SeedPortfolioWithForecastedFeature();
            await CreateDelivery(portfolioId);

            var delivery = await GetSingleDelivery(portfolioId);

            var completionDate = delivery.FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;
            Assert.That(completionDate, Is.EqualTo(Today.AddDays(12)));
        }

        [Test]
        public async Task GetDelivery_RecurringRuleDays_FeaturePercentileDateIdenticalToEquivalentOneOffPeriod()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));

            ConfigureOneOffBlackoutPeriod(firstBlackoutDay, secondBlackoutDay);
            var oneOffPortfolioId = await SeedPortfolioWithForecastedFeature();
            await CreateDelivery(oneOffPortfolioId);
            var oneOffDate = (await GetSingleDelivery(oneOffPortfolioId))
                .FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;

            RemoveAllOneOffPeriods();
            RemoveAllPortfolios();
            RemoveAllFeatures();
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);
            var recurringPortfolioId = await SeedPortfolioWithForecastedFeature();
            await CreateDelivery(recurringPortfolioId);
            var recurringDate = (await GetSingleDelivery(recurringPortfolioId))
                .FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;

            Assert.That(recurringDate, Is.EqualTo(oneOffDate),
                "A recurring-rule day must shift the delivery percentile date identically to a one-off blackout period (D4 unified evaluation).");
        }

        [Test]
        public async Task GetDelivery_FeatureWithRecurringRuleDays_LikelihoodComputedOnTheWorkingDayCount()
        {
            var firstBlackoutDay = DateOnly.FromDateTime(Today.AddDays(3));
            var secondBlackoutDay = DateOnly.FromDateTime(Today.AddDays(4));
            await ConfigureRecurringRuleCovering(firstBlackoutDay, secondBlackoutDay);

            var portfolioId = await SeedPortfolioWithForecast(ForecastSplitBetweenNineAndElevenWorkingDays());
            await CreateDelivery(portfolioId, dueDateDaysOut: 11);

            var delivery = await GetSingleDelivery(portfolioId);

            Assert.That(delivery.FeatureLikelihoods.Single().LikelihoodPercentage, Is.EqualTo(50),
                "GetLikelhoodForDate counts working days excluding recurring-rule days; the 2 recurring days consume 2 of the 11 calendar days, leaving 9 working days. Half the forecast trials complete in 9 days and half in 11, so the likelihood is 50 — without the recurring days the full 11-day window would yield 100.");
        }

        private async Task ConfigureRecurringRuleCovering(DateOnly firstDay, DateOnly secondDay)
        {
            Client.AsSystemAdmin();
            var rule = new
            {
                weekdays = new[] { firstDay.DayOfWeek.ToString(), secondDay.DayOfWeek.ToString() },
                intervalWeeks = 1,
                start = firstDay.ToString("yyyy-MM-dd"),
                end = secondDay.ToString("yyyy-MM-dd"),
                description = "Equivalent recurring window",
            };

            var response = await Client.PostAsJsonAsync("/api/latest/recurring-blackout-rules", rule);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created),
                await response.Content.ReadAsStringAsync());
        }

        private void RemoveAllOneOffPeriods()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            foreach (var period in repository.GetAll().ToList())
            {
                repository.Remove(period);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private void RemoveAllPortfolios()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            foreach (var portfolio in repository.GetAll().ToList())
            {
                repository.Remove(portfolio);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private void RemoveAllFeatures()
        {
            using var scope = Factory.Services.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            foreach (var feature in repository.GetAll().ToList())
            {
                repository.Remove(feature);
            }
            repository.Save().GetAwaiter().GetResult();
        }

        private Task<int> SeedPortfolioWithForecastedFeature()
        {
            return SeedPortfolioWithForecast(ForecastCompletingInWorkingDays(WorkingDaysToCompletion));
        }

        private async Task<int> SeedPortfolioWithForecast(WhenForecast forecast)
        {
            using var scope = Factory.Services.CreateScope();

            var connection = new WorkTrackingSystemConnection { Name = $"Connection {Guid.NewGuid():N}", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = $"Delivery Team {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };

            var teamRepository = scope.ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var feature = new Feature(team, 5) { Name = "Feature", Order = "12" };
            feature.SetFeatureForecasts([forecast]);

            var portfolio = new Portfolio { Name = $"Test Portfolio {Guid.NewGuid():N}", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = scope.ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolio.Id;
        }

        private static WhenForecast ForecastSplitBetweenNineAndElevenWorkingDays()
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[9] = 50;
            simulation.SimulationResults[11] = 50;
            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private async Task CreateDelivery(int portfolioId, int dueDateDaysOut = 30)
        {
            using var scope = Factory.Services.CreateScope();
            var featureRepository = scope.ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var featureIds = featureRepository.GetAll().Select(f => f.Id).ToList();

            var request = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = Today.AddDays(dueDateDaysOut),
                FeatureIds = featureIds,
                SelectionMode = DeliverySelectionMode.Manual,
            };

            Client.AsPortfolioAdmin(portfolioId);
            var response = await Client.PostAsync($"/api/latest/deliveries/portfolio/{portfolioId}", JsonContent.Create(request));
            response.EnsureSuccessStatusCode();
        }

        private async Task<DeliveryWithLikelihoodDto> GetSingleDelivery(int portfolioId)
        {
            Client.AsPortfolioViewer(portfolioId);
            var response = await Client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(body, JsonOptions)!;
            return deliveries.Single();
        }
    }
}
