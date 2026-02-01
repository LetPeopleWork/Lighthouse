using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class DeliveryRulesControllerTest
    {
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<IDeliveryRuleService> deliveryRuleServiceMock;
        private DeliveryRulesController subject;

        [SetUp]
        public void SetUp()
        {
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            deliveryRuleServiceMock = new Mock<IDeliveryRuleService>();
            
            subject = new DeliveryRulesController(
                portfolioRepositoryMock.Object,
                deliveryRuleServiceMock.Object);
        }

        [Test]
        public void GetSchema_PortfolioNotFound_ReturnsNotFound()
        {
            portfolioRepositoryMock.Setup(r => r.GetById(1)).Returns((Portfolio?)null);

            var result = subject.GetSchema(1);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public void GetSchema_ValidPortfolio_ReturnsSchema()
        {
            var portfolio = CreateTestPortfolio();
            var schema = new DeliveryRuleSchema
            {
                Fields = [new DeliveryRuleFieldDefinition { FieldKey = "feature.type", DisplayName = "Type" }],
                Operators = ["equals", "notEquals", "contains"],
                MaxRules = 20,
                MaxValueLength = 500
            };
            
            portfolioRepositoryMock.Setup(r => r.GetById(1)).Returns(portfolio);
            deliveryRuleServiceMock.Setup(s => s.GetRuleSchema(portfolio)).Returns(schema);

            var result = subject.GetSchema(1);

            Assert.That(result, Is.TypeOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            Assert.That(okResult.Value, Is.EqualTo(schema));
        }

        [Test]
        public void Validate_PortfolioNotFound_ReturnsNotFound()
        {
            portfolioRepositoryMock.Setup(r => r.GetById(1)).Returns((Portfolio?)null);
            var request = new ValidateDeliveryRulesRequest { Rules = [] };

            var result = subject.Validate(1, request);

            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public void Validate_InvalidRules_ReturnsBadRequest()
        {
            var portfolio = CreateTestPortfolio();
            portfolioRepositoryMock.Setup(r => r.GetById(1)).Returns(portfolio);
            
            deliveryRuleServiceMock
                .Setup(s => s.GetMatchingFeaturesForRuleset(It.IsAny<DeliveryRuleSet>(), It.IsAny<IEnumerable<Feature>>()))
                .Returns([]);

            var request = new ValidateDeliveryRulesRequest { Rules = [] };
            var result = subject.Validate(1, request);
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
                var badResult = (BadRequestObjectResult)result;
                Assert.That(badResult.Value, Is.TypeOf<List<FeatureDto>>());
                var response = (List<FeatureDto>)badResult.Value!;
                
                Assert.That(response, Is.Empty);
            }
        }

        [Test]
        public void Validate_ValidRules_ReturnsMatchingFeatures()
        {
            var portfolio = CreateTestPortfolio();
            var feature = CreateTestFeature();
            portfolio.Features.Add(feature);
            
            portfolioRepositoryMock.Setup(r => r.GetById(1)).Returns(portfolio);
            
            deliveryRuleServiceMock
                .Setup(s => s.GetMatchingFeaturesForRuleset(It.IsAny<DeliveryRuleSet>(), It.IsAny<IEnumerable<Feature>>()))
                .Returns([feature]);

            var request = new ValidateDeliveryRulesRequest
            {
                Rules = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };
            var result = subject.Validate(1, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkObjectResult>());
                var okResult = (OkObjectResult)result;
                Assert.That(okResult.Value, Is.TypeOf<List<FeatureDto>>());
                var response = (List<FeatureDto>)okResult.Value!;
                
                Assert.That(response, Has.Count.EqualTo(1));
            }
        }

        private static Portfolio CreateTestPortfolio()
        {
            return new Portfolio
            {
                Id = 1,
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection()
            };
        }

        private static Feature CreateTestFeature()
        {
            return new Feature
            {
                Id = 1,
                ReferenceId = "F-1",
                Name = "Test Feature",
                Type = "Feature",
                State = "In Progress"
            };
        }
    }
}
