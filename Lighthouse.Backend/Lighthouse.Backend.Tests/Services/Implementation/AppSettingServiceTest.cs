using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.Events;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.OptionalFeatures;
using Moq;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DomainEvents;
using Lighthouse.Backend.Services.Interfaces.Repositories;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    public class AppSettingServiceTests
    {
        private static readonly string[] SeedThenRecord = ["seed", "record"];

        private Mock<IRepository<AppSetting>> repositoryMock;
        private Mock<IRepository<OptionalFeature>> optionalFeatureRepositoryMock;
        private Mock<IFeatureOrderingPolicyProvider> policyProviderMock;
        private Mock<IFeatureRankSeeder> rankSeederMock;
        private Mock<IDomainEventDispatcher> domainEventDispatcherMock;
        private OptionalFeature orderingSetting;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IRepository<AppSetting>>();
            optionalFeatureRepositoryMock = new Mock<IRepository<OptionalFeature>>();
            policyProviderMock = new Mock<IFeatureOrderingPolicyProvider>();
            rankSeederMock = new Mock<IFeatureRankSeeder>();
            domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();

            orderingSetting = new OptionalFeature { Id = 0, Key = OptionalFeatureKeys.FeatureOrderingKey, Enabled = false, IsPremium = true };
            optionalFeatureRepositoryMock
                .Setup(x => x.GetByPredicate(It.Is<Func<OptionalFeature, bool>>(predicate => predicate(orderingSetting))))
                .Returns(orderingSetting);
        }

        // Epic 5375 - the order of these steps is the point. The places are read off the sequence the
        // administrator was looking at, so they have to be taken before the choice is recorded.
        [Test]
        public async Task SetFeatureOrderingPolicy_TakingTheOrderOver_SeedsBeforeItRecordsTheChoice()
        {
            var steps = new List<string>();
            rankSeederMock.Setup(s => s.SeedMissingRanks()).Callback(() => steps.Add("seed")).Returns(Task.CompletedTask);
            optionalFeatureRepositoryMock.Setup(x => x.Update(It.IsAny<OptionalFeature>())).Callback(() => steps.Add("record"));

            await CreateService().SetFeatureOrderingPolicy(FeatureOrderingPolicy.ManualOrder);

            Assert.That(steps, Is.EqualTo(SeedThenRecord));
        }

        [Test]
        public async Task SetFeatureOrderingPolicy_GivingTheOrderBack_PlacesNobody()
        {
            await CreateService().SetFeatureOrderingPolicy(FeatureOrderingPolicy.SourceOrder);

            using (Assert.EnterMultipleScope())
            {
                rankSeederMock.Verify(s => s.SeedMissingRanks(), Times.Never);
                optionalFeatureRepositoryMock.Verify(x => x.Update(orderingSetting), Times.Once);
                Assert.That(orderingSetting.Enabled, Is.False);
            }
        }

        // Without the announcement the places move and every forecast date stays where it was (ADR-133).
        [TestCase(FeatureOrderingPolicy.ManualOrder)]
        [TestCase(FeatureOrderingPolicy.SourceOrder)]
        public async Task SetFeatureOrderingPolicy_AnnouncesTheChange(FeatureOrderingPolicy policy)
        {
            await CreateService().SetFeatureOrderingPolicy(policy);

            domainEventDispatcherMock.Verify(
                d => d.PublishAsync(It.Is<FeatureOrderingPolicyChanged>(e => e.Policy == policy), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public void GetFeatureOrderingPolicy_AsksTheOnlyReaderOfTheSetting()
        {
            policyProviderMock.Setup(p => p.GetPolicy()).Returns(FeatureOrderingPolicy.ManualOrder);

            Assert.That(CreateService().GetFeatureOrderingPolicy(), Is.EqualTo(FeatureOrderingPolicy.ManualOrder));
        }

        [Test]
        public void GetFeatureRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.FeaturesRefreshInterval, "60", AppSettingKeys.FeaturesRefreshAfter, "360", AppSettingKeys.FeaturesRefreshStartDelay, "1");

            var service = CreateService();

            var settings = service.GetFeatureRefreshSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Interval, Is.EqualTo(60));
                Assert.That(settings.RefreshAfter, Is.EqualTo(360));
                Assert.That(settings.StartDelay, Is.EqualTo(1));
            }
        }

        [Test]
        public void GetTeamDataRefreshSettings_ReturnsCorrectSettings()
        {
            SetupRepositoryForKeys(AppSettingKeys.TeamDataRefreshInterval, "30", AppSettingKeys.TeamDataRefreshAfter, "180", AppSettingKeys.TeamDataRefreshStartDelay, "2");

            var service = CreateService();

            var settings = service.GetTeamDataRefreshSettings();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(settings.Interval, Is.EqualTo(30));
                Assert.That(settings.RefreshAfter, Is.EqualTo(180));
                Assert.That(settings.StartDelay, Is.EqualTo(2));
            }
        }

        [Test]
        public async Task UpdateFeatureRefreshSettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(AppSettingKeys.FeaturesRefreshInterval, "60", AppSettingKeys.FeaturesRefreshAfter, "360", AppSettingKeys.FeaturesRefreshStartDelay, "1");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 70, RefreshAfter = 370, StartDelay = 10 };
            await service.UpdateFeatureRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshInterval, "70");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshAfter, "370");
            VerifyUpdateCalled(AppSettingKeys.FeaturesRefreshStartDelay, "10");
        }

        [Test]
        public async Task UpdateTeamDataRefreshSettings_UpdatesCorrectlyAsync()
        {
            SetupRepositoryForKeys(AppSettingKeys.TeamDataRefreshInterval, "30", AppSettingKeys.TeamDataRefreshAfter, "180", AppSettingKeys.TeamDataRefreshStartDelay, "2");

            var service = CreateService();

            var newSettings = new RefreshSettings { Interval = 35, RefreshAfter = 190, StartDelay = 3 };
            await service.UpdateTeamDataRefreshSettings(newSettings);

            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshInterval, "35");
            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshAfter, "190");
            VerifyUpdateCalled(AppSettingKeys.TeamDataRefreshStartDelay, "3");
        }

        [Test]
        public void GetSettingByKey_KeyDoesNotExist_ThrowsException()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<AppSetting, bool>>())).Returns((AppSetting)null);

            var service = CreateService();

            Assert.Throws<ArgumentNullException>(() => service.GetFeatureRefreshSettings());
        }

        [Test]
        public void GetRefreshLogRetentionRuns_ReturnsParsedValue()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.Is<Func<AppSetting, bool>>(predicate => predicate(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns })))).Returns(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns, Value = "50" });

            var service = CreateService();

            var result = service.GetRefreshLogRetentionRuns();

            Assert.That(result, Is.EqualTo(50));
        }

        [Test]
        public void GetRefreshLogRetentionRuns_SettingMissing_ReturnsDefault30()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<AppSetting, bool>>())).Returns((AppSetting)null);

            var service = CreateService();

            var result = service.GetRefreshLogRetentionRuns();

            Assert.That(result, Is.EqualTo(30));
        }

        [Test]
        public void GetRefreshLogRetentionRuns_ValueBelowMin_ClampsTo10()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.Is<Func<AppSetting, bool>>(predicate => predicate(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns })))).Returns(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns, Value = "5" });

            var service = CreateService();

            var result = service.GetRefreshLogRetentionRuns();

            Assert.That(result, Is.EqualTo(10));
        }

        [Test]
        public void GetRefreshLogRetentionRuns_ValueAboveMax_ClampsTo200()
        {
            repositoryMock.Setup(x => x.GetByPredicate(It.Is<Func<AppSetting, bool>>(predicate => predicate(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns })))).Returns(new AppSetting { Key = AppSettingKeys.RefreshLogRetentionRuns, Value = "999" });

            var service = CreateService();

            var result = service.GetRefreshLogRetentionRuns();

            Assert.That(result, Is.EqualTo(200));
        }

        private AppSettingService CreateService()
        {
            var orderingApplier = new FeatureOrderingApplier(
                optionalFeatureRepositoryMock.Object,
                rankSeederMock.Object,
                domainEventDispatcherMock.Object);

            var applierRegistry = new OptionalFeatureApplierRegistry(
                [orderingApplier],
                new DefaultOptionalFeatureApplier(optionalFeatureRepositoryMock.Object));

            return new AppSettingService(
                repositoryMock.Object,
                optionalFeatureRepositoryMock.Object,
                policyProviderMock.Object,
                applierRegistry,
                TimeProvider.System);
        }

        private void SetupRepositoryForKeys(params string[] keyValuePairs)
        {
            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                var key = keyValuePairs[i];
                var value = keyValuePairs[i + 1];
                repositoryMock.Setup(x => x.GetByPredicate(It.Is<Func<AppSetting, bool>>(predicate => predicate(new AppSetting { Key = key })))).Returns(new AppSetting { Key = key, Value = value });
            }
        }

        private void VerifyUpdateCalled(string key, string value)
        {
            repositoryMock.Verify(x => x.Update(It.Is<AppSetting>(s => s.Key == key && s.Value == value)), Times.Once);
        }
    }
}