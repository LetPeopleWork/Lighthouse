using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.UsageData;
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

        // Long enough that every scenario not about install age is comfortably past it, so a test
        // that means to be about something else cannot accidentally be about the threshold.
        private const int AskAfterDays = 3;

        private const int ReAskAfterDays = 90;

        private Mock<IUsageDataConsentRepository> repositoryMock;
        private Mock<ILicenseService> licenseServiceMock;
        private Mock<IAppSettingService> appSettingsMock;
        private Mock<IUsageDataMasterSwitch> masterSwitchMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IUsageDataConsentRepository>();
            licenseServiceMock = new Mock<ILicenseService>();

            appSettingsMock = new Mock<IAppSettingService>();
            appSettingsMock.Setup(s => s.GetInstallTimestamp()).Returns(Now.AddDays(-365));

            masterSwitchMock = new Mock<IUsageDataMasterSwitch>();
            masterSwitchMock.Setup(s => s.IsAllowed()).Returns(true);
        }

        [Test]
        public async Task GetState_ForABrowserThatGranted_ReportsSending()
        {
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataConsent { TokenHash = "digest", Decision = UsageDataDecision.Granted });

            var state = await CreateService().GetStateAsync("a-token", TestContext.CurrentContext.CancellationToken);

            Assert.That(state.Sending, Is.True);
        }

        // The indicator this feeds reads "being sent from this browser". Answering it with whether
        // ANYONE on the instance still consents put that sentence in front of somebody who had just
        // declined, because a colleague had said yes - a sentence that was false about them twice.
        [TestCase(UsageDataDecision.Declined)]
        [TestCase(UsageDataDecision.Revoked)]
        public async Task GetState_ForABrowserThatSaidNo_ReportsNotSending_WhoeverElseSaidYes(
            UsageDataDecision decision)
        {
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataConsent { TokenHash = "digest", Decision = decision });

            var state = await CreateService().GetStateAsync("a-token", TestContext.CurrentContext.CancellationToken);

            Assert.That(state.Sending, Is.False);
        }

        [Test]
        public async Task GetState_WithNoToken_ReportsNotSending_WhoeverElseSaidYes()
        {
            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.Sending, Is.False);
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

            // Left at its default, which is long enough ago that every window has passed - so this
            // is only about which answers leave anything still to ask, and never about timing.
            TheBrowserAnswered(decision, decidedAt: default);

            var state = await StateForABrowserWithAToken();

            Assert.That(state.MayAsk, Is.EqualTo(expected));
        }

        /// <summary>
        /// The pseudonym is written in the same save as the answer, so there is no moment where a
        /// browser has agreed and has nothing to be counted under. Only for a yes: a browser that
        /// said no must have no pseudonym anywhere, because the row recording a refusal is the one
        /// place a refusal could accidentally become an identity.
        /// </summary>
        [TestCase(UsageDataDecision.Granted, true, Description = "agreeing needs something to be counted under")]
        [TestCase(UsageDataDecision.Declined, false, Description = "refusing must leave no identity behind")]
        public async Task RecordDecision_GivesSomethingToBeCountedUnderOnlyToABrowserThatAgreed(
            UsageDataDecision decision, bool expected)
        {
            UsageDataConsent? written = null;
            repositoryMock
                .Setup(r => r.AddAsync(It.IsAny<UsageDataConsent>(), It.IsAny<CancellationToken>()))
                .Callback((UsageDataConsent consent, CancellationToken _) => written = consent)
                .Returns(Task.CompletedTask);

            await CreateService().RecordDecisionAsync(decision, TestContext.CurrentContext.CancellationToken);

            Assert.That(written?.AnalyticsId is not null, Is.EqualTo(expected),
                "either a browser that agreed was recorded with nothing to be counted under - so "
                + "its first event has no identity to carry - or a browser that refused was given "
                + "one anyway, which is an identity minted for somebody who asked not to have one");
        }

        // Slice 02 (#5835). Everything below is the cadence arithmetic, and it lives here rather
        // than beside the endpoint tests for the reason this class exists at all: the endpoints
        // answer at one instant, so which direction a window points and which tier makes a refusal
        // final are invisible to them. Flip either and every endpoint test still passes.

        [Test]
        public async Task GetState_ForABrowserThatNeverAnswered_SaysToAsk()
        {
            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.MayAsk, Is.True);
        }

        [Test]
        public async Task GetState_BeforeTheInstanceIsOldEnough_SaysNotToAsk()
        {
            appSettingsMock.Setup(s => s.GetInstallTimestamp()).Returns(Now.AddDays(-AskAfterDays).AddHours(1));

            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.MayAsk, Is.False,
                "an hour short of the threshold is short of the threshold. A comparison the wrong way "
                + "round asks every brand-new instance and nothing else in the suite would notice");
        }

        [Test]
        public async Task GetState_TheMomentTheInstanceIsOldEnough_SaysToAsk()
        {
            appSettingsMock.Setup(s => s.GetInstallTimestamp()).Returns(Now.AddDays(-AskAfterDays));

            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.MayAsk, Is.True, "the threshold is reached, not merely approached");
        }

        [Test]
        public async Task GetState_WhenTheInstallTimestampCouldNotBeEstablished_SaysNotToAsk()
        {
            appSettingsMock.Setup(s => s.GetInstallTimestamp()).Returns((DateTimeOffset?)null);

            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.MayAsk, Is.False,
                "an instance whose age nobody knows is exactly the instance this threshold exists to "
                + "keep the dialog away from");
        }

        [Test]
        public async Task GetState_WithTheAdministratorsSwitchOff_SaysNotToAsk()
        {
            masterSwitchMock.Setup(s => s.IsAllowed()).Returns(false);

            var state = await CreateService().GetStateAsync(null, TestContext.CurrentContext.CancellationToken);

            Assert.That(state.MayAsk, Is.False);
        }

        [Test]
        public async Task GetState_ForACommunityRefusal_SaysNotToAskBeforeTheWindowHasPassed()
        {
            TheBrowserAnswered(UsageDataDecision.Declined, Now.AddDays(-ReAskAfterDays).AddDays(1));

            var state = await StateForABrowserWithAToken();

            Assert.That(state.MayAsk, Is.False,
                "a day early is early. The dialog named a few months, and coming back sooner than it "
                + "said is the broken promise that costs more than the nag it avoided");
        }

        [Test]
        public async Task GetState_ForACommunityRefusal_SaysToAskOnceTheWindowHasPassed()
        {
            TheBrowserAnswered(UsageDataDecision.Declined, Now.AddDays(-ReAskAfterDays));

            var state = await StateForABrowserWithAToken();

            Assert.That(state.MayAsk, Is.True,
                "the dialog promised the question would come back, and a promise nothing acts on is "
                + "the same defect as one broken early");
        }

        // The case that makes AskedAt worth its column. Three months after refusing, this browser was
        // shown the dialog again and closed it - so its stored decision is untouched and months old,
        // and anchoring on that alone would make it due again on the very next request, and every
        // request after that.
        [Test]
        public async Task GetState_ForABrowserAskedAgainSinceItDecided_CountsFromTheAskRatherThanTheAnswer()
        {
            TheBrowserAnswered(
                UsageDataDecision.Declined,
                decidedAt: Now.AddDays(-ReAskAfterDays * 2),
                askedAt: Now.AddDays(-1));

            var state = await StateForABrowserWithAToken();

            Assert.That(state.MayAsk, Is.False);
        }

        [Test]
        public async Task GetState_ForABrowserAskedAgainLongEnoughAgo_SaysToAskOnceMore()
        {
            TheBrowserAnswered(
                UsageDataDecision.Declined,
                decidedAt: Now.AddYears(-2),
                askedAt: Now.AddDays(-ReAskAfterDays));

            var state = await StateForABrowserWithAToken();

            Assert.That(state.MayAsk, Is.True,
                "anchoring on the ask must not become a way of never asking again - each window "
                + "restarts from the last time the question was actually put");
        }

        private UsageDataConsentService CreateService()
        {
            var configuration = new UsageDataConfiguration
            {
                ConsentLivenessWindowDays = WindowDays,
                AskAfterInstallDays = AskAfterDays,
                ReAskAfterDays = ReAskAfterDays,
            };
            var monitor = Mock.Of<IOptionsMonitor<UsageDataConfiguration>>(m => m.CurrentValue == configuration);

            return new UsageDataConsentService(
                repositoryMock.Object,
                licenseServiceMock.Object,
                appSettingsMock.Object,
                masterSwitchMock.Object,
                monitor,
                new FakeTimeProvider(Now));
        }

        private void TheBrowserAnswered(UsageDataDecision decision, DateTime decidedAt, DateTime? askedAt = null)
        {
            repositoryMock
                .Setup(r => r.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UsageDataConsent
                {
                    TokenHash = "digest",
                    Decision = decision,
                    DecidedAt = decidedAt,
                    AskedAt = askedAt,
                });
        }

        private Task<UsageDataState> StateForABrowserWithAToken()
        {
            return CreateService().GetStateAsync("a-token", TestContext.CurrentContext.CancellationToken);
        }

        private sealed class FakeTimeProvider(DateTime now) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(now, TimeSpan.Zero);
        }
    }
}
