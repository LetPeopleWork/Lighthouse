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
    // Story #5569 AC-01.8: the delivery likelihood of a multi-team feature is at most the likelihood of
    // any single contributing team. Both teams are seeded with the same two-value histogram, so a target
    // date on the median gives 50 % per team and 25 % jointly.
    [TestFixture]
    public class MultiTeamJointForecastDeliveryIntegrationTest() : IntegrationTestBase
    {
        private const int MedianDays = 5;
        private const int TailDays = 40;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private static DateTime Today => DateTime.UtcNow.Date;

        [Test]
        [Ignore("RED until Story #5569 implements the joint distribution")]
        public async Task GetDelivery_MultiTeamFeature_LikelihoodIsTheJointProbabilityNotTheWorstTeams()
        {
            var portfolio = await SeedPortfolioWithTwoEquallyForecastedTeams();
            await CreateDelivery(portfolio, Today.AddDays(MedianDays));

            var delivery = await GetSingleDelivery(portfolio.Id);

            var singleTeamLikelihood = SingleTeamForecast().GetLikelihood(MedianDays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(singleTeamLikelihood, Is.EqualTo(50).Within(0.01), "single contributing team");
                Assert.That(delivery.FeatureLikelihoods.Single().LikelihoodPercentage, Is.EqualTo(25).Within(0.01), "joint likelihood");
            }
        }

        private static WhenForecast SingleTeamForecast()
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[MedianDays] = 5000;
            simulation.SimulationResults[TailDays] = 5000;

            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private async Task<Portfolio> SeedPortfolioWithTwoEquallyForecastedTeams()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var teamA = new Team { Name = "Team A", WorkTrackingSystemConnection = connection };
            var teamB = new Team { Name = "Team B", WorkTrackingSystemConnection = connection };

            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(teamA);
            teamRepository.Add(teamB);
            await teamRepository.Save();

            var feature = new Feature([(teamA, 3, 3), (teamB, 3, 3)]) { Name = "Feature", Order = "12" };

            var forecastTeamA = SingleTeamForecast();
            forecastTeamA.TeamId = teamA.Id;
            var forecastTeamB = SingleTeamForecast();
            forecastTeamB.TeamId = teamB.Id;
            feature.SetFeatureForecasts([forecastTeamA, forecastTeamB]);

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([feature]);

            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }

        private async Task CreateDelivery(Portfolio portfolio, DateTime date)
        {
            var featureRepository = ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var featureIds = featureRepository.GetAll().Select(f => f.Id).ToList();

            var request = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = date,
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
