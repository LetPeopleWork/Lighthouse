using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class DeliveryWithLikelihoodDtoTest
    {
        [Test]
        public void Should_Calculate_Likelihood_Based_On_Feature_Forecasts()
        {
            // Arrange
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            // Create a forecast that shows 75% likelihood to complete within 30 days
            var simulationResult = new Dictionary<int, int>
            {
                { 10, 25 }, // 25 simulations completed in 10 days 
                { 20, 25 }, // 25 simulations completed in 20 days
                { 30, 25 }, // 25 simulations completed in 30 days (total: 75 out of 100 = 75%)
                { 40, 25 }  // 25 simulations completed in 40 days
            };
            var forecast = new WhenForecast();
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, new object[] { simulationResult });
            
            var feature = new Feature();
            feature.Forecasts.Add(forecast);
            
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.Features.Add(feature);
            
            // Act
            var deliveryDto = DeliveryWithLikelihoodDto.FromDelivery(delivery);
            
            // Assert
            Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(75.0));
            Assert.That(deliveryDto.Id, Is.EqualTo(1));
            Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
            Assert.That(deliveryDto.Date, Is.EqualTo(deliveryDate));
        }
        
        [Test]
        public void Should_Return_Hundred_Likelihood_When_No_Features_Have_Forecasts()
        {
            // Arrange
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            var feature = new Feature();
            
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.Features.Add(feature);
            
            // Act
            var deliveryDto = DeliveryWithLikelihoodDto.FromDelivery(delivery);
            
            // Assert
            Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(100.0));
        }
        
        [Test]
        public void Should_Calculate_Minimum_Likelihood_Across_Multiple_Features()
        {
            // Arrange
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            // Feature 1: 80% likelihood to complete within 30 days
            var simulationResult1 = new Dictionary<int, int>
            {
                { 10, 20 },
                { 20, 30 },
                { 30, 30 }, // Total: 80 out of 100 = 80%
                { 40, 20 }
            };
            var forecast1 = new WhenForecast();
            forecast1.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast1, new object[] { simulationResult1 });
            
            // Feature 2: 60% likelihood to complete within 30 days
            var simulationResult2 = new Dictionary<int, int>
            {
                { 10, 10 },
                { 20, 20 },
                { 30, 30 }, // Total: 60 out of 100 = 60%
                { 40, 40 }
            };
            var forecast2 = new WhenForecast();
            forecast2.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast2, new object[] { simulationResult2 });
            
            var feature1 = new Feature();
            feature1.Forecasts.Add(forecast1);
            
            var feature2 = new Feature();
            feature2.Forecasts.Add(forecast2);
            
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.Features.Add(feature1);
            delivery.Features.Add(feature2);
            
            // Act
            var deliveryDto = DeliveryWithLikelihoodDto.FromDelivery(delivery);
            
            // Assert - Should return the lowest likelihood (most conservative)
            Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(60.0));
        }
        
        [Test]
        public void Should_Include_Individual_Feature_Likelihoods()
        {
            // Arrange
            var deliveryDate = DateTime.UtcNow.AddDays(30);
            
            // Feature 1: 80% likelihood
            var simulationResult1 = new Dictionary<int, int>
            {
                { 10, 20 },
                { 20, 30 },
                { 30, 30 }, // Total: 80 out of 100 = 80%
                { 40, 20 }
            };
            var forecast1 = new WhenForecast();
            forecast1.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast1, new object[] { simulationResult1 });
            
            // Feature 2: 60% likelihood
            var simulationResult2 = new Dictionary<int, int>
            {
                { 10, 10 },
                { 20, 20 },
                { 30, 30 }, // Total: 60 out of 100 = 60%
                { 40, 40 }
            };
            var forecast2 = new WhenForecast();
            forecast2.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast2, new object[] { simulationResult2 });
            
            var feature1 = new Feature { Id = 1 };
            feature1.Forecasts.Add(forecast1);
            
            var feature2 = new Feature { Id = 2 };
            feature2.Forecasts.Add(forecast2);
            
            var feature3 = new Feature { Id = 3 }; // No forecast
            
            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.Features.Add(feature1);
            delivery.Features.Add(feature2);
            delivery.Features.Add(feature3);
            
            // Act
            var deliveryDto = DeliveryWithLikelihoodDto.FromDelivery(delivery);
            
            // Assert
            Assert.That(deliveryDto.FeatureLikelihoods, Has.Count.EqualTo(3));
            Assert.That(deliveryDto.FeatureLikelihoods[0].FeatureId, Is.EqualTo(1));
            Assert.That(deliveryDto.FeatureLikelihoods[0].LikelihoodPercentage, Is.EqualTo(80.0));
            Assert.That(deliveryDto.FeatureLikelihoods[1].FeatureId, Is.EqualTo(2));
            Assert.That(deliveryDto.FeatureLikelihoods[1].LikelihoodPercentage, Is.EqualTo(60.0));
            Assert.That(deliveryDto.FeatureLikelihoods[2].FeatureId, Is.EqualTo(3));
            Assert.That(deliveryDto.FeatureLikelihoods[2].LikelihoodPercentage, Is.EqualTo(100.0));
        }
    }
}