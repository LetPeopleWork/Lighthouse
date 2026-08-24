using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Archived;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Authorization;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API
{
    public class DeliveriesControllerTest
    {
        private Mock<IDeliveryRepository> deliveryRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;

        private Mock<ILicenseService> licenseServiceMock;

        private Mock<IDeliveryRuleService> deliveryRuleServiceMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;
        private Mock<IDeliveryMetricSnapshotRepository> deliveryMetricSnapshotRepositoryMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;
        private Mock<IDeliverySourceResolver> deliverySourceResolverMock;
        private Dictionary<string, PortfolioSourcePreview> releasesTheResolverAnswersFor;
        private Delivery? persistedDelivery;

        [SetUp]
        public void Setup()
        {
            deliveryRepositoryMock = new Mock<IDeliveryRepository>();
            portfolioRepositoryMock = new Mock<IRepository<Portfolio>>();
            licenseServiceMock = new Mock<ILicenseService>();
            deliveryRuleServiceMock = new Mock<IDeliveryRuleService>();
            rbacAdministrationServiceMock = new Mock<IRbacAdministrationService>();
            deliveryMetricSnapshotRepositoryMock = new Mock<IDeliveryMetricSnapshotRepository>();
            blackoutPeriodServiceMock = new Mock<IBlackoutPeriodService>();
            deliverySourceResolverMock = new Mock<IDeliverySourceResolver>();
            releasesTheResolverAnswersFor = [];
            persistedDelivery = null;
            deliveryRepositoryMock.Setup(x => x.Add(It.IsAny<Delivery>())).Callback<Delivery>(delivery => persistedDelivery = delivery);

            blackoutPeriodServiceMock
                .Setup(x => x.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<BlackoutPeriod>());

            deliveryMetricSnapshotRepositoryMock
                .Setup(x => x.GetSnapshotCountsByDelivery(It.IsAny<IEnumerable<int>>()))
                .Returns(new Dictionary<int, int>());

            deliveryRuleServiceMock.Setup(x =>
                    x.GetMatchingFeaturesForRuleset(It.IsAny<WorkItemRuleSet>(), It.IsAny<IEnumerable<Feature>>()))
                .Returns([]);

            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(new List<Feature>());

            portfolioRepositoryMock.Setup(x => x.GetById(It.IsAny<int>())).Returns(new Portfolio());
            rbacAdministrationServiceMock
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        [Test]
        public void GetByPortfolio_WithForecastedFeatures_ReturnsDeliveriesWithLikelihood()
        {
            // Arrange
            const int portfolioId = 1;
            var deliveryDate = DateTime.UtcNow.AddDays(30);

            // Create feature with 80% likelihood forecast
            var simulationResult = new Dictionary<int, int>
            {
                { 10, 20 },
                { 20, 30 },
                { 30, 30 }, // Total: 80 out of 100 = 80%
                { 40, 20 }
            };
            var forecast = new WhenForecast();
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, [simulationResult]);

            // Since Story #5587 the rollup enumerates FROM FeatureWork and LEFT JOINs Forecasts, so
            // the pair and its row have to name the same team or the delivery cannot be forecast.
            var team = new Team { Id = 1, Name = "Team" };
            forecast.TeamId = team.Id;

            var feature = new Feature();
            feature.Forecasts.Add(forecast);
            feature.FeatureWork.Add(new FeatureWork { Team = team, TeamId = team.Id, RemainingWorkItems = 12 });

            var delivery = new Delivery
            {
                Id = 1,
                Name = "Q1 Release",
                Date = deliveryDate
            };
            delivery.ReplaceFeatures([feature]);

            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns([delivery]);

            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var deliveries = (okResult.Value as PortfolioDeliveriesDto ?? throw new NullReferenceException("Deliveries is null")).Active;

            Assert.That(deliveries, Has.Count.EqualTo(1));

            var deliveryDto = deliveries[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDto.Id, Is.EqualTo(1));
                Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(80.0));
            }
        }

        [Test]
        public async Task CreateDelivery_ValidData_ReturnsOk()
        {
            // Arrange
            const int portfolioId = 1;
            const string name = "Q1 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int> { 1, 2 };

            var features = GetTestFeatures(featureIds);
            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(features);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            deliveryRepositoryMock.Verify(x => x.Add(It.IsAny<Delivery>()), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task CreateDelivery_PastDate_ReturnsBadRequest()
        {
            // Arrange
            const int portfolioId = 1;
            const string name = "Past Release";
            var pastDate = DateTime.UtcNow.AddDays(-1);
            var featureIds = new List<int>();

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = pastDate,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult.Value, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public async Task CreateDelivery_NonPremiumWithExistingDelivery_ReturnsForbidden()
        {
            // Arrange
            const int portfolioId = 1;
            const string name = "Q2 Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery> { GetTestDelivery() });

            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };

            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ObjectResult>());
                var objectResult = result as ObjectResult;
                Assert.That(objectResult.StatusCode, Is.EqualTo(403));
                Assert.That(objectResult.Value, Is.EqualTo("Free users can only have 1 delivery per portfolio"));
            }
        }

        [Test]
        public async Task CreateDelivery_NonPremiumFirstDelivery_ReturnsOk()
        {
            // Arrange
            const int portfolioId = 1;
            const string name = "First Release";
            var date = DateTime.UtcNow.AddDays(30);
            var featureIds = new List<int>();

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(new List<Delivery>());

            var controller = CreateSubject();

            // Act
            var request = new UpdateDeliveryRequest
            {
                Name = name,
                Date = date,
                FeatureIds = featureIds
            };
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
            deliveryRepositoryMock.Verify(x => x.Add(It.IsAny<Delivery>()), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task DeleteDelivery_ValidId_ReturnsNoContent()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = GetTestDelivery();

            deliveryRepositoryMock.Setup(x => x.GetPortfolioId(deliveryId))
                .Returns(existingDelivery.PortfolioId);

            var controller = CreateSubject();

            // Act
            var result = await controller.DeleteDelivery(deliveryId);

            // Assert
            Assert.That(result, Is.InstanceOf<NoContentResult>());
            deliveryRepositoryMock.Verify(x => x.Remove(deliveryId), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        /// <summary>
        /// Bug #5567, decision 2: the check is a day comparison, so the instance's own tomorrow is
        /// accepted even once UTC has already rolled past it. 02:00 UTC on the 11th is still the
        /// 10th in Los Angeles, which makes the 11th a future day for that instance.
        /// </summary>
        [Test]
        public async Task CreateDelivery_DatedTheInstanceTomorrowThatUtcHasAlreadyReached_IsAccepted()
        {
            var clock = new FakeLighthouseClock(
                new DateTimeOffset(2026, 3, 11, 2, 0, 0, TimeSpan.Zero),
                TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles"));

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject(clock);

            var result = await controller.CreateDelivery(1, new UpdateDeliveryRequest
            {
                Name = "Q1 Release",
                Date = new DateTime(2026, 3, 11, 0, 0, 0, DateTimeKind.Utc),
                FeatureIds = [],
            });

            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        /// <summary>
        /// Bug #5567, decision 2 - the deliberate behaviour change, UTC instances included: the
        /// check compares days, so a date that merely has a later CLOCK TIME today is no longer
        /// "in the future". It used to slip through purely because of its time component.
        /// </summary>
        [Test]
        public async Task CreateDelivery_DatedLaterTodayInTheInstanceZone_ReturnsBadRequest()
        {
            var clock = new FakeLighthouseClock(new DateTimeOffset(2026, 3, 10, 10, 0, 0, TimeSpan.Zero));

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject(clock);

            var result = await controller.CreateDelivery(1, new UpdateDeliveryRequest
            {
                Name = "Q1 Release",
                Date = new DateTime(2026, 3, 10, 23, 0, 0, DateTimeKind.Utc),
                FeatureIds = [],
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
                Assert.That(((BadRequestObjectResult)result).Value, Is.EqualTo("Delivery date must be in the future"));
            }
        }

        private DeliveriesController CreateSubject()
        {
            return CreateSubject(TestToday.Clock);
        }

        private DeliveriesController CreateSubject(ILighthouseClock clock)
        {
            return new DeliveriesController(
                deliveryRepositoryMock.Object,
                portfolioRepositoryMock.Object,
                licenseServiceMock.Object,
                deliveryRuleServiceMock.Object,
                rbacAdministrationServiceMock.Object,
                deliveryMetricSnapshotRepositoryMock.Object,
                blackoutPeriodServiceMock.Object,
                new DeliveryMetricValuesProjector(blackoutPeriodServiceMock.Object),
                clock,
                deliverySourceResolverMock.Object);
        }

        [Test]
        public void GetByPortfolio_HasPortfolioReadRbacGuardAttribute()
        {
            var method = typeof(DeliveriesController).GetMethod(nameof(DeliveriesController.GetByPortfolio));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute, Is.Not.Null);
                Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.PortfolioRead));
                Assert.That(attribute.ScopeIdRouteKey, Is.EqualTo("portfolioId"));
            }
        }

        [Test]
        public void CreateDelivery_HasPortfolioWriteRbacGuardAttribute()
        {
            var method = typeof(DeliveriesController).GetMethod(nameof(DeliveriesController.CreateDelivery));
            var attribute = method?
                .GetCustomAttributes(typeof(RbacGuardAttribute), inherit: true)
                .Cast<RbacGuardAttribute>()
                .SingleOrDefault();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(attribute, Is.Not.Null);
                Assert.That(attribute!.Requirement, Is.EqualTo(RbacGuardRequirement.PortfolioWrite));
                Assert.That(attribute.ScopeIdRouteKey, Is.EqualTo("portfolioId"));
            }
        }

        [Test]
        public async Task UpdateDelivery_WithoutPortfolioWrite_ReturnsForbidden()
        {
            const int deliveryId = 42;
            const int portfolioId = 7;
            var existingDelivery = new Delivery("Existing", DateTime.UtcNow.AddDays(10), portfolioId) { Id = deliveryId };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);
            rbacAdministrationServiceMock
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    portfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();
            var request = new UpdateDeliveryRequest
            {
                Name = "Renamed",
                Date = DateTime.UtcNow.AddDays(20),
                FeatureIds = []
            };

            var result = await controller.UpdateDelivery(deliveryId, request);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
            rbacAdministrationServiceMock.Verify(
                x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    portfolioId,
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task DeleteDelivery_WithoutPortfolioWrite_ReturnsForbidden()
        {
            const int deliveryId = 42;
            const int portfolioId = 7;

            deliveryRepositoryMock.Setup(x => x.GetPortfolioId(deliveryId)).Returns(portfolioId);
            rbacAdministrationServiceMock
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    portfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var controller = CreateSubject();

            var result = await controller.DeleteDelivery(deliveryId);

            Assert.That(result, Is.InstanceOf<ForbidResult>());
            deliveryRepositoryMock.Verify(x => x.Remove(It.IsAny<int>()), Times.Never);
        }

        private static List<Feature> GetTestFeatures(List<int> ids)
        {
            return ids.Select(id => new Feature
            {
                Id = id,
                Name = $"Feature {id}",
                Order = "1000",
                Type = "Epic",
                State = "New"
            }).ToList();
        }

        [Test]
        public void GetByPortfolio_WithFeaturesAndWork_ReturnsDeliveriesWithProgressAndFeatures()
        {
            // Arrange
            const int portfolioId = 1;
            var deliveryDate = DateTime.UtcNow.AddDays(30);

            // Create team and feature work
            var team = new Team { Id = 1, Name = "Test Team" };

            // Create feature with forecast and work
            var simulationResult = new Dictionary<int, int>
            {
                { 10, 20 },
                { 20, 30 },
                { 30, 30 }, // Total: 80 out of 100 = 80%
                { 40, 20 }
            };
            var forecast = new WhenForecast();
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, [simulationResult]);

            var feature = new Feature
            {
                Id = 1,
                Name = "Test Feature"
            };
            // The row has to name the pair's team since Story #5587's LEFT JOIN.
            forecast.TeamId = team.Id;
            feature.Forecasts.Add(forecast);

            var featureWork = new FeatureWork(team, 20, 100, feature); // 80% progress (80/100 completed)
            feature.FeatureWork.Add(featureWork);

            var delivery = new Delivery("Q1 Release", deliveryDate, portfolioId)
            {
                Id = 1
            };
            delivery.ReplaceFeatures([feature]);

            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns([delivery]);

            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            var deliveries = (okResult.Value as PortfolioDeliveriesDto ?? throw new NullReferenceException("Deliveries is null")).Active;

            Assert.That(deliveries, Has.Count.EqualTo(1));

            var deliveryDto = deliveries[0];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDto.Id, Is.EqualTo(1));
                Assert.That(deliveryDto.Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDto.PortfolioId, Is.EqualTo(portfolioId));

                Assert.That(deliveryDto.LikelihoodPercentage, Is.EqualTo(80.0));
                Assert.That(deliveryDto.Progress, Is.EqualTo(80.0)); // (100-20)/100 * 100 = 80%

                Assert.That(deliveryDto.RemainingWork, Is.EqualTo(20));
                Assert.That(deliveryDto.TotalWork, Is.EqualTo(100));
                Assert.That(deliveryDto.Features, Is.Not.Null);

                Assert.That(deliveryDto.Features, Has.Count.EqualTo(1));
                Assert.That(deliveryDto.Features[0], Is.EqualTo(1));
            }
        }

        [Test]
        public void GetByPortfolio_WithRecordedSnapshots_ReturnsMetricSnapshotCountPerDelivery()
        {
            const int portfolioId = 1;
            var deliveryWithout = new Delivery("No Snapshots", DateTime.UtcNow.AddDays(30), portfolioId) { Id = 10 };
            var deliveryWith = new Delivery("Four Snapshots", DateTime.UtcNow.AddDays(60), portfolioId) { Id = 20 };

            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns([deliveryWithout, deliveryWith]);
            deliveryMetricSnapshotRepositoryMock
                .Setup(x => x.GetSnapshotCountsByDelivery(It.IsAny<IEnumerable<int>>()))
                .Returns(new Dictionary<int, int> { { 20, 4 } });

            var controller = CreateSubject();

            var result = controller.GetByPortfolio(portfolioId);

            var okResult = result as OkObjectResult;
            var deliveries = (okResult!.Value as PortfolioDeliveriesDto)!.Active;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveries.Single(d => d.Id == 10).MetricSnapshotCount, Is.Zero);
                Assert.That(deliveries.Single(d => d.Id == 20).MetricSnapshotCount, Is.EqualTo(4));
            }
        }

        [Test]
        public void GetByPortfolio_ValidPortfolioId_ReturnsDeliveries()
        {
            // Arrange
            const int portfolioId = 1;
            var expectedDeliveries = new List<Delivery>
            {
                new("Q1 Release", DateTime.UtcNow.AddDays(30), portfolioId),
                new("Q2 Release", DateTime.UtcNow.AddDays(90), portfolioId)
            };

            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(expectedDeliveries);

            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var deliveryDtos = (okResult.Value as PortfolioDeliveriesDto ?? throw new NullReferenceException("DeliveryDtos is null")).Active;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDtos, Has.Count.EqualTo(2));
                Assert.That(deliveryDtos[0].Name, Is.EqualTo("Q1 Release"));
                Assert.That(deliveryDtos[^1].Name, Is.EqualTo("Q2 Release"));
            }
        }

        [Test]
        public void GetByPortfolio_ValidPortfolioId_ReturnsInCorrectOrder()
        {
            // Arrange
            const int portfolioId = 1;
            var expectedDeliveries = new List<Delivery>
            {
                new("Q1 Release", DateTime.UtcNow.AddDays(30), portfolioId),
                new("Hotfix Release", DateTime.UtcNow.AddDays(10), portfolioId)
            };

            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(portfolioId))
                .Returns(expectedDeliveries);

            var controller = CreateSubject();

            // Act
            var result = controller.GetByPortfolio(portfolioId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var deliveryDtos = (okResult.Value as PortfolioDeliveriesDto ?? throw new NullReferenceException("DeliveryDtos is null")).Active;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryDtos, Has.Count.EqualTo(2));
                Assert.That(deliveryDtos[0].Name, Is.EqualTo("Hotfix Release"));
                Assert.That(deliveryDtos[^1].Name, Is.EqualTo("Q1 Release"));
            }
        }

        [Test]
        public async Task UpdateDelivery_WithValidRequest_ReturnsOk()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Original Name", DateTime.UtcNow.AddDays(10), 1);
            var feature1 = new Feature { Id = 1, Name = "Feature 1" };
            var feature2 = new Feature { Id = 2, Name = "Feature 2" };

            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                FeatureIds = [1, 2]
            };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);
            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(new List<Feature> { feature1, feature2 });
            deliveryRepositoryMock.Setup(x => x.Save()).Returns(Task.CompletedTask);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(existingDelivery.Name, Is.EqualTo("Updated Delivery"));
                Assert.That(existingDelivery.Date, Is.EqualTo(request.Date));
                Assert.That(existingDelivery.Features, Has.Count.EqualTo(2));
            }
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task UpdateDelivery_WithPastDate_ReturnsBadRequest()
        {
            // Arrange
            const int deliveryId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(-1), // Past date
                FeatureIds = [1]
            };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(GetTestDelivery());

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest.Value, Is.EqualTo("Delivery date must be in the future"));
        }

        [Test]
        public async Task UpdateDelivery_WithEmptyName_ReturnsBadRequest()
        {
            // Arrange
            const int deliveryId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [1]
            };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(GetTestDelivery());

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
            var badRequest = result as BadRequestObjectResult;
            Assert.That(badRequest.Value, Is.EqualTo("Name is required"));
        }

        [Test]
        public async Task UpdateDelivery_WithNonExistentDelivery_ReturnsNotFound()
        {
            // Arrange
            const int deliveryId = 999;
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [1]
            };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns((Delivery)null);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound.Value, Is.EqualTo("Delivery with ID 999 not found"));
        }

        [Test]
        public async Task UpdateDelivery_WithNonExistentFeature_ReturnsNotFound()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Test", DateTime.UtcNow.AddDays(10), 1);
            var request = new UpdateDeliveryRequest
            {
                Name = "Test Delivery",
                Date = DateTime.UtcNow.AddDays(10),
                FeatureIds = [999]
            };

            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);
            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(new List<Feature>());

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundObjectResult>());
            var notFound = result as NotFoundObjectResult;
            Assert.That(notFound.Value, Is.EqualTo("Feature with ID 999 does not exist"));
        }

        private static Delivery GetTestDelivery()
        {
            return new Delivery("Existing Delivery", DateTime.UtcNow.AddDays(60), 1);
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithoutPremiumLicense_ReturnsForbidden()
        {
            // Arrange
            var portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.TypeOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult!.StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithEmptyRules_ReturnsBadRequest()
        {
            // Arrange
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = []
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithValidRules_SavesRuleDefinition()
        {
            // Arrange
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(savedDelivery, Is.Not.Null);
                Assert.That(savedDelivery!.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
                Assert.That(savedDelivery.RuleDefinitionJson, Is.Not.Null);
                Assert.That(savedDelivery.RuleSchemaVersion, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithModeOr_PersistsOrInRuleDefinitionJson()
        {
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery (OR)",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Mode = "or",
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock
                .Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();
            await controller.CreateDelivery(portfolioId, request);

            var storedRuleSet = WorkItemRuleSetJson.Deserialize(savedDelivery!.RuleDefinitionJson);
            Assert.That(storedRuleSet?.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithModeOmitted_DefaultsToAnd()
        {
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery (no mode)",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock
                .Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();
            await controller.CreateDelivery(portfolioId, request);

            var storedRuleSet = WorkItemRuleSetJson.Deserialize(savedDelivery!.RuleDefinitionJson);
            Assert.That(storedRuleSet?.Mode, Is.EqualTo(WorkItemRuleSet.ModeAnd));
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithValidRules_FeatureMatchesRules_SetsFeaturesCorrectly()
        {
            // Arrange
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Features = { new Feature { Id = 12 } }
            };

            portfolioRepositoryMock.Setup(x => x.GetById(portfolioId)).Returns(portfolio);
            deliveryRuleServiceMock.Setup(x =>
                    x.GetMatchingFeaturesForRuleset(It.IsAny<WorkItemRuleSet>(), It.IsAny<IEnumerable<Feature>>()))
                .Returns(portfolio.Features);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(savedDelivery, Is.Not.Null);

                Assert.That(savedDelivery.Features, Has.Count.EqualTo(1));
                Assert.That(savedDelivery.Features.Single().Id, Is.EqualTo(12));
            }
        }

        [Test]
        public async Task CreateDelivery_RuleBasedWithValidRules_FeatureDoesNotMatchRules_SetsFeaturesCorrectly()
        {
            // Arrange
            const int portfolioId = 1;
            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var portfolio = new Portfolio
            {
                Id = portfolioId,
                Features = { new Feature { Id = 12 } }
            };

            portfolioRepositoryMock.Setup(x => x.GetById(portfolioId)).Returns(portfolio);
            deliveryRuleServiceMock.Setup(x =>
                    x.GetMatchingFeaturesForRuleset(It.IsAny<WorkItemRuleSet>(), It.IsAny<IEnumerable<Feature>>()))
                .Returns([]);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(savedDelivery, Is.Not.Null);

                Assert.That(savedDelivery.Features, Has.Count.EqualTo(0));
            }
        }

        [Test]
        public async Task CreateDelivery_ManualWithBothRulesAndFeatures_UsesFeaturesIgnoresRules()
        {
            // Arrange
            const int portfolioId = 1;
            var featureIds = new List<int> { 1 };
            var features = GetTestFeatures(featureIds);
            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(features);

            var request = new UpdateDeliveryRequest
            {
                Name = "Manual Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.Manual,
                FeatureIds = featureIds,
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            Delivery? savedDelivery = null;
            deliveryRepositoryMock.Setup(x => x.Add(It.IsAny<Delivery>()))
                .Callback<Delivery>(d => savedDelivery = d);

            var controller = CreateSubject();

            // Act
            var result = await controller.CreateDelivery(portfolioId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(savedDelivery, Is.Not.Null);
                Assert.That(savedDelivery!.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(savedDelivery.RuleDefinitionJson, Is.Null);
                Assert.That(savedDelivery.Features, Has.Count.EqualTo(1));
            }
        }

        [Test]
        public async Task UpdateDelivery_RuleBasedWithoutPremiumLicense_ReturnsForbidden()
        {
            // Arrange
            var deliveryId = 1;
            var existingDelivery = new Delivery("Existing", DateTime.UtcNow.AddDays(60), 1) { Id = deliveryId };
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);

            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<ObjectResult>());
            var objectResult = result as ObjectResult;
            Assert.That(objectResult!.StatusCode, Is.EqualTo(403));
        }

        [Test]
        public async Task UpdateDelivery_RuleBasedWithEmptyRules_ReturnsBadRequest()
        {
            // Arrange
            var deliveryId = 1;
            var existingDelivery = new Delivery("Existing", DateTime.UtcNow.AddDays(60), 1) { Id = deliveryId };
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);

            var request = new UpdateDeliveryRequest
            {
                Name = "Updated Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = []
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            // Assert
            Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task UpdateDelivery_RuleBasedWithValidRules_SavesRuleDefinition()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Existing", DateTime.UtcNow.AddDays(60), 1) { Id = deliveryId };
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);

            var request = new UpdateDeliveryRequest
            {
                Name = "Rule-Based Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.RuleBased,
                FeatureIds = [],
                Rules = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Feature" }]
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(existingDelivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
                Assert.That(existingDelivery.RuleDefinitionJson, Is.Not.Null);
                Assert.That(existingDelivery.RuleSchemaVersion, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task UpdateDelivery_SwitchFromRuleBasedToManual_ClearsRuleDefinition()
        {
            // Arrange
            const int deliveryId = 1;
            var existingDelivery = new Delivery("Existing", DateTime.UtcNow.AddDays(60), 1)
            {
                Id = deliveryId,
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = "{\"Version\":1,\"Conditions\":[]}",
                RuleSchemaVersion = 1
            };
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(deliveryId)).Returns(existingDelivery);

            var featureIds = new List<int> { 1 };
            var features = GetTestFeatures(featureIds);
            deliveryRepositoryMock.Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>())).Returns(features);

            var request = new UpdateDeliveryRequest
            {
                Name = "Manual Delivery",
                Date = DateTime.UtcNow.AddDays(30),
                SelectionMode = DeliverySelectionMode.Manual,
                FeatureIds = featureIds,
                Rules = null
            };

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var controller = CreateSubject();

            // Act
            var result = await controller.UpdateDelivery(deliveryId, request);

            using (Assert.EnterMultipleScope())
            {
                // Assert
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(existingDelivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(existingDelivery.RuleDefinitionJson, Is.Null);
                Assert.That(existingDelivery.RuleSchemaVersion, Is.Null);
                Assert.That(existingDelivery.Features, Has.Count.EqualTo(1));
            }
        }

        private const string JiraReleaseSourceKey = "jira-release";
        private const string TheReleaseJiraHolds = "10412";
        private const string TheNameJiraHolds = "Autumn Release";
        private const string TheNameTheBrowserSent = "Autumn Release - typed by hand";

        private static readonly DateTime TheDayTheReleaseShipped = new(2026, 2, 3, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime TheDayTheBrowserSent = new(2026, 2, 2, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] TheWorkTaggedAgainstTheRelease = ["LGH-1"];

        private static readonly List<int> FeatureIdsThatWouldNotResolve = [999];

        /// <summary>
        /// What a Delivery ends up holding, read back off the aggregate that was handed to the
        /// repository. Named as a whole rather than asserted one property at a time, so a row that
        /// must NOT have gained a source binding says so in the same place as the row that must.
        /// </summary>
        public sealed record PersistedDelivery(
            string Name,
            DateTime Date,
            DeliverySelectionMode SelectionMode,
            string? SourceKey,
            string? SourceReference,
            bool HoldsARuleDefinition,
            int FeatureCount);

        /// <summary>
        /// The Release is what the Delivery follows, so its name, its date and the work tagged against
        /// it are what gets stored. Whatever the browser had on screen is neither compared with them
        /// nor refused for differing - it is simply never read, which is what keeps a browser that
        /// renders the day one off from ever being taken for somebody trying to edit the date.
        /// </summary>
        [Test]
        public async Task Creating_a_Delivery_from_a_Release_persists_Jiras_name_date_and_Features_and_ignores_whatever_the_client_sent()
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            GivenTheReleaseResolvesTo(AResolvedRelease(), AFeature(1));

            var result = await CreateSubject().CreateDelivery(1, ADeliveryFollowingTheRelease());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(WhatWasPersisted(), Is.EqualTo(new PersistedDelivery(
                    TheNameJiraHolds,
                    TheDayTheReleaseShipped,
                    DeliverySelectionMode.SourceBound,
                    JiraReleaseSourceKey,
                    TheReleaseJiraHolds,
                    false,
                    1)));
                deliveryRepositoryMock.Verify(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>()), Times.Never,
                    "the Feature ids the browser sent are not looked up at all - had they been, the ones in this request do not exist and the create would have failed instead of ignoring them.");
            }
        }

        /// <summary>
        /// The Manual and Rule-based rows are the regression proof: what those two payloads produce is
        /// unchanged by a Delivery gaining a third way of choosing its Features.
        /// </summary>
        [TestCaseSource(nameof(EveryWayOfChoosingWhatADeliveryContains))]
        public async Task What_a_created_Delivery_holds_is_settled_by_how_its_Features_were_chosen(
            UpdateDeliveryRequest request, PersistedDelivery expected)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            GivenTheReleaseResolvesTo(AResolvedRelease(), AFeature(1));

            var result = await CreateSubject().CreateDelivery(1, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(WhatWasPersisted(), Is.EqualTo(expected));
            }
        }

        private static IEnumerable<TestCaseData> EveryWayOfChoosingWhatADeliveryContains()
        {
            yield return new TestCaseData(
                new UpdateDeliveryRequest
                {
                    Name = "Chosen By Hand",
                    Date = TestToday.AFutureDate,
                    FeatureIds = [],
                    SelectionMode = DeliverySelectionMode.Manual,
                },
                new PersistedDelivery("Chosen By Hand", TestToday.AFutureDate, DeliverySelectionMode.Manual, null, null, false, 0));

            yield return new TestCaseData(
                new UpdateDeliveryRequest
                {
                    Name = "Chosen By Rule",
                    Date = TestToday.AFutureDate,
                    FeatureIds = [],
                    SelectionMode = DeliverySelectionMode.RuleBased,
                    Rules = [new WorkItemRuleCondition { FieldKey = "state", Operator = "equals", Value = "Doing" }],
                },
                new PersistedDelivery("Chosen By Rule", TestToday.AFutureDate, DeliverySelectionMode.RuleBased, null, null, true, 0));

            yield return new TestCaseData(
                ADeliveryFollowingTheRelease(),
                new PersistedDelivery(
                    TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.SourceBound,
                    JiraReleaseSourceKey, TheReleaseJiraHolds, false, 1));
        }

        /// <summary>
        /// A remote that could not be asked and a Release that is gone are answered differently on
        /// purpose: reporting a network blip as a deleted Release sends somebody off to re-create a
        /// Delivery whose Release never moved.
        /// </summary>
        [TestCaseSource(nameof(EveryVerdictAReleaseCanComeBackWith))]
        public async Task A_Release_that_does_not_resolve_leaves_no_Delivery_behind_and_says_which_kind_of_no_it_was(
            DeliverySourceResolution resolution, int expectedStatusCode, bool expectedToPersist)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            GivenTheReleaseResolvesTo(resolution, AFeature(1));

            var result = await CreateSubject().CreateDelivery(1, ADeliveryFollowingTheRelease());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusCodeOf(result), Is.EqualTo(expectedStatusCode));
                Assert.That(persistedDelivery != null, Is.EqualTo(expectedToPersist));
            }
        }

        private static IEnumerable<TestCaseData> EveryVerdictAReleaseCanComeBackWith()
        {
            yield return new TestCaseData(AResolvedRelease(), StatusCodes.Status200OK, true);
            yield return new TestCaseData(new DeliverySourceResolution.NotFound(), StatusCodes.Status404NotFound, false);
            yield return new TestCaseData(new DeliverySourceResolution.NoDate(TheNameJiraHolds), StatusCodes.Status400BadRequest, false);
            yield return new TestCaseData(
                new DeliverySourceResolution.Unavailable(DeliverySourceUnavailableReason.CapabilityWithdrawn),
                StatusCodes.Status503ServiceUnavailable,
                false);
        }

        /// <summary>
        /// The free-tier cap counts Deliveries, so a mode that merely fell through to it would let the
        /// first bound Delivery in a Portfolio through and refuse only the second.
        /// </summary>
        [TestCase(true, 0, StatusCodes.Status200OK)]
        [TestCase(true, 1, StatusCodes.Status200OK)]
        [TestCase(false, 0, StatusCodes.Status403Forbidden)]
        [TestCase(false, 1, StatusCodes.Status403Forbidden)]
        public async Task Following_a_Release_is_premium_from_the_first_Delivery_in_a_Portfolio_onwards(
            bool hasPremiumLicense, int deliveriesAlreadyInThePortfolio, int expectedStatusCode)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasPremiumLicense);
            deliveryRepositoryMock.Setup(x => x.GetByPortfolioAsync(1))
                .Returns(Enumerable.Range(0, deliveriesAlreadyInThePortfolio).Select(_ => GetTestDelivery()).ToList());
            GivenTheReleaseResolvesTo(AResolvedRelease(), AFeature(1));

            var result = await CreateSubject().CreateDelivery(1, ADeliveryFollowingTheRelease());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusCodeOf(result), Is.EqualTo(expectedStatusCode));
                Assert.That(persistedDelivery != null, Is.EqualTo(hasPremiumLicense));
            }
        }

        [TestCase(null, TheReleaseJiraHolds)]
        [TestCase("", TheReleaseJiraHolds)]
        [TestCase(JiraReleaseSourceKey, null)]
        [TestCase(JiraReleaseSourceKey, "")]
        public async Task A_Delivery_that_names_no_Release_to_follow_is_refused_rather_than_bound_to_nothing(
            string? sourceKey, string? sourceReference)
        {
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            GivenTheReleaseResolvesTo(AResolvedRelease(), AFeature(1));

            var request = ADeliveryFollowingTheRelease();
            request.SourceKey = sourceKey;
            request.SourceReference = sourceReference;

            var result = await CreateSubject().CreateDelivery(1, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
                Assert.That(persistedDelivery, Is.Null);
            }
        }

        private static UpdateDeliveryRequest ADeliveryFollowingTheRelease()
        {
            return new UpdateDeliveryRequest
            {
                Name = TheNameTheBrowserSent,
                Date = TheDayTheBrowserSent,
                FeatureIds = FeatureIdsThatWouldNotResolve,
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = TheReleaseJiraHolds,
            };
        }

        private static DeliverySourceResolution.Resolved AResolvedRelease()
        {
            return new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheNameJiraHolds, TheDayTheReleaseShipped, TheWorkTaggedAgainstTheRelease));
        }

        private static Feature AFeature(int id)
        {
            return new Feature { Id = id, Name = $"Feature {id}", ReferenceId = $"LGH-{id}", Order = "1000", Type = "Epic", State = "New" };
        }

        private void GivenTheReleaseResolvesTo(DeliverySourceResolution resolution, params Feature[] trackedFeatures)
        {
            GivenTheReleaseResolvesTo(TheReleaseJiraHolds, resolution, trackedFeatures);
        }

        /// <summary>
        /// The answers accumulate rather than replace one another, so a test that needs two Releases -
        /// the one a Delivery follows and the one it is pointed at instead - can arrange both.
        /// </summary>
        private void GivenTheReleaseResolvesTo(
            string sourceReference, DeliverySourceResolution resolution, params Feature[] trackedFeatures)
        {
            releasesTheResolverAnswersFor[sourceReference] = new PortfolioSourcePreview(
                resolution, trackedFeatures, trackedFeatures.Length);

            deliverySourceResolverMock
                .Setup(x => x.ResolveForPortfolio(It.IsAny<Portfolio>(), JiraReleaseSourceKey, It.IsAny<IReadOnlyList<string>>()))
                .ReturnsAsync(releasesTheResolverAnswersFor);
        }

        private PersistedDelivery? WhatWasPersisted()
        {
            return persistedDelivery == null ? null : StateOf(persistedDelivery);
        }

        private const int TheDeliveryBeingEdited = 77;
        private const int ThePortfolioItLivesIn = 3;
        private const string TheOtherReleaseJiraHolds = "10999";
        private const string TheNameTheOtherReleaseHolds = "Winter Release";
        private const string TheNameSomebodyTypedIn = "Renamed By Hand";
        private const string TheNameOnFile = "What It Was Called";
        private const string ARuleThatMatchesNothing = "{\"Version\":1,\"Conditions\":[]}";

        private static readonly DateTime TheDayTheOtherReleaseShips = new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime TheDayOnFile = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Keeping the name, the date and the Features is why somebody stops following a Release
        /// instead of deleting the Delivery. Nothing in the payload is read on the way out either: a
        /// Release that shipped last quarter leaves a past date on screen, and reading that back would
        /// be refused as a past date on any other update.
        /// </summary>
        [Test]
        public async Task Releasing_a_Delivery_from_its_Release_keeps_what_the_Release_last_gave_it_and_hands_it_back_editable()
        {
            var boundToAReleaseThatShippedLastQuarter = ADeliveryFollowingTheReleaseInJira();
            GivenTheDeliveryBeingEditedIs(boundToAReleaseThatShippedLastQuarter);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var stopFollowingIt = AnUpdateChoosingByHand(TheDayTheBrowserSent);

            var result = await CreateSubject().UpdateDelivery(TheDeliveryBeingEdited, stopFollowingIt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(StateOf(boundToAReleaseThatShippedLastQuarter), Is.EqualTo(new PersistedDelivery(
                    TheNameJiraHolds,
                    TheDayTheReleaseShipped,
                    DeliverySelectionMode.Manual,
                    null,
                    null,
                    false,
                    2)));
                deliveryRepositoryMock.Verify(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>()), Times.Never,
                    "the Feature ids the browser sent are not read on the way out - what the Release last put there is what stays.");
                deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
            }

            deliveryRepositoryMock
                .Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>()))
                .Returns(new List<Feature> { AFeature(1) });

            var renameItNow = AnUpdateChoosingByHand(TestToday.AFutureDate);
            var secondResult = await CreateSubject().UpdateDelivery(TheDeliveryBeingEdited, renameItNow);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(secondResult, Is.TypeOf<OkResult>());
                Assert.That(StateOf(boundToAReleaseThatShippedLastQuarter), Is.EqualTo(new PersistedDelivery(
                    TheNameSomebodyTypedIn,
                    TestToday.AFutureDate,
                    DeliverySelectionMode.Manual,
                    null,
                    null,
                    false,
                    1)));
            }
        }

        /// <summary>
        /// Every way a Delivery can be told to choose its Features, crossed with every way it was
        /// choosing them already. The four rows that start by hand or by rule are the regression proof.
        /// The rows that start bound are the ones a Delivery could not leave at all while the name was
        /// written before the mode was read.
        /// </summary>
        [TestCaseSource(nameof(EveryModeADeliveryCanBeMovedBetween))]
        public async Task What_a_Delivery_holds_after_an_update_is_settled_by_the_mode_it_moves_into(
            Delivery before, UpdateDeliveryRequest request, PersistedDelivery expected)
        {
            GivenTheDeliveryBeingEditedIs(before);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            deliveryRepositoryMock
                .Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>()))
                .Returns(new List<Feature> { AFeature(1) });
            GivenTheReleaseResolvesTo(TheReleaseJiraHolds, AResolvedRelease(), AFeature(1));
            GivenTheReleaseResolvesTo(TheOtherReleaseJiraHolds, TheOtherReleaseAsItResolves(), AFeature(2));

            var result = await CreateSubject().UpdateDelivery(TheDeliveryBeingEdited, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(StateOf(before), Is.EqualTo(expected));
            }
        }

        private static IEnumerable<TestCaseData> EveryModeADeliveryCanBeMovedBetween()
        {
            yield return new TestCaseData(
                ADeliveryChosenByHand(),
                AnUpdateChoosingByHand(TestToday.AFutureDate),
                new PersistedDelivery(TheNameSomebodyTypedIn, TestToday.AFutureDate, DeliverySelectionMode.Manual, null, null, false, 1))
                .SetName("By hand stays by hand");

            yield return new TestCaseData(
                ADeliveryChosenByHand(),
                AnUpdateChoosingByRule(),
                new PersistedDelivery(TheNameSomebodyTypedIn, TestToday.AFutureDate, DeliverySelectionMode.RuleBased, null, null, true, 0))
                .SetName("By hand becomes by rule");

            yield return new TestCaseData(
                ADeliveryChosenByHand(),
                AnUpdateFollowingTheRelease(TheReleaseJiraHolds),
                new PersistedDelivery(
                    TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.SourceBound,
                    JiraReleaseSourceKey, TheReleaseJiraHolds, false, 1))
                .SetName("By hand starts following a Release");

            yield return new TestCaseData(
                ADeliveryChosenByRule(),
                AnUpdateChoosingByHand(TestToday.AFutureDate),
                new PersistedDelivery(TheNameSomebodyTypedIn, TestToday.AFutureDate, DeliverySelectionMode.Manual, null, null, false, 1))
                .SetName("By rule becomes by hand");

            yield return new TestCaseData(
                ADeliveryChosenByRule(),
                AnUpdateChoosingByRule(),
                new PersistedDelivery(TheNameSomebodyTypedIn, TestToday.AFutureDate, DeliverySelectionMode.RuleBased, null, null, true, 0))
                .SetName("By rule stays by rule");

            yield return new TestCaseData(
                ADeliveryChosenByRule(),
                AnUpdateFollowingTheRelease(TheReleaseJiraHolds),
                new PersistedDelivery(
                    TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.SourceBound,
                    JiraReleaseSourceKey, TheReleaseJiraHolds, false, 1))
                .SetName("By rule starts following a Release and stops holding the rule");

            yield return new TestCaseData(
                ADeliveryFollowingTheReleaseInJira(),
                AnUpdateChoosingByHand(TheDayTheBrowserSent),
                new PersistedDelivery(TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.Manual, null, null, false, 2))
                .SetName("Following a Release that already shipped, released by hand and dated in the past");

            yield return new TestCaseData(
                ADeliveryFollowingTheReleaseInJira(),
                AnUpdateChoosingByHand(TestToday.AFutureDate),
                new PersistedDelivery(TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.Manual, null, null, false, 2))
                .SetName("Following a Release, released by hand and dated ahead");

            yield return new TestCaseData(
                ADeliveryFollowingTheReleaseInJira(),
                AnUpdateChoosingByRule(),
                new PersistedDelivery(TheNameJiraHolds, TheDayTheReleaseShipped, DeliverySelectionMode.RuleBased, null, null, true, 0))
                .SetName("Following a Release, released and left choosing by rule");

            yield return new TestCaseData(
                ADeliveryFollowingTheReleaseInJira(),
                AnUpdateFollowingTheRelease(TheOtherReleaseJiraHolds),
                new PersistedDelivery(
                    TheNameTheOtherReleaseHolds, TheDayTheOtherReleaseShips, DeliverySelectionMode.SourceBound,
                    JiraReleaseSourceKey, TheOtherReleaseJiraHolds, false, 1))
                .SetName("Following a Release, pointed at a different one");
        }

        /// <summary>
        /// The free-tier cap counts Deliveries and an update creates none, so nothing on this path had
        /// been asking about the licence at all.
        /// </summary>
        [TestCase(true, StatusCodes.Status200OK, DeliverySelectionMode.SourceBound)]
        [TestCase(false, StatusCodes.Status403Forbidden, DeliverySelectionMode.Manual)]
        public async Task Pointing_an_existing_Delivery_at_a_Release_is_premium_just_as_creating_one_that_follows_it_is(
            bool hasPremiumLicense, int expectedStatusCode, DeliverySelectionMode expectedMode)
        {
            var chosenByHand = ADeliveryChosenByHand();
            GivenTheDeliveryBeingEditedIs(chosenByHand);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(hasPremiumLicense);
            GivenTheReleaseResolvesTo(TheReleaseJiraHolds, AResolvedRelease(), AFeature(1));

            var result = await CreateSubject().UpdateDelivery(
                TheDeliveryBeingEdited, AnUpdateFollowingTheRelease(TheReleaseJiraHolds));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StatusCodeOf(result), Is.EqualTo(expectedStatusCode));
                Assert.That(chosenByHand.SelectionMode, Is.EqualTo(expectedMode));
            }
        }

        /// <summary>
        /// A payload that says it follows a Release but names none is somebody's mistake, not somebody
        /// asking to stop following one - and reading it as the latter would quietly hand back a
        /// Delivery that was meant to keep syncing.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        public async Task An_update_that_says_it_follows_a_Release_but_names_none_is_refused_rather_than_read_as_letting_go(
            string? sourceReference)
        {
            var bound = ADeliveryFollowingTheReleaseInJira();
            GivenTheDeliveryBeingEditedIs(bound);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            var request = AnUpdateFollowingTheRelease(TheReleaseJiraHolds);
            request.SourceReference = sourceReference;

            var result = await CreateSubject().UpdateDelivery(TheDeliveryBeingEdited, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
                Assert.That(bound.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound));
                Assert.That(bound.SourceReference, Is.EqualTo(TheReleaseJiraHolds));
                deliveryRepositoryMock.Verify(x => x.Save(), Times.Never);
            }
        }

        /// <summary>
        /// The version an open browser is holding is pinned against the row exactly once per accepted
        /// update, whichever mode the update leaves the Delivery in. Pinning it twice would discard the
        /// version the browser actually had and turn a conflict into a silent last-one-wins.
        /// </summary>
        [TestCaseSource(nameof(EveryModeAnUpdateCanLeaveADeliveryIn))]
        public async Task An_accepted_update_pins_the_version_the_browser_was_holding_exactly_once(
            Delivery before, UpdateDeliveryRequest request)
        {
            GivenTheDeliveryBeingEditedIs(before);
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);
            deliveryRepositoryMock
                .Setup(x => x.GetFeaturesByIds(It.IsAny<IEnumerable<int>>()))
                .Returns(new List<Feature> { AFeature(1) });
            GivenTheReleaseResolvesTo(TheReleaseJiraHolds, AResolvedRelease(), AFeature(1));

            var tokenTheBrowserWasHolding = Guid.NewGuid();
            request.ConcurrencyToken = tokenTheBrowserWasHolding;

            var result = await CreateSubject().UpdateDelivery(TheDeliveryBeingEdited, request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                deliveryRepositoryMock.Verify(
                    x => x.ApplyConcurrencyTokenForEdit(before, tokenTheBrowserWasHolding), Times.Once);
            }
        }

        private static IEnumerable<TestCaseData> EveryModeAnUpdateCanLeaveADeliveryIn()
        {
            yield return new TestCaseData(ADeliveryChosenByHand(), AnUpdateChoosingByHand(TestToday.AFutureDate))
                .SetName("Pinned once when the Features are chosen by hand");
            yield return new TestCaseData(ADeliveryChosenByHand(), AnUpdateChoosingByRule())
                .SetName("Pinned once when the Features are chosen by rule");
            yield return new TestCaseData(ADeliveryChosenByHand(), AnUpdateFollowingTheRelease(TheReleaseJiraHolds))
                .SetName("Pinned once when the Delivery starts following a Release");
            yield return new TestCaseData(ADeliveryFollowingTheReleaseInJira(), AnUpdateChoosingByHand(TheDayTheBrowserSent))
                .SetName("Pinned once when the Delivery is released from its Release");
        }

        private void GivenTheDeliveryBeingEditedIs(Delivery delivery)
        {
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(TheDeliveryBeingEdited)).Returns(delivery);
        }

        private static Delivery ADeliveryChosenByHand()
        {
            var delivery = new Delivery(TheNameOnFile, TheDayOnFile, ThePortfolioItLivesIn)
            {
                Id = TheDeliveryBeingEdited,
            };
            delivery.ReplaceFeatures([AFeature(4)]);
            return delivery;
        }

        private static Delivery ADeliveryChosenByRule()
        {
            var delivery = new Delivery(TheNameOnFile, TheDayOnFile, ThePortfolioItLivesIn)
            {
                Id = TheDeliveryBeingEdited,
            };
            delivery.SelectFeaturesByRule(ARuleThatMatchesNothing, WorkItemRuleSet.SchemaVersion);
            return delivery;
        }

        private static Delivery ADeliveryFollowingTheReleaseInJira()
        {
            var delivery = new Delivery(TheNameJiraHolds, TheDayTheReleaseShipped, ThePortfolioItLivesIn)
            {
                Id = TheDeliveryBeingEdited,
            };
            delivery.ReplaceFeatures([AFeature(1), AFeature(2)]);
            delivery.BindToSource(JiraReleaseSourceKey, TheReleaseJiraHolds);
            return delivery;
        }

        private static UpdateDeliveryRequest AnUpdateChoosingByHand(DateTime date)
        {
            return new UpdateDeliveryRequest
            {
                Name = TheNameSomebodyTypedIn,
                Date = date,
                FeatureIds = [1],
                SelectionMode = DeliverySelectionMode.Manual,
            };
        }

        private static UpdateDeliveryRequest AnUpdateChoosingByRule()
        {
            return new UpdateDeliveryRequest
            {
                Name = TheNameSomebodyTypedIn,
                Date = TestToday.AFutureDate,
                FeatureIds = [1],
                SelectionMode = DeliverySelectionMode.RuleBased,
                Rules = [new WorkItemRuleCondition { FieldKey = "state", Operator = "equals", Value = "Doing" }],
            };
        }

        private static UpdateDeliveryRequest AnUpdateFollowingTheRelease(string sourceReference)
        {
            return new UpdateDeliveryRequest
            {
                Name = TheNameTheBrowserSent,
                Date = TheDayTheBrowserSent,
                FeatureIds = FeatureIdsThatWouldNotResolve,
                SelectionMode = DeliverySelectionMode.SourceBound,
                SourceKey = JiraReleaseSourceKey,
                SourceReference = sourceReference,
            };
        }

        private static DeliverySourceResolution.Resolved TheOtherReleaseAsItResolves()
        {
            return new DeliverySourceResolution.Resolved(
                new DeliverySourceSnapshot(TheNameTheOtherReleaseHolds, TheDayTheOtherReleaseShips, TheWorkTaggedAgainstTheRelease));
        }

        private static PersistedDelivery StateOf(Delivery delivery)
        {
            return new PersistedDelivery(
                delivery.Name,
                delivery.Date,
                delivery.SelectionMode,
                delivery.SourceKey,
                delivery.SourceReference,
                delivery.RuleDefinitionJson != null,
                delivery.Features.Count);
        }

        private static int StatusCodeOf(IActionResult result)
        {
            return result switch
            {
                ObjectResult objectResult => objectResult.StatusCode ?? 0,
                StatusCodeResult statusCodeResult => statusCodeResult.StatusCode,
                _ => 0,
            };
        }
    }
}