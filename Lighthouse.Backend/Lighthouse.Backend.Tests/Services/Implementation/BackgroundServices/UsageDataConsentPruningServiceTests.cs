using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Services.Implementation.BackgroundServices;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.BackgroundServices
{
    /// <summary>
    /// What the service decides, which is everything the query is not allowed to.
    ///
    /// The repository takes two instants and a flag and does as it is told; the tests beside it pin
    /// what it does with them. Nothing there can say whether the caller counted backwards from now
    /// or forwards, or whether it asked the licence at all - flip either and every repository test
    /// still passes.
    /// </summary>
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataConsentPruningServiceTests
    {
        private static readonly DateTime Now = new(2026, 9, 13, 12, 0, 0, DateTimeKind.Utc);

        private const int RetentionDays = 180;
        private const int ReAskAfterDays = 90;

        private Mock<IUsageDataConsentRepository> repositoryMock;
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void Setup()
        {
            repositoryMock = new Mock<IUsageDataConsentRepository>();
            licenseServiceMock = new Mock<ILicenseService>();
        }

        [Test]
        public async Task PruneNow_ForgetsBrowsersLastSeenBeforeTheRetentionWindow()
        {
            await CreateService().PruneNowAsync(TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                Now.AddDays(-RetentionDays),
                It.IsAny<DateTime>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PruneNow_TreatsAQuestionAsFallenDueOnceTheReAskWindowHasPassed()
        {
            await CreateService().PruneNowAsync(TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                It.IsAny<DateTime>(),
                Now.AddDays(-ReAskAfterDays),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));
        }

        // Both instants count backwards from now. Counting forwards would make every row look newer
        // than both thresholds and the pass would quietly forget nothing at all, for ever - which
        // reads, from outside, exactly like a pass that is working.
        [Test]
        public async Task PruneNow_CountsBackwardsFromNow_NotForwards()
        {
            await CreateService().PruneNowAsync(TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                It.Is<DateTime>(instant => instant < Now),
                It.Is<DateTime>(instant => instant < Now),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()));
        }

        [TestCase(true, Description = "Premium: a refusal is final, so its row is never forgotten")]
        [TestCase(false, Description = "Community: a refusal is owed another question and ages out after it")]
        public async Task PruneNow_TellsTheQueryWhetherTheLicenceMakesARefusalFinal(bool premium)
        {
            licenseServiceMock.Setup(l => l.CanUsePremiumFeatures()).Returns(premium);

            await CreateService().PruneNowAsync(TestContext.CurrentContext.CancellationToken);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                premium,
                It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task PruneNow_ReportsHowManyBrowsersWereForgotten()
        {
            repositoryMock
                .Setup(r => r.PruneStaleAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(7);

            var forgotten = await CreateService().PruneNowAsync(TestContext.CurrentContext.CancellationToken);

            Assert.That(forgotten, Is.EqualTo(7));
        }

        // The loop around the pass, which is not about what gets forgotten but about the service
        // still being there tomorrow. Both of these are claims the code makes in its own comments
        // and neither was exercised by anything.

        [Test]
        public async Task TheLoop_RunsAPassWithoutWaitingForTheFirstDay()
        {
            var clock = new FakeTimeProvider(Now);
            var service = CreateService(clock);

            await service.StartAsync(CancellationToken.None);
            await WaitUntilPasses(1);
            await service.StopAsync(CancellationToken.None);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task TheLoop_SurvivesAPassThatFailed_AndTriesAgainTheNextDay()
        {
            repositoryMock
                .SetupSequence(r => r.PruneStaleAsync(
                    It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("the database went away"))
                .ReturnsAsync(3);

            var clock = new FakeTimeProvider(Now);
            var service = CreateService(clock);

            await service.StartAsync(CancellationToken.None);

            // Advanced from inside the wait rather than once, because a single jump can land before
            // the loop has reached its delay at all - and a timer set against a clock that has
            // already moved never fires, so the test would hang rather than fail.
            await WaitUntilPasses(2, nudge: clock);

            await service.StopAsync(CancellationToken.None);

            Assert.Pass("a housekeeping pass that threw took the next attempt with it if this hangs");
        }

        // Without the wait between passes this is a loop with nothing in it, running the prune
        // query as fast as the database will answer. Nothing else here would notice: every other
        // test advances the clock before looking for the next pass, so a loop that never waited
        // would satisfy them all and hammer the instance in production.
        [Test]
        public async Task TheLoop_WaitsForTheNextDayRatherThanRunningStraightAgain()
        {
            var clock = new FakeTimeProvider(Now);
            var service = CreateService(clock);

            await service.StartAsync(CancellationToken.None);
            await WaitUntilPasses(1);

            // Real time, deliberately: the clock the service waits on is the fake one and is not
            // moving, so anything that happens in this window happened without waiting at all.
            await Task.Delay(250);
            await service.StopAsync(CancellationToken.None);

            repositoryMock.Verify(r => r.PruneStaleAsync(
                It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task TheLoop_StopsWhenItIsAskedTo()
        {
            var service = CreateService(new FakeTimeProvider(Now));

            await service.StartAsync(CancellationToken.None);
            await WaitUntilPasses(1);

            Assert.That(async () => await service.StopAsync(CancellationToken.None), Throws.Nothing);
        }

        /// <summary>
        /// The loop runs on its own thread, so a test has to wait for it rather than assume it has
        /// got there. Bounded, because the failure this guards against is the loop never arriving -
        /// and a test that waits for ever reports that as a hang rather than as a failure.
        /// </summary>
        private async Task WaitUntilPasses(int passes, FakeTimeProvider? nudge = null)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);

            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    repositoryMock.Verify(r => r.PruneStaleAsync(
                        It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
                        Times.AtLeast(passes));
                    return;
                }
                catch (MockException)
                {
                    nudge?.Advance(TimeSpan.FromDays(1));
                    await Task.Delay(20);
                }
            }

            Assert.Fail($"the pruning loop did not reach pass {passes} within five seconds");
        }

        private UsageDataConsentPruningService CreateService(TimeProvider? clock = null)
        {
            var configuration = new UsageDataConfiguration
            {
                ConsentRetentionDays = RetentionDays,
                ReAskAfterDays = ReAskAfterDays,
            };

            var services = new ServiceCollection();
            services.AddSingleton(repositoryMock.Object);
            services.AddSingleton(licenseServiceMock.Object);

            return new UsageDataConsentPruningService(
                services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
                Mock.Of<IOptionsMonitor<UsageDataConfiguration>>(m => m.CurrentValue == configuration),
                clock ?? new FakeTimeProvider(Now),
                NullLogger<UsageDataConsentPruningService>.Instance);
        }
    }
}
