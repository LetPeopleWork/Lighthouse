using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.UsageData
{
    /// <summary>
    /// Everything this gate refuses is refused in silence, on purpose - a browser that withdrew and
    /// left a tab open would otherwise fill the disk of the instance whose owner already said no. So
    /// the day's total, written once when the day turns over, is the entire operator-facing surface
    /// of the feature. If it is missing, wrong, or says "switched off" about an instance whose
    /// database is failing, then nobody outside this class can tell a quiet instance from a broken
    /// one, and the feature has no way of ever being reported as faulty.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataGateReportingTests
    {
        private const string APresentedToken = "a-token-a-browser-presented";
        private const string ATokenThisInstanceNeverMinted = "a-token-nobody-here-ever-minted";
        private const string ATokenWhoseRecordHasNoPseudonym = "a-token-whose-record-has-no-pseudonym";
        private const string MasterSwitchKey = "UsageData";

        private static readonly DateOnly Today = new(2026, 9, 13);

        private static readonly InvalidOperationException TheDatabaseWasUnwell = new("the row could not be read");

        private static readonly HttpRequestException TheCollectorCouldNotBeReached = new("nothing answered");

        private RecordingLogger<UsageDataGate> logger = null!;
        private Mock<ILighthouseClock> clock = null!;
        private DateOnly whatDayItIs;
        private int allowance;
        private List<OptionalFeature> switches = null!;
        private Mock<IUsageDataConsentRepository> consents = null!;

        [SetUp]
        public void SetUp()
        {
            logger = new RecordingLogger<UsageDataGate>();

            whatDayItIs = Today;

            // Large enough that the allowance decides nothing unless a scenario says otherwise.
            allowance = 1000;

            clock = new Mock<ILighthouseClock>();
            clock.SetupGet(reading => reading.Today).Returns(() => whatDayItIs);
            clock.SetupGet(reading => reading.Now)
                .Returns(() => new DateTimeOffset(whatDayItIs.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));

            // Empty means no row has ever been written for the master switch, which is the state
            // every instance starts in and the one that leaves the feature on, subject to consent.
            switches = [];

            consents = new Mock<IUsageDataConsentRepository>();
            consents
                .Setup(repository => repository.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsageDataConsent?)null);
        }

        /// <summary>
        /// Two things stopped data leaving on the same day, and only one of them is a fault. Nobody
        /// agreeing is the ordinary state of a new instance and belongs at a level nobody reads; a
        /// database this could not question is the case the whole feature has no other way of
        /// reporting, and it has to arrive where an operator sees it, carrying what actually went
        /// wrong. Collapse the two and either the fault hides among the ordinary or every instance
        /// where nobody has answered yet looks broken.
        /// </summary>
        [Test]
        public async Task ADayInWhichSomethingBrokeAndSomethingWasMerelyNotAgreedTo_ReportsThemApart()
        {
            AskingTheRecordFailsOnceAndOtherwiseFindsNobody();
            var gate = AGate();

            await AskThreeTimes(gate);
            await TomorrowArrivesAndSomethingAsksAgain(gate);

            var aboutTheFault = TheLineAbout(UsageDataSuppressionReason.EvaluationFailed);
            var aboutNobodyAgreeing = TheLineAbout(UsageDataSuppressionReason.NoLiveConsent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aboutTheFault.Level, Is.EqualTo(LogLevel.Warning),
                    "an instance that could not ask its own database whether anybody agreed reported "
                    + "it where nobody looks, so the one fault this feature can have goes unnoticed "
                    + "for as long as the instance runs");
                Assert.That(aboutTheFault.Failure, Is.SameAs(TheDatabaseWasUnwell),
                    "the line naming the fault does not carry the fault, so whoever reads it knows "
                    + "something failed and has nothing to go on");
                Assert.That(aboutTheFault.Message, Does.Contain("1 batch"),
                    "the line does not say how many batches it is about, which is the difference "
                    + "between a one-off and a database that has been failing all day");
                Assert.That(aboutNobodyAgreeing.Level, Is.EqualTo(LogLevel.Debug),
                    "nobody having agreed is the ordinary state of a new instance, and reporting it "
                    + "where an operator sees it is how they learn to ignore these lines");
                Assert.That(aboutNobodyAgreeing.Failure, Is.Null,
                    "a line about nobody having agreed carries a database failure that has nothing "
                    + "to do with it, which sends whoever reads it after the wrong thing");
                Assert.That(aboutNobodyAgreeing.Message, Does.Contain("2 batch"),
                    "the total is not the number of batches it was actually asked about, so the one "
                    + "figure an operator gets is not a figure");
            }
        }

        /// <summary>
        /// The total is per day, and a total that never resets is a running sum wearing a daily
        /// total's clothes - it grows every day whatever happens, so it stops being able to say
        /// anything about today.
        /// </summary>
        [Test]
        public async Task WhatYesterdayCounted_IsNotAddedToTodaysTotal()
        {
            var gate = AGate();

            await Ask(gate);
            await Ask(gate);
            await TomorrowArrivesAndSomethingAsksAgain(gate);
            await TomorrowArrivesAndSomethingAsksAgain(gate);

            var totals = logger.Written(LogLevel.Debug);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(totals, Has.Count.EqualTo(2),
                    "a day that suppressed something did not report it, or a day reported twice. "
                    + "Written: " + string.Join(" | ", totals));
                Assert.That(totals[0], Does.Contain("2 batch"),
                    "the first day's total is not what the first day counted");
                Assert.That(totals[1], Does.Contain("1 batch"),
                    "the second day's total still carries the first day's count, so every day from "
                    + "here reads busier than it was");
            }
        }

        /// <summary>
        /// An operator who turns the feature off wants it off, not off-and-shouting. And what they
        /// read afterwards has to say it was them: a line that reads like a fault sends somebody
        /// looking for a problem they created on purpose.
        /// </summary>
        [Test]
        public async Task TheFeatureSwitchedOffAtTheInstance_LetsNothingOutAndIsReportedAsAChoice()
        {
            switches.Add(new OptionalFeature { Id = 1, Key = MasterSwitchKey, Enabled = false });
            var gate = AGate();

            var permit = await Ask(gate);
            await TomorrowArrivesAndSomethingAsksAgain(gate);

            var reported = TheLineAbout(UsageDataSuppressionReason.MasterSwitchOff);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(permit, Is.Null,
                    "the switch an operator turned off let a batch through anyway, so turning the "
                    + "feature off does not turn the feature off");
                Assert.That(reported.Level, Is.EqualTo(LogLevel.Debug),
                    "an instance doing exactly what it was told reports it where an operator sees "
                    + "it, which turns a deliberate setting into a daily complaint");
            }
        }

        /// <summary>
        /// One row among many, found by name. Read by position or by the wrong name, whichever
        /// optional feature happens to be first decides whether usage data flows - so somebody
        /// turning off something unrelated silences this, or turning this off changes nothing.
        /// </summary>
        [Test]
        public async Task ASwitchBelongingToSomeOtherFeature_DecidesNothingHere()
        {
            switches.Add(new OptionalFeature { Id = 1, Key = "SomethingElseEntirely", Enabled = false });
            switches.Add(new OptionalFeature { Id = 2, Key = MasterSwitchKey, Enabled = true });
            AskingTheRecordFindsABrowserThatAgreed();
            var gate = AGate();

            Assert.That(await Ask(gate), Is.Not.Null,
                "a switch belonging to another feature decided whether usage data may leave, so "
                + "what turns this feature off is not the switch that names it");
        }

        /// <summary>
        /// Three different things all mean the same thing to an operator: nobody who agreed is
        /// behind this batch. Counting them under one heading is what keeps the daily line readable
        /// - but losing one of them under that heading makes the number smaller than the truth, and
        /// the number is the only thing anybody outside this instance ever sees.
        /// </summary>
        [Test]
        public async Task EveryWayABatchCanBeTurnedAwayForWantOfConsent_LandsInTheSameTotal()
        {
            TheRecordHoldsNothingFor(ATokenThisInstanceNeverMinted);
            TheRecordHolds(ATokenWhoseRecordHasNoPseudonym, ABrowserThatAgreed(withAPseudonym: false));
            var gate = AGate();

            await AskWith(gate, "   ");
            await AskWith(gate, ATokenThisInstanceNeverMinted);
            await AskWith(gate, ATokenWhoseRecordHasNoPseudonym);
            await TomorrowArrivesAndSomethingAsksAgain(gate);

            Assert.That(TheLineAbout(UsageDataSuppressionReason.NoLiveConsent).Message, Does.Contain("3 batch"),
                "a batch turned away for want of consent was left out of the day's total, so the one "
                + "number an operator has understates what this instance is throwing away");
        }

        /// <summary>
        /// The two outcomes an operator has to be able to tell apart. An allowance that ran out is
        /// either an unusually busy instance or one being abused; a collector that took nothing is
        /// this product's own pipe being broken. Both arrive as "the data did not leave", both are
        /// invisible to every browser by design, and the reason on the daily line is the only thing
        /// that separates them.
        /// </summary>
        [Test]
        public async Task ADayThatRanOutOfAllowanceAndAlsoCouldNotReachTheCollector_NamesTheTwoApart()
        {
            AskingTheRecordFindsABrowserThatAgreed();
            allowance = 1;
            var gate = AGate();

            await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);
            await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);
            gate.GiveBackWhatCouldNotBeSent(1, TheCollectorCouldNotBeReached);

            whatDayItIs = whatDayItIs.AddDays(1);
            await gate.RequestPermitToSendAsync(APresentedToken, 1, CancellationToken.None);

            var aboutTheAllowance = TheLineAbout(UsageDataSuppressionReason.BudgetExhausted);
            var aboutTheCollector = TheLineAbout(UsageDataSuppressionReason.SendFailed);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aboutTheAllowance.Level, Is.EqualTo(LogLevel.Warning),
                    "an instance that emptied its share of a shared allowance reported it where "
                    + "nobody looks, so neither an unusually busy day nor an abusive one is visible");
                Assert.That(aboutTheCollector.Level, Is.EqualTo(LogLevel.Warning),
                    "a collector that took nothing reported it where nobody looks, which is the one "
                    + "case where somebody agreed, there was room, and the data still did not arrive");
            }
        }

        private RecordingLogger<UsageDataGate>.Entry TheLineAbout(UsageDataSuppressionReason reason)
        {
            var matching = logger.Everything
                .Where(entry => entry.Message.EndsWith(reason.ToString(), StringComparison.Ordinal))
                .ToList();

            Assert.That(matching, Has.Count.EqualTo(1),
                $"expected exactly one line about {reason}. None means the day's total for it was "
                + "never written, so nothing outside this instance can tell it happened; more than "
                + "one means a line per occurrence, which is the flood this counting exists to "
                + "prevent. Written: "
                + string.Join(" | ", logger.Everything.Select(entry => $"[{entry.Level}] {entry.Message}")));

            return matching[0];
        }

        private static async Task<UsageDataEmitPermit?> Ask(UsageDataGate gate)
        {
            return await AskWith(gate, APresentedToken);
        }

        private static async Task<UsageDataEmitPermit?> AskWith(UsageDataGate gate, string? token)
        {
            return await gate.RequestPermitAsync(token, CancellationToken.None);
        }

        private static async Task AskThreeTimes(UsageDataGate gate)
        {
            for (var asked = 0; asked < 3; asked++)
            {
                await Ask(gate);
            }
        }

        /// <summary>
        /// The day's total is written when the next day's first question arrives, because there is
        /// nothing else running that could notice midnight passing.
        /// </summary>
        private async Task TomorrowArrivesAndSomethingAsksAgain(UsageDataGate gate)
        {
            whatDayItIs = whatDayItIs.AddDays(1);

            await Ask(gate);
        }

        private void AskingTheRecordFindsABrowserThatAgreed()
        {
            consents
                .Setup(repository => repository.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => ABrowserThatAgreed());
        }

        private void TheRecordHolds(string token, UsageDataConsent consent)
        {
            consents
                .Setup(repository => repository.FindByTokenHashAsync(
                    UsageDataConsentToken.HashOf(token), It.IsAny<CancellationToken>()))
                .ReturnsAsync(consent);
        }

        private void TheRecordHoldsNothingFor(string token)
        {
            consents
                .Setup(repository => repository.FindByTokenHashAsync(
                    UsageDataConsentToken.HashOf(token), It.IsAny<CancellationToken>()))
                .ReturnsAsync((UsageDataConsent?)null);
        }

        private UsageDataConsent ABrowserThatAgreed(bool withAPseudonym = true)
        {
            return new UsageDataConsent
            {
                TokenHash = "does-not-matter-here",
                Decision = UsageDataDecision.Granted,
                LastSeenAt = whatDayItIs.ToDateTime(new TimeOnly(12, 0)),
                AnalyticsId = withAPseudonym ? "a-pseudonym" : null,
            };
        }

        /// <summary>
        /// The failure is the second question rather than the first because the very first question
        /// a fresh gate is asked also starts the day it is counting, and starting a day discards the
        /// failure it is holding. A day that opens on a fault is a corner worth knowing about; it is
        /// not what this is measuring.
        /// </summary>
        private void AskingTheRecordFailsOnceAndOtherwiseFindsNobody()
        {
            var asked = 0;

            consents
                .Setup(repository => repository.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => ++asked == 2
                    ? throw TheDatabaseWasUnwell
                    : Task.FromResult((UsageDataConsent?)null));
        }

        private UsageDataGate AGate()
        {
            var features = new Mock<IRepository<OptionalFeature>>();
            features
                .Setup(repository => repository.GetByPredicate(It.IsAny<Func<OptionalFeature, bool>>()))
                .Returns((Func<OptionalFeature, bool> named) => switches.Find(one => named(one)));

            var services = new ServiceCollection();
            services.AddScoped(_ => consents.Object);
            services.AddScoped(_ => features.Object);

            var scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();

            var configuration = new Mock<IOptionsMonitor<UsageDataConfiguration>>();
            configuration
                .SetupGet(options => options.CurrentValue)
                .Returns(() => new UsageDataConfiguration { DailyEventBudget = allowance });

            return new UsageDataGate(scopeFactory, configuration.Object, clock.Object, logger);
        }
    }
}
