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
    /// Epic 5733 slice 01c (ADO #5980) - the per-instance daily allowance, at its own seam.
    ///
    /// Not through HTTP, for two reasons the integration host cannot get around. The allowance resets
    /// on a calendar day boundary and no fixture can wait out midnight, but the clock here is a seam
    /// that can simply be told it is tomorrow. And the browser-facing answer is deliberately the same
    /// whether a batch was kept or thrown away, so nothing observable over HTTP can distinguish the
    /// two - which is the point of the design and the reason it has to be checked from this side.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataGateBudgetTests
    {
        private const string APresentedToken = "a-token-a-browser-presented";
        private const int Allowance = 3;

        private static readonly DateOnly Today = new(2026, 9, 13);

        private static readonly HttpRequestException TheCollectorCouldNotBeReached = new("nothing answered");

        private RecordingLogger<UsageDataGate> logger = null!;
        private Mock<ILighthouseClock> clock = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new RecordingLogger<UsageDataGate>();
            clock = new Mock<ILighthouseClock>();
            clock.SetupGet(c => c.Today).Returns(Today);
            clock.SetupGet(c => c.Now).Returns(new DateTimeOffset(Today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
        }

        /// <summary>
        /// The allowance being protected is shared across every instance in the world, so it has to
        /// stop somewhere. Stopping late by even a multiple matters: forty instances each sending
        /// twice what they were configured for is the whole allowance gone.
        /// </summary>
        [Test]
        public async Task PastTheDaysAllowance_NothingFurtherIsLetOut()
        {
            var gate = AGate();

            var letOut = await HowManyAreLetOut(gate, asks: Allowance * 4);

            Assert.That(letOut, Is.EqualTo(Allowance),
                "the day let out a different number than it was configured for, so the number an "
                + "operator sets is not the number that leaves");
        }

        /// <summary>
        /// The precondition the count is worthless without: a gate that let nothing out at all would
        /// also "never exceed the allowance", and would have switched the feature off instead.
        /// </summary>
        [Test]
        public async Task InsideTheDaysAllowance_ThingsAreActuallyLetOut()
        {
            var gate = AGate();

            var letOut = await HowManyAreLetOut(gate, asks: Allowance);

            Assert.That(letOut, Is.EqualTo(Allowance),
                "nothing was let out even inside the allowance, so counting what was stopped past it "
                + "measures a pipe that was never running");
        }

        /// <summary>
        /// A line per drop would let whoever triggered the drops fill the disk of the instance they
        /// are already abusing - the defence turned into the attack. No line at all leaves an
        /// operator unable to tell a busy day from an attack.
        /// </summary>
        [Test]
        public async Task EmptyingTheAllowanceOverAndOver_IsSaidOutLoudExactlyOnce()
        {
            var gate = AGate();

            await HowManyAreLetOut(gate, asks: Allowance * 100);

            var saidOutLoud = logger.Warnings;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(saidOutLoud, Has.Count.EqualTo(1),
                    "the number of warnings tracks the number of drops, so an attacker controls how "
                    + "much this instance writes to its own disk");
                Assert.That(saidOutLoud[0], Does.Contain(Allowance.ToString()),
                    "the line does not name the allowance, so an operator reading it cannot tell "
                    + "whether the number is too low or the traffic too high");
            }
        }

        /// <summary>
        /// A day's allowance that never came back would switch the feature off permanently the first
        /// time it was reached, which is an outage wearing a budget's clothes.
        /// </summary>
        [Test]
        public async Task WhenTheDayTurnsOver_TheAllowanceStartsAgain()
        {
            var gate = AGate();
            await HowManyAreLetOut(gate, asks: Allowance * 2);

            clock.SetupGet(c => c.Today).Returns(Today.AddDays(1));
            var letOutTomorrow = await HowManyAreLetOut(gate, asks: Allowance * 2);

            Assert.That(letOutTomorrow, Is.EqualTo(Allowance),
                "yesterday's count is still being spent today, so one busy day silences the instance "
                + "for as long as the process lives");
        }

        /// <summary>
        /// Both ends of the pipe ask whether a browser is still agreeing - once when a batch is taken
        /// in, once when it is sent. Only the second spends the allowance. If the first spent it too,
        /// every batch would be charged twice and sending would stop at half the configured number,
        /// which no configuration could correct for.
        /// </summary>
        [Test]
        public async Task AskingWhetherABatchMayBeKept_DoesNotSpendTheAllowance()
        {
            var gate = AGate();

            for (var asked = 0; asked < Allowance * 4; asked++)
            {
                await gate.RequestPermitAsync(APresentedToken, CancellationToken.None);
            }

            var letOut = await HowManyAreLetOut(gate, asks: Allowance);

            Assert.That(letOut, Is.EqualTo(Allowance),
                "taking a batch in spent part of the allowance, so the number that actually leaves is "
                + "a fraction of the number an operator configured");
        }

        /// <summary>
        /// An allowance is spent by sending, not by trying. It is taken before the call - two batches
        /// going out at once must not both be told there is room for the last of it - so a collector
        /// that is refusing everything would otherwise empty a whole day onto batches that never
        /// arrived, and the instance would then stay silent until midnight, long after the collector
        /// came back. The day an operator can no longer tell those two days apart is the day this
        /// number stops meaning anything.
        /// </summary>
        [Test]
        public async Task WhatNeverArrived_DoesNotSpendTheDaysAllowance()
        {
            var gate = AGate();

            for (var attempt = 0; attempt < Allowance * 4; attempt++)
            {
                var permit = await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);

                if (permit is not null)
                {
                    gate.GiveBackWhatCouldNotBeSent(1, TheCollectorCouldNotBeReached);
                }
            }

            var letOutOnceItComesBack = await HowManyAreLetOut(gate, asks: Allowance);

            Assert.That(letOutOnceItComesBack, Is.EqualTo(Allowance),
                "an hour of a collector being unreachable has spent the day's allowance on nothing, "
                + "so this instance sends nothing further until midnight even though the collector "
                + "is answering again");
        }

        /// <summary>
        /// The half that makes the other half findable. A pipe that is dropping everything and a pipe
        /// that is quiet because nobody agreed look identical from outside - that is this feature
        /// working as designed - so the one case where somebody agreed, there was room, and the data
        /// still did not arrive has to be said where an operator sees it. Once, for the reason every
        /// other line here is said once.
        /// </summary>
        [Test]
        public async Task ACollectorThatNeverTakesAnything_IsSaidOutLoudExactlyOnce()
        {
            var gate = AGate();

            for (var attempt = 0; attempt < Allowance * 100; attempt++)
            {
                await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);
                gate.GiveBackWhatCouldNotBeSent(1, TheCollectorCouldNotBeReached);
            }

            Assert.That(logger.Warnings, Has.Count.EqualTo(1),
                "expected exactly one line, and only that one. None leaves a broken pipe reading as "
                + "an instance nobody agreed on. A line per failure lets whoever is causing them "
                + "decide how much this instance writes to its own disk. A second line means the "
                + "allowance was emptied by batches that never arrived. Written: "
                + string.Join(" | ", logger.Warnings));
        }

        private static async Task<int> HowManyAreLetOut(UsageDataGate gate, int asks)
        {
            var letOut = 0;

            for (var ask = 0; ask < asks; ask++)
            {
                var permit = await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);

                if (permit is not null)
                {
                    letOut++;
                }
            }

            return letOut;
        }

        private UsageDataGate AGate()
        {
            var consents = new Mock<IUsageDataConsentRepository>();
            consents
                .Setup(repository => repository.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(ABrowserThatAgreed());
            consents
                .Setup(repository => repository.TouchAsync(
                    It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Null means no row has ever been written for the master switch, which is the state every
            // instance starts in and the one that leaves the feature on, subject to consent.
            var features = new Mock<IRepository<OptionalFeature>>();
            features
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>()))
                .Returns((OptionalFeature?)null);

            var services = new ServiceCollection();
            services.AddScoped(_ => consents.Object);
            services.AddScoped(_ => features.Object);

            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            return new UsageDataGate(scopeFactory, AnAllowanceOf(Allowance), clock.Object, logger);
        }

        private UsageDataConsent ABrowserThatAgreed()
        {
            return new UsageDataConsent
            {
                TokenHash = "does-not-matter-here",
                Decision = UsageDataDecision.Granted,
                LastSeenAt = clock.Object.Now.UtcDateTime,
                AnalyticsId = "a-pseudonym",
            };
        }

        private static IOptionsMonitor<UsageDataConfiguration> AnAllowanceOf(int budget)
        {
            var configuration = new Mock<IOptionsMonitor<UsageDataConfiguration>>();
            configuration
                .SetupGet(options => options.CurrentValue)
                .Returns(new UsageDataConfiguration { DailyEventBudget = budget });

            return configuration.Object;
        }
    }
}
