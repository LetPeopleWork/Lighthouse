using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// Who is let through, and what being let through costs. The endpoints cannot show any of this:
    /// a browser is answered identically whether its batch was kept or thrown away, which is the
    /// design and the reason the decision has to be checked from this side. What a browser cannot
    /// see, nobody can - so an expiry that runs the wrong way, a refresh that never happens or a
    /// refusal that quietly spends the day's allowance would all look exactly like working.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataGateConsentTests
    {
        private const string APresentedToken = "a-token-a-browser-presented";
        private const string ThisBrowsersDigest = "the-digest-of-that-token";
        private const int WindowDays = 30;
        private const int Allowance = 3;

        private static readonly DateOnly Today = new(2026, 9, 13);

        private static readonly DateTime Now = Today.ToDateTime(new TimeOnly(12, 0));

        private Mock<ILighthouseClock> clock = null!;
        private Mock<IUsageDataConsentRepository> consents = null!;

        [SetUp]
        public void SetUp()
        {
            clock = new Mock<ILighthouseClock>();
            clock.SetupGet(reading => reading.Today).Returns(Today);
            clock.SetupGet(reading => reading.Now).Returns(new DateTimeOffset(Now, TimeSpan.Zero));

            consents = new Mock<IUsageDataConsentRepository>();
            consents
                .Setup(repository => repository.TouchAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
        }

        /// <summary>
        /// Clearing browser storage sends nothing, so a browser that has gone away can only be
        /// noticed by its silence, and the window is how long that silence has to last. The edge is
        /// the part worth pinning: a window that includes its own far edge counts a browser that has
        /// been gone for exactly the configured time as still here, which is the one moment the
        /// setting was chosen to describe.
        /// </summary>
        [TestCase(0, false, TestName = "AConsentLastSeen(exactly a window ago)")]
        [TestCase(-1, false, TestName = "AConsentLastSeen(a moment beyond the window)")]
        [TestCase(1, true, TestName = "AConsentLastSeen(a moment inside the window)")]
        public async Task ABrowserLastSeenAtTheEdgeOfTheWindow_CountsOnlyIfItIsStillInsideIt(
            int ticksInsideTheEdge, bool stillCounts)
        {
            TheRecordHolds(ABrowserThatAgreed(lastSeenAt: Now.AddDays(-WindowDays).AddTicks(ticksInsideTheEdge)));

            var permit = await AGate().RequestPermitAsync(APresentedToken, CancellationToken.None);

            Assert.That(permit is not null, Is.EqualTo(stillCounts),
                "a browser exactly at the edge of the window was counted as still here when it is "
                + "not, or dropped while it still is - so how long consent survives silence is not "
                + "the number an operator configured");
        }

        /// <summary>
        /// A browser that is actively using Lighthouse must not age out from under itself, so being
        /// seen refreshes the stamp - but only when the stored one is already old, because SQLite
        /// serialises writers across the whole process and a write per flush would put every browser
        /// with a tab open in contention with the background refreshes.
        /// </summary>
        [Test]
        public async Task ABrowserStillAgreeing_HasItsConsentRefreshedFromBehindRatherThanFromTheFuture()
        {
            TheRecordHolds(ABrowserThatAgreed());

            var refreshed = new List<(string Digest, DateTime To, DateTime OnlyIfOlderThan)>();
            consents
                .Setup(repository => repository.TouchAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Callback((string digest, DateTime to, DateTime onlyIfOlderThan, CancellationToken _) =>
                    refreshed.Add((digest, to, onlyIfOlderThan)))
                .ReturnsAsync(1);

            await AGate().RequestPermitAsync(APresentedToken, CancellationToken.None);

            Assert.That(refreshed, Has.Count.EqualTo(1),
                "a browser that presented its token was not marked as seen, so a browser in daily "
                + "use silently ages out of its own consent and stops being counted");

            var (digest, refreshedTo, onlyIfOlderThan) = refreshed[0];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(digest, Is.EqualTo(ThisBrowsersDigest),
                    "some other browser's consent was refreshed by this one being seen");
                Assert.That(refreshedTo, Is.EqualTo(Now),
                    "the stamp was not moved to the moment this browser was actually seen");
                Assert.That(onlyIfOlderThan, Is.LessThan(Now),
                    "the throttle only means anything if it asks whether the stored stamp is OLDER "
                    + "than a moment already past; a threshold in the future makes every flush a "
                    + "write, which is the contention it exists to avoid");
                Assert.That(onlyIfOlderThan, Is.GreaterThan(Now.AddDays(-WindowDays)),
                    "and it has to be a small slice of the window rather than a multiple of it - a "
                    + "cutoff further back than the window itself never refreshes anything, so every "
                    + "browser silently ages out while it is still here");
            }
        }

        /// <summary>
        /// The allowance is a single shared one, drawn on by every instance in the world, and it is
        /// spent by sending. Charging it for a batch that was refused means an instance where nobody
        /// agreed empties a day of it on nothing - and then the first person who does agree is
        /// dropped for the rest of the day, with the only line about it saying the allowance was
        /// spent, which reads as a busy day.
        /// </summary>
        [Test]
        public async Task ABatchNobodyAgreedTo_DoesNotSpendTheDaysAllowance()
        {
            TheRecordHolds(null);
            var gate = AGate();

            for (var refused = 0; refused < Allowance * 4; refused++)
            {
                await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);
            }

            TheRecordHolds(ABrowserThatAgreed());

            Assert.That(await HowManyAreLetOut(gate, asks: Allowance), Is.EqualTo(Allowance),
                "batches from a browser that never agreed spent the day's allowance, so the first "
                + "person on this instance who does agree is dropped for the rest of the day");
        }

        /// <summary>
        /// The pseudonym is what every event a browser reports is counted under. A record that says
        /// yes but carries none of it cannot be sent under anything, and sending under a blank would
        /// merge that browser's history with every other blank on the collector.
        /// </summary>
        [TestCase(null, TestName = "AConsentCarryingNoPseudonym_IsNotAPermit(never written)")]
        [TestCase("", TestName = "AConsentCarryingNoPseudonym_IsNotAPermit(empty)")]
        [TestCase("   ", TestName = "AConsentCarryingNoPseudonym_IsNotAPermit(blank)")]
        public async Task AConsentCarryingNoPseudonym_IsNotAPermit(string? whatIsOnTheRecord)
        {
            TheRecordHolds(ABrowserThatAgreed(analyticsId: whatIsOnTheRecord));

            var permit = await AGate().RequestPermitAsync(APresentedToken, CancellationToken.None);

            Assert.That(permit, Is.Null,
                "a browser with nothing to be counted under was let through anyway, so its events "
                + "arrive under a blank and merge with everyone else's");
        }

        private static async Task<int> HowManyAreLetOut(UsageDataGate gate, int asks)
        {
            var letOut = 0;

            for (var ask = 0; ask < asks; ask++)
            {
                if (await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None) is not null)
                {
                    letOut++;
                }
            }

            return letOut;
        }

        private static UsageDataConsent ABrowserThatAgreed(
            DateTime? lastSeenAt = null, string? analyticsId = "a-pseudonym")
        {
            return new UsageDataConsent
            {
                TokenHash = ThisBrowsersDigest,
                Decision = UsageDataDecision.Granted,
                LastSeenAt = lastSeenAt ?? Now,
                AnalyticsId = analyticsId,
            };
        }

        private void TheRecordHolds(UsageDataConsent? consent)
        {
            consents
                .Setup(repository => repository.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(consent);
        }

        private UsageDataGate AGate()
        {
            // Null means no row has ever been written for the master switch, which is the state
            // every instance starts in and the one that leaves the feature on, subject to consent.
            var features = new Mock<IRepository<OptionalFeature>>();
            features
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>()))
                .Returns((OptionalFeature?)null);

            var services = new ServiceCollection();
            services.AddScoped(_ => consents.Object);
            services.AddScoped(_ => features.Object);

            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            var configuration = new Mock<IOptionsMonitor<UsageDataConfiguration>>();
            configuration
                .SetupGet(options => options.CurrentValue)
                .Returns(new UsageDataConfiguration
                {
                    ConsentLivenessWindowDays = WindowDays,
                    DailyEventBudget = Allowance,
                });

            return new UsageDataGate(
                scopeFactory, configuration.Object, clock.Object, new RecordingLogger<UsageDataGate>());
        }
    }
}
