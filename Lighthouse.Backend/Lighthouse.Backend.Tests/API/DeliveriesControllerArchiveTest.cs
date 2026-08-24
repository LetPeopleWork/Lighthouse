using System.Security.Claims;
using Lighthouse.Backend.API;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Authorization;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Authorization;
using Lighthouse.Backend.Services.Interfaces.DeliverySources;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Lighthouse.Backend.Tests.API
{
    public class DeliveriesControllerArchiveTest
    {
        private const int DeliveryId = 42;
        private const int PortfolioId = 7;

        private Mock<IDeliveryRepository> deliveryRepositoryMock;
        private Mock<IRepository<Portfolio>> portfolioRepositoryMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IDeliveryRuleService> deliveryRuleServiceMock;
        private Mock<IRbacAdministrationService> rbacAdministrationServiceMock;
        private Mock<IDeliveryMetricSnapshotRepository> deliveryMetricSnapshotRepositoryMock;
        private Mock<IBlackoutPeriodService> blackoutPeriodServiceMock;

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

            blackoutPeriodServiceMock
                .Setup(x => x.GetEffectiveBlackoutDays(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(new List<BlackoutPeriod>());

            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(true);

            deliveryRepositoryMock.Setup(x => x.GetPortfolioId(DeliveryId)).Returns(PortfolioId);

            rbacAdministrationServiceMock
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<RbacGuardRequirement>(),
                    It.IsAny<int?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        [Test]
        public async Task ArchiveDelivery_WithWriteAccessAndLicense_MarksTheDeliveryArchivedAndSaves()
        {
            var delivery = GivenDelivery();
            var closureRecord = GivenClosureRecord();

            var result = await CreateSubject().ArchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(delivery.ArchivedOn, Is.EqualTo(TestToday.AmbientAsUtcMidnight));
                Assert.That(closureRecord.ArchivedOn, Is.EqualTo(TestToday.AmbientAsUtcMidnight));
            }

            deliveryRepositoryMock.Verify(x => x.GetOrCreateClosureRecord(DeliveryId), Times.Once);
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task ArchiveDelivery_PinsWhatTheProjectorReadsForToday()
        {
            var delivery = GivenDelivery();
            var closureRecord = GivenClosureRecord();

            var expected = new DeliveryMetricValuesProjector(blackoutPeriodServiceMock.Object)
                .Project(delivery, TestToday.Ambient);

            await CreateSubject().ArchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(closureRecord.TargetDateAtClosure, Is.EqualTo(expected.TargetDate));
                Assert.That(closureRecord.TotalWork, Is.EqualTo(expected.TotalWork));
                Assert.That(closureRecord.DoneWork, Is.EqualTo(expected.DoneWork));
                Assert.That(closureRecord.RemainingWork, Is.EqualTo(expected.RemainingWork));
                Assert.That(closureRecord.EstimatedItemCount, Is.EqualTo(expected.EstimatedItemCount));
                Assert.That(closureRecord.LikelihoodPercentage, Is.EqualTo(expected.LikelihoodPercentage));
                Assert.That(closureRecord.WhenDistributionJson, Is.EqualTo(expected.WhenDistributionJson));
                Assert.That(closureRecord.FeatureBreakdownJson, Is.EqualTo(expected.FeatureBreakdownJson));
                Assert.That(closureRecord.HasSufficientData, Is.EqualTo(expected.HasSufficientData));
                Assert.That(closureRecord.TeamsWithoutForecastJson, Is.EqualTo(expected.TeamsWithoutForecastJson));
                Assert.That(closureRecord.SelectionMode, Is.EqualTo(expected.SelectionMode));
                Assert.That(closureRecord.RuleDefinitionJson, Is.EqualTo(expected.RuleDefinitionJson));
                Assert.That(closureRecord.RuleSchemaVersion, Is.EqualTo(expected.RuleSchemaVersion));
            }
        }

        [Test]
        public async Task ArchiveDelivery_WithoutALicense_IsRefusedAndNothingIsWritten()
        {
            var delivery = GivenDelivery();
            GivenClosureRecord();
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var result = await CreateSubject().ArchiveDelivery(DeliveryId, null);
            var refusal = result as ObjectResult;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal, Is.Not.Null);
                Assert.That(refusal?.StatusCode, Is.EqualTo(403));
                Assert.That(delivery.ArchivedOn, Is.Null);
            }

            deliveryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task ArchiveDelivery_WithoutPortfolioWrite_ReturnsForbidden()
        {
            var delivery = GivenDelivery();
            GivenClosureRecord();
            GivenNoWriteAccess();

            var result = await CreateSubject().ArchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ForbidResult>());
                Assert.That(delivery.ArchivedOn, Is.Null);
            }

            deliveryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task ArchiveDelivery_ForAnUnknownDelivery_ReturnsNotFound()
        {
            deliveryRepositoryMock.Setup(x => x.GetPortfolioId(DeliveryId)).Returns((int?)null);

            var result = await CreateSubject().ArchiveDelivery(DeliveryId, null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void ArchiveDelivery_WhenAlreadyArchived_IsRefusedByTheAggregate()
        {
            var delivery = GivenDelivery();
            delivery.Archive(TestToday.AmbientAsUtcMidnight);
            GivenClosureRecord();

            var controller = CreateSubject();

            Assert.That(async () => await controller.ArchiveDelivery(DeliveryId, null),
                Throws.InstanceOf<DeliveryArchivedException>());
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task ArchiveDelivery_WithAClientToken_PutsTheTokenOnTheEdit()
        {
            var delivery = GivenDelivery();
            GivenClosureRecord();
            var clientToken = Guid.NewGuid();

            await CreateSubject().ArchiveDelivery(DeliveryId, new ArchiveDeliveryRequest { ConcurrencyToken = clientToken });

            deliveryRepositoryMock.Verify(x => x.ApplyConcurrencyTokenForEdit(delivery, clientToken), Times.Once);
        }

        [Test]
        public async Task UnarchiveDelivery_PutsTheDeliveryBackOnTheActiveList()
        {
            var delivery = GivenArchivedDelivery();

            var result = await CreateSubject().UnarchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(delivery.ArchivedOn, Is.Null);
            }

            deliveryRepositoryMock.Verify(x => x.Save(), Times.Once);
        }

        [Test]
        public async Task UnarchiveDelivery_WithoutALicense_StillSucceeds()
        {
            var delivery = GivenArchivedDelivery();
            licenseServiceMock.Setup(x => x.CanUsePremiumFeatures()).Returns(false);

            var result = await CreateSubject().UnarchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.TypeOf<OkResult>());
                Assert.That(delivery.ArchivedOn, Is.Null);
            }
        }

        [Test]
        public async Task UnarchiveDelivery_LeavesThePinnedRecordWhereItIs()
        {
            GivenArchivedDelivery();

            await CreateSubject().UnarchiveDelivery(DeliveryId, null);

            deliveryRepositoryMock.Verify(x => x.GetOrCreateClosureRecord(It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void UnarchiveDelivery_WhenNotArchived_IsRefusedByTheAggregate()
        {
            GivenDelivery();

            var controller = CreateSubject();

            Assert.That(async () => await controller.UnarchiveDelivery(DeliveryId, null),
                Throws.InstanceOf<DeliveryArchivedException>());
            deliveryRepositoryMock.Verify(x => x.Save(), Times.Never);
        }

        [Test]
        public async Task UnarchiveDelivery_WithoutPortfolioWrite_ReturnsForbidden()
        {
            var delivery = GivenArchivedDelivery();
            GivenNoWriteAccess();

            var result = await CreateSubject().UnarchiveDelivery(DeliveryId, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Is.InstanceOf<ForbidResult>());
                Assert.That(delivery.ArchivedOn, Is.Not.Null);
            }
        }

        [Test]
        public async Task UnarchiveDelivery_ForAnUnknownDelivery_ReturnsNotFound()
        {
            deliveryRepositoryMock.Setup(x => x.GetPortfolioId(DeliveryId)).Returns((int?)null);

            var result = await CreateSubject().UnarchiveDelivery(DeliveryId, null);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UnarchiveDelivery_WithAClientToken_PutsTheTokenOnTheEdit()
        {
            var delivery = GivenArchivedDelivery();
            var clientToken = Guid.NewGuid();

            await CreateSubject().UnarchiveDelivery(DeliveryId, new ArchiveDeliveryRequest { ConcurrencyToken = clientToken });

            deliveryRepositoryMock.Verify(x => x.ApplyConcurrencyTokenForEdit(delivery, clientToken), Times.Once);
        }

        private void GivenNoWriteAccess()
        {
            rbacAdministrationServiceMock
                .Setup(x => x.CanSatisfyRequirementAsync(
                    It.IsAny<ClaimsPrincipal>(),
                    RbacGuardRequirement.PortfolioWrite,
                    PortfolioId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
        }

        private Delivery GivenDelivery()
        {
            var delivery = DeliveryWithForecastableWork();
            deliveryRepositoryMock.Setup(x => x.GetById(DeliveryId)).Returns(delivery);
            deliveryRepositoryMock.Setup(x => x.GetByIdForUpdate(DeliveryId)).Returns(delivery);
            return delivery;
        }

        private Delivery GivenArchivedDelivery()
        {
            var delivery = GivenDelivery();
            delivery.Archive(TestToday.AmbientAsUtcMidnight);
            return delivery;
        }

        /// <summary>
        /// Archiving freezes what a Delivery is, and where it gets its name and date from is part of
        /// that. Left open, a refresh pass could start driving a Delivery nobody is running any more,
        /// or release one whose closure record was pinned against the Release it followed.
        /// </summary>
        [Test]
        public void An_archived_Delivery_can_neither_be_made_to_follow_a_Release_nor_released_from_one()
        {
            var archivedAndBound = DeliveryWithForecastableWork();
            archivedAndBound.BindToSource("jira-release", "10412");
            archivedAndBound.Archive(TestToday.AmbientAsUtcMidnight);

            var archivedAndUnbound = DeliveryWithForecastableWork();
            archivedAndUnbound.Archive(TestToday.AmbientAsUtcMidnight);

            using (Assert.EnterMultipleScope())
            {
                Assert.Throws<DeliveryArchivedException>(archivedAndBound.Unbind);
                Assert.Throws<DeliveryArchivedException>(() => archivedAndUnbound.BindToSource("jira-release", "10412"));
                Assert.That(archivedAndBound.SourceReference, Is.EqualTo("10412"),
                    "the refusal has to leave the frozen Delivery exactly as it was - a half-applied release is worse than none.");
                Assert.That(archivedAndUnbound.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
            }
        }

        private DeliveryClosureRecord GivenClosureRecord()
        {
            var closureRecord = new DeliveryClosureRecord { DeliveryId = DeliveryId };
            deliveryRepositoryMock.Setup(x => x.GetOrCreateClosureRecord(DeliveryId)).Returns(closureRecord);
            return closureRecord;
        }

        private static Delivery DeliveryWithForecastableWork()
        {
            var team = new Team { Id = 1, Name = "Team" };

            var forecast = new WhenForecast { TeamId = team.Id };
            forecast.GetType()
                .GetMethod("SetSimulationResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .Invoke(forecast, [new Dictionary<int, int> { { 10, 40 }, { 20, 60 } }]);

            var feature = new Feature { Id = 1, Name = "Feature", Order = "1000", Type = "Epic", State = "New" };
            feature.Forecasts.Add(forecast);
            feature.FeatureWork.Add(new FeatureWork { Team = team, TeamId = team.Id, TotalWorkItems = 10, RemainingWorkItems = 4 });

            var delivery = new Delivery("Q1 Release", TestToday.AFutureDate, PortfolioId, TestToday.Ambient) { Id = DeliveryId };
            delivery.ReplaceFeatures([feature]);
            return delivery;
        }

        private DeliveriesController CreateSubject()
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
                TestToday.Clock,
                Mock.Of<IDeliverySourceResolver>());
        }
    }
}
