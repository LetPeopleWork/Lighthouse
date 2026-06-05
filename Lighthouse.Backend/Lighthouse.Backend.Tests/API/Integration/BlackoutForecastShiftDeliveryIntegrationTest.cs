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
    [Ignore("pending DELIVER — Epic 4974 US-03 feature/delivery date shift not yet implemented")]
    public class BlackoutForecastShiftDeliveryIntegrationTest() : IntegrationTestBase
    {
        private const int WorkingDaysToCompletion = 10;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private static DateTime Today => DateTime.UtcNow.Date;

        [Test]
        public async Task GetDelivery_FeatureWithFutureBlackoutDays_FeaturePercentileDateStepsOverTheBlackoutSpan()
        {
            var portfolio = await SeedPortfolioWithForecastedFeature();
            ConfigureBlackoutPeriod(Today.AddDays(3), Today.AddDays(4));
            await CreateDelivery(portfolio);

            var delivery = await GetSingleDelivery(portfolio.Id);

            var completionDate = delivery.FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;
            Assert.That(completionDate, Is.EqualTo(Today.AddDays(12)));
        }

        [Test]
        public async Task GetDelivery_FeatureWithNoBlackoutPeriods_FeaturePercentileDateUnchanged()
        {
            var portfolio = await SeedPortfolioWithForecastedFeature();
            await CreateDelivery(portfolio);

            var delivery = await GetSingleDelivery(portfolio.Id);

            var completionDate = delivery.FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;
            Assert.That(completionDate, Is.EqualTo(Today.AddDays(WorkingDaysToCompletion)));
        }

        [Test]
        public async Task GetDelivery_MultiTeamFeatureWithBlackoutDays_StillForecastsPerTeamThenStepsTheWorstCaseDate()
        {
            var portfolio = await SeedPortfolioWithMultiTeamForecastedFeature();
            ConfigureBlackoutPeriod(Today.AddDays(3), Today.AddDays(4));
            await CreateDelivery(portfolio);

            var delivery = await GetSingleDelivery(portfolio.Id);

            var completionDate = delivery.FeatureLikelihoods.Single().CompletionDates.Single(d => d.Probability == 85).ExpectedDate;
            Assert.That(completionDate, Is.EqualTo(Today.AddDays(12)));
        }

        private void ConfigureBlackoutPeriod(DateTime start, DateTime end)
        {
            var repository = ServiceProvider.GetRequiredService<IRepository<BlackoutPeriod>>();
            repository.Add(new BlackoutPeriod
            {
                Start = DateOnly.FromDateTime(start),
                End = DateOnly.FromDateTime(end),
                Description = "Company shutdown",
            });
            repository.Save().GetAwaiter().GetResult();
        }

        private static WhenForecast DeterministicForecast(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private async Task<Portfolio> SeedPortfolioWithForecastedFeature()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var team = new Team { Name = "Test Team", WorkTrackingSystemConnection = connection };

            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            await teamRepository.Save();

            var feature = new Feature(team, 5) { Name = "Feature", Order = "12" };
            feature.SetFeatureForecasts([DeterministicForecast(WorkingDaysToCompletion)]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }

        private async Task<Portfolio> SeedPortfolioWithMultiTeamForecastedFeature()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var teamA = new Team { Name = "Team A", WorkTrackingSystemConnection = connection };
            var teamB = new Team { Name = "Team B", WorkTrackingSystemConnection = connection };

            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(teamA);
            teamRepository.Add(teamB);
            await teamRepository.Save();

            var feature = new Feature([(teamA, 3, 3), (teamB, 2, 2)]) { Name = "Feature", Order = "12" };
            var forecastTeamA = DeterministicForecast(WorkingDaysToCompletion);
            forecastTeamA.TeamId = teamA.Id;
            var forecastTeamB = DeterministicForecast(WorkingDaysToCompletion - 3);
            forecastTeamB.TeamId = teamB.Id;
            feature.SetFeatureForecasts([forecastTeamA, forecastTeamB]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }

        private async Task CreateDelivery(Portfolio portfolio)
        {
            var featureRepository = ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var featureIds = featureRepository.GetAll().Select(f => f.Id).ToList();

            var request = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = Today.AddDays(30),
                FeatureIds = featureIds,
                SelectionMode = DeliverySelectionMode.Manual,
            };

            Client.AsPortfolioViewer(portfolio.Id);
            var content = JsonContent.Create(request);
            var response = await Client.PostAsync($"/api/latest/deliveries/portfolio/{portfolio.Id}", content);
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
