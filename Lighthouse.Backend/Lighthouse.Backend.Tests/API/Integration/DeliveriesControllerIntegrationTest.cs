using System.Net;
using System.Text;
using System.Text.Json;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.API.Integration
{
    public class DeliveriesControllerIntegrationTest() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task CreateDelivery_WithMissingName_ReturnsBadRequest()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);
            
            var request = new CreateDeliveryRequest
            {
                // Missing Name
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = features.Select(f => f.Id).ToList(),
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        
        [Test]
        public async Task CreateDelivery_PortfolioDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var portfolioId = 123213;
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);
            
            var request = new CreateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = features.Select(f => f.Id).ToList(),
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolioId}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }

        [Test]
        public async Task CreateDelivery_WithInvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            
            var invalidJson = "invalid json structure";
            var content = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }
        
        [Test]
        public async Task CreateDelivery_FeaturesDontExist_ReturnsNotFound()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            
            var request = new CreateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [999, 1000] // Non-existent feature IDs
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
        
        [Test]
        public async Task CreateDelivery_WithValidData_ReturnsOk()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            
            var features = await AddFeatures(portfolio);
            
            var request = new CreateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = features.Select(f => f.Id).ToList()
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", content);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
        
        [Test]
        public async Task CreateDelivery_RemoveDelivery_WorksAsExpected()
        {
            // Arrange
            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();
            
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);
            
            var request = new CreateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = features.Select(f => f.Id).ToList(),
            };
            
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act - Create Delivery
            var createResponse = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", content);
            createResponse.EnsureSuccessStatusCode();
            
            Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            
            // Act - Get Created Delivery
            var getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            getResponse.EnsureSuccessStatusCode();
            var getResponseContent = await getResponse.Content.ReadAsStringAsync();
            var deliveries = JsonSerializer.Deserialize<List<Delivery>>(getResponseContent,
                new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true }
            );
            var createdDelivery = deliveries.FirstOrDefault(d => d.Name == "Release 1");
            Assert.That(createdDelivery, Is.Not.Null);

            // Act - Delete Delivery
            var deleteResponse = await Client.DeleteAsync($"/api/deliveries/{createdDelivery.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // Act - Try to Get Deleted Delivery
            getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            getResponse.EnsureSuccessStatusCode();
            getResponseContent = await getResponse.Content.ReadAsStringAsync();
            deliveries = JsonSerializer.Deserialize<List<Delivery>>(getResponseContent,
                new JsonSerializerOptions
                    { PropertyNameCaseInsensitive = true }
            );

            // Assert
            Assert.That(deliveries, Has.Count.EqualTo(0));
            
            var portfolioRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            var dbPortfolio = portfolioRepository.GetById(portfolio.Id);
            Assert.That(dbPortfolio, Is.Not.Null);

            var allFeatures = featureRepository.GetAll().Select(x => x.Id).ToList();
            foreach (var feature in features)
            {
                Assert.That(allFeatures, Contains.Item(feature.Id));
            }
        }

        private async Task<Portfolio> AddPortfolio()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            
            var team = new Team
            {
                Name = "Test Team",
                WorkTrackingSystemConnection =  workTrackingSystemConnection
            };
            
            var teamRepository = ServiceProvider.GetService<IRepository<Team>>();
            teamRepository.Add(team);
            
            await teamRepository.Save();
            
            var portfolio = new Portfolio
            {
                Name = "Test Portfolio",
                WorkTrackingSystemConnection =  workTrackingSystemConnection,
            };
            
            portfolio.Teams.Add(team);
            
            var portfolioRepository = ServiceProvider.GetService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }

        private async Task<List<Feature>> AddFeatures(Portfolio portfolio)
        {
            var featureRepository = ServiceProvider.GetService<IRepository<Feature>>();

            var features = new List<Feature>();

            var team = portfolio.Teams.Single();

            for (int i = 1; i <= 3; i++)
            {
                var feature = new Feature(team, 10)
                {
                    Name = $"Feature {i}",
                    Order = "12",
                };

                featureRepository.Add(feature);
                features.Add(feature);
            }

            portfolio.UpdateFeatures(features);
            await featureRepository.Save();

            return featureRepository.GetAll().ToList();
        }
    }
}