using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        private static readonly JsonSerializerOptions JsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        [Test]
        public async Task CreateDelivery_WithMissingName_ReturnsBadRequest()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);
            
            var request = new UpdateDeliveryRequest
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
            
            var request = new UpdateDeliveryRequest
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
            
            var request = new UpdateDeliveryRequest
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
            
            var request = new UpdateDeliveryRequest
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
            
            var request = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = features.Select(f => f.Id).ToList(),
                SelectionMode = DeliverySelectionMode.Manual
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
            var deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(getResponseContent,
                JsonSerializerOptions
            );

            if (deliveries == null)
            {
                throw new NullReferenceException("Deliveries deserialization resulted in null");
            }
            
            var createdDelivery = deliveries.FirstOrDefault(d => d.Name == "Release 1");
            Assert.That(createdDelivery, Is.Not.Null);

            // Act - Delete Delivery
            var deleteResponse = await Client.DeleteAsync($"/api/deliveries/{createdDelivery.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            // Act - Try to Get Deleted Delivery
            getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            getResponse.EnsureSuccessStatusCode();
            getResponseContent = await getResponse.Content.ReadAsStringAsync();
            deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(getResponseContent,
                JsonSerializerOptions
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

        [Test]
        public async Task UpdateDelivery_ManualSelection_ChangeFeatures_ReturnsOk()
        {
            // Arrange - create portfolio with team and features
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);

            // Create a manual delivery with features 0 and 1
            var createRequest = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[0].Id, features[1].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };

            var createJson = JsonSerializer.Serialize(createRequest);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", createContent);
            createResponse.EnsureSuccessStatusCode();

            // Get created delivery ID
            var getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            getResponse.EnsureSuccessStatusCode();
            var deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await getResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
            var createdDelivery = deliveries!.Single(d => d.Name == "Release 1");

            // Act - Update delivery: remove feature 1, add feature 2
            var updateRequest = new UpdateDeliveryRequest
            {
                Name = "Release 1 Updated",
                Date = DateTime.UtcNow.AddDays(35),
                FeatureIds = [features[0].Id, features[2].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };

            var updateJson = JsonSerializer.Serialize(updateRequest);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await Client.PutAsync($"/api/deliveries/{createdDelivery.Id}", updateContent);

            // Assert - update should succeed
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Verify persisted state
            var verifyResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            verifyResponse.EnsureSuccessStatusCode();
            var updatedDeliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await verifyResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
            var updatedDelivery = updatedDeliveries!.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updatedDelivery.Name, Is.EqualTo("Release 1 Updated"));
                Assert.That(updatedDelivery.Features, Is.EquivalentTo(new[] { features[0].Id, features[2].Id }));
            }
        }

        [Test]
        public async Task UpdateDelivery_ManualSelection_NameOnlyChange_ReturnsOk()
        {
            // Arrange - create portfolio with team and features
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);

            // Create a manual delivery with features 0 and 1
            var createRequest = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[0].Id, features[1].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };

            var createJson = JsonSerializer.Serialize(createRequest);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", createContent);
            createResponse.EnsureSuccessStatusCode();

            // Get created delivery ID
            var getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            getResponse.EnsureSuccessStatusCode();
            var deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await getResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
            var createdDelivery = deliveries!.Single(d => d.Name == "Release 1");

            // Act - Update only name; same features
            var updateRequest = new UpdateDeliveryRequest
            {
                Name = "Release 1 Renamed",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[0].Id, features[1].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };

            var updateJson = JsonSerializer.Serialize(updateRequest);
            var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");
            var updateResponse = await Client.PutAsync($"/api/deliveries/{createdDelivery.Id}", updateContent);

            // Assert
            Assert.That(updateResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Verify persisted state
            var verifyResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            verifyResponse.EnsureSuccessStatusCode();
            var updatedDeliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await verifyResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
            var updatedDelivery = updatedDeliveries!.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(updatedDelivery.Name, Is.EqualTo("Release 1 Renamed"));
                Assert.That(updatedDelivery.Features, Is.EquivalentTo(new[] { features[0].Id, features[1].Id }));
            }
        }

        [Test]
        public async Task UpdateDelivery_ManualSelection_ConsecutiveUpdates_ReturnsOk()
        {
            // Arrange
            var portfolio = await AddPortfolio();
            var features = await AddFeatures(portfolio);

            var createRequest = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[0].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };

            var createJson = JsonSerializer.Serialize(createRequest);
            var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
            var createResponse = await Client.PostAsync($"/api/deliveries/portfolio/{portfolio.Id}", createContent);
            createResponse.EnsureSuccessStatusCode();

            var getResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            var deliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await getResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
            var deliveryId = deliveries!.Single().Id;

            // Act - First update: add feature
            var update1 = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[0].Id, features[1].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };
            var update1Response = await Client.PutAsync($"/api/deliveries/{deliveryId}",
                new StringContent(JsonSerializer.Serialize(update1), Encoding.UTF8, "application/json"));
            Assert.That(update1Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            // Act - Second update: swap features
            var update2 = new UpdateDeliveryRequest
            {
                Name = "Release 1",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [features[1].Id, features[2].Id],
                SelectionMode = DeliverySelectionMode.Manual
            };
            var update2Response = await Client.PutAsync($"/api/deliveries/{deliveryId}",
                new StringContent(JsonSerializer.Serialize(update2), Encoding.UTF8, "application/json"));

            // Assert
            Assert.That(update2Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var verifyResponse = await Client.GetAsync($"/api/deliveries/portfolio/{portfolio.Id}");
            var updatedDeliveries = JsonSerializer.Deserialize<List<DeliveryWithLikelihoodDto>>(
                await verifyResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);

            Assert.That(updatedDeliveries!.Single().Features, Is.EquivalentTo(new[] { features[1].Id, features[2].Id }));
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
            
            portfolio.UpdateFeatures([new Feature(team, 12){ Name = "Feature", Order = "12" }]);
            
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