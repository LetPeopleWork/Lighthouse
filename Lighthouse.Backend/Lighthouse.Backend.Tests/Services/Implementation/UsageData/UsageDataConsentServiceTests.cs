using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// The decisions the endpoints cannot show. Those answer over HTTP at a single instant, so which
    /// direction a window points and which tier makes a refusal final are invisible to them - flip
    /// either and every endpoint test still passes.
    /// </summary>
    public class UsageDataConsentServiceTests
    {
        private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

        private const int WindowDays = 30;

        private Mock<IUsageDataConsentRepository> repositoryMock;
        private Mock<IAppSettingService> appSettingServiceMock;
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IUsageDataConsentRepository>();
            appSettingServiceMock = new Mock<IAppSettingService>();
            licenseServiceMock = new Mock<ILicenseService>();

            appSettingServiceMock.Setup(s => s.EnsureUsageDataInstanceId()).ReturnsAsync("an-instance");
        }

        [Test]
        public async Task GetState_AsksWhetherAnyoneConsentedWithinTheWindow_LookingBackwardsNotForwards()
        {
            DateTime? asked = null;
            repositoryMock
                .Setup(r => r.AnyLiveGrantAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback<DateTime, CancellationToken>((threshold, _) => asked = threshold)
                .ReturnsAsync(false);

            await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(asked, Is.EqualTo(Now.AddDays(-WindowDays)),
                "a threshold in the future would count every grant that ever existed as live, and the "
                + "instance would keep sending long after everybody stopped visiting");
        }

        [Test]
        public async Task GetState_WithNoToken_DoesNotTouchAnything()
        {
            await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(
                r => r.TouchAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
                Times.Never,
                "there is no browser here whose consent could be kept alive");
        }

        [Test]
        public async Task GetState_WithATokenThisInstanceNeverMinted_AnswersTheSameAsNoTokenAtAll()
        {
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsageDataConsent?)null);
            var service = CreateService();

            var withoutToken = await service.GetStateAsync(null, TestContext.CurrentContext.CancellationToken);
            var withUnknownToken = await service.GetStateAsync("never-minted-here", TestContext.CurrentContext.CancellationToken);

            Assert.That(withUnknownToken, Is.EqualTo(withoutToken),
                "answering differently would let anyone hold up a token and be told whether this "
                + "instance has ever seen it");
        }

        [Test]
        public async Task GetState_ForABrowserItKnows_KeepsThatBrowsersConsentAliveFromBehind()
        {
            DateTime? staleBefore = null;
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataConsent { TokenHash = "digest", Decision = UsageDataDecision.Granted });
            repositoryMock
                .Setup(r => r.TouchAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback<string, DateTime, DateTime, CancellationToken>((_, _, stale, _) => staleBefore = stale)
                .ReturnsAsync(1);

            await CreateService().GetStateAsync("a-token", TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(staleBefore, Is.LessThan(Now),
                    "the throttle only means anything if it asks whether the stamp is OLDER than a "
                    + "moment already past; a threshold in the future makes every read a write");
                Assert.That(staleBefore, Is.GreaterThan(Now.AddDays(-WindowDays)),
                    "and it has to be a small slice of the window rather than a multiple of it - a "
                    + "cutoff further back than the window itself never refreshes anything, so every "
                    + "browser silently ages out while it is still here");
            }
        }

        [Test]
        public async Task RecordDecision_WhenSomebodyAgrees_NamesTheInstanceFirst()
        {
            await CreateService().RecordDecisionAsync(UsageDataDecision.Granted, TestContext.CurrentContext.CancellationToken);

            appSettingServiceMock.Verify(s => s.EnsureUsageDataInstanceId(), Times.Once);
        }

        [Test]
        public async Task RecordDecision_WhenSomebodyRefuses_LeavesTheInstanceUnnamed()
        {
            await CreateService().RecordDecisionAsync(UsageDataDecision.Declined, TestContext.CurrentContext.CancellationToken);

            appSettingServiceMock.Verify(s => s.EnsureUsageDataInstanceId(), Times.Never,
                "an instance nobody has agreed on has no identifier at all, which is what makes "
                + "\"we hold nothing about instances that did not opt in\" true of the database");
        }

        [Test]
        public async Task RecordDecision_WhenTheInstanceCannotBeNamed_StillRecordsTheAnswer()
        {
            appSettingServiceMock
                .Setup(s => s.EnsureUsageDataInstanceId())
                .ThrowsAsync(new InvalidOperationException("the identifier could not be written"));

            await CreateService().RecordDecisionAsync(UsageDataDecision.Granted, TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(
                r => r.AddAsync(It.IsAny<UsageDataConsent>(), It.IsAny<CancellationToken>()),
                Times.Once,
                "the answer is the thing the person gave us; refusing it over a write they know "
                + "nothing about is the worse failure");
        }

        [Test]
        public async Task RecordDecision_MintsADifferentTokenEveryTime()
        {
            var service = CreateService();

            var first = await service.RecordDecisionAsync(UsageDataDecision.Granted, TestContext.CurrentContext.CancellationToken);
            var second = await service.RecordDecisionAsync(UsageDataDecision.Granted, TestContext.CurrentContext.CancellationToken);

            Assert.That(second, Is.Not.EqualTo(first));
        }

        [TestCase(UsageDataDecision.Granted, true, false, Description = "already agreed, Premium")]
        [TestCase(UsageDataDecision.Granted, false, false, Description = "already agreed, Community")]
        [TestCase(UsageDataDecision.Declined, true, false, Description = "a Premium refusal is final")]
        [TestCase(UsageDataDecision.Declined, false, true, Description = "a Community refusal comes back")]
        [TestCase(UsageDataDecision.Revoked, true, true, Description = "changing your mind must not lock you out")]
        [TestCase(UsageDataDecision.Revoked, false, true, Description = "same on Community")]
        public async Task GetState_SaysWhetherThisBrowserWillBeAskedAgain(
            UsageDataDecision decision, bool premium, bool expected)
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(premium);
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataConsent { TokenHash = "digest", Decision = decision });

            var state = await CreateService().GetStateAsync("a-token", TestContext.CurrentContext.CancellationToken);

            Assert.That(state.WillAskAgain, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetState_ForABrowserThatHasNotDecided_WillAskAgainWhateverTheTier()
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(true);

            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.WillAskAgain, Is.True, "nobody has answered yet, so there is still something to ask");
        }

        private UsageDataConsentService CreateService()
        {
            var configuration = new UsageDataConfiguration { ConsentLivenessWindowDays = WindowDays };
            var monitor = Mock.Of<IOptionsMonitor<UsageDataConfiguration>>(m => m.CurrentValue == configuration);

            return new UsageDataConsentService(
                repositoryMock.Object,
                appSettingServiceMock.Object,
                licenseServiceMock.Object,
                monitor,
                new FakeTimeProvider(Now),
                Mock.Of<ILogger<UsageDataConsentService>>());
        }

        private sealed class FakeTimeProvider(DateTime now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
        }
    }
}
