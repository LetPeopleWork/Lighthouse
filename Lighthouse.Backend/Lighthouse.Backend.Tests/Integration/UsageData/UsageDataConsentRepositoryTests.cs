using Lighthouse.Backend.Models.UsageData;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Integration.UsageData
{
    /// <summary>
    /// The store's own guarantees, which the endpoint tests cannot reach. Those run black box over
    /// HTTP at a single instant against fresh rows, so nothing they can do distinguishes "older than
    /// the threshold" from "newer than" - every comparison in here could be flipped and every one of
    /// them would still pass. These tests exist because mutation testing said so.
    ///
    /// Against SQLite rather than the in-memory provider, because the conditional updates these
    /// methods are built on do not exist there.
    /// </summary>
    [TestFixture]
    [Category("epic-5733-opt-in-usage-data")]
    public class UsageDataConsentRepositoryTests : IntegrationTestBase
    {
        private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);

        private IUsageDataConsentRepository Repository =>
            ServiceProvider.GetRequiredService<IUsageDataConsentRepository>();

        [Test]
        public async Task AddAsync_PersistsTheRow_SoALaterLookupFindsIt()
        {
            await Repository.AddAsync(Consent("kept", UsageDataDecision.Granted, Now), TestContext.CurrentContext.CancellationToken);

            var found = await Repository.FindByTokenHashAsync("kept", TestContext.CurrentContext.CancellationToken);

            Assert.That(found, Is.Not.Null, "a consent that is not stored is a decision the person made and we lost");
        }

        [Test]
        public async Task FindByTokenHashAsync_WithADigestThisInstanceNeverStored_FindsNothing()
        {
            await Repository.AddAsync(Consent("real", UsageDataDecision.Granted, Now), TestContext.CurrentContext.CancellationToken);

            var found = await Repository.FindByTokenHashAsync("never-stored", TestContext.CurrentContext.CancellationToken);

            Assert.That(found, Is.Null);
        }

        [Test]
        public async Task TouchAsync_WhenTheStampIsOlderThanTheThrottle_MovesIt()
        {
            await Repository.AddAsync(Consent("stale", UsageDataDecision.Granted, Now.AddHours(-10)), TestContext.CurrentContext.CancellationToken);

            var moved = await Repository.TouchAsync("stale", Now, Now.AddHours(-7), TestContext.CurrentContext.CancellationToken);

            Assert.That(moved, Is.EqualTo(1), "a browser that is still here has to keep its consent alive");
        }

        [Test]
        public async Task TouchAsync_WhenTheStampIsAlreadyFresh_LeavesItAlone()
        {
            await Repository.AddAsync(Consent("fresh", UsageDataDecision.Granted, Now.AddMinutes(-5)), TestContext.CurrentContext.CancellationToken);

            var moved = await Repository.TouchAsync("fresh", Now, Now.AddHours(-7), TestContext.CurrentContext.CancellationToken);

            Assert.That(moved, Is.Zero,
                "without the throttle every page load becomes a write, and SQLite serialises writers "
                + "across the whole process");
        }

        [Test]
        public async Task TouchAsync_WhenTheStampSitsExactlyOnTheThrottle_LeavesItAlone()
        {
            await Repository.AddAsync(Consent("exact", UsageDataDecision.Granted, Now.AddHours(-7)), TestContext.CurrentContext.CancellationToken);

            var moved = await Repository.TouchAsync("exact", Now, Now.AddHours(-7), TestContext.CurrentContext.CancellationToken);

            Assert.That(moved, Is.Zero,
                "the throttle asks whether the stamp is older than the cutoff, not whether it has "
                + "reached it - a browser refreshing on the exact second would otherwise write twice");
        }

        [Test]
        public async Task TouchAsync_WithADigestThisInstanceNeverStored_MovesNothing()
        {
            await Repository.AddAsync(Consent("mine", UsageDataDecision.Granted, Now.AddHours(-10)), TestContext.CurrentContext.CancellationToken);

            var moved = await Repository.TouchAsync("not-mine", Now, Now.AddHours(-7), TestContext.CurrentContext.CancellationToken);

            Assert.That(moved, Is.Zero, "one browser must not be able to keep another's consent alive");
        }

        [Test]
        public async Task TryRevokeAsync_OnAGrant_WithdrawsItAndSaysSo()
        {
            await Repository.AddAsync(Consent("granted", UsageDataDecision.Granted, Now), TestContext.CurrentContext.CancellationToken);

            var withdrawn = await Repository.TryRevokeAsync("granted", Now, TestContext.CurrentContext.CancellationToken);
            var afterwards = await Repository.FindByTokenHashAsync("granted", TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withdrawn, Is.EqualTo(1));
                Assert.That(afterwards?.Decision, Is.EqualTo(UsageDataDecision.Revoked),
                    "a withdrawal is its own state - a deleted row would read as never having decided, "
                    + "and the browser would be asked again");
            }
        }

        [Test]
        public async Task TryRevokeAsync_OnARefusal_ChangesNothing()
        {
            await Repository.AddAsync(Consent("declined", UsageDataDecision.Declined, Now), TestContext.CurrentContext.CancellationToken);

            var withdrawn = await Repository.TryRevokeAsync("declined", Now.AddDays(1), TestContext.CurrentContext.CancellationToken);
            var afterwards = await Repository.FindByTokenHashAsync("declined", TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(withdrawn, Is.Zero);
                Assert.That(afterwards?.Decision, Is.EqualTo(UsageDataDecision.Declined));
                Assert.That(afterwards?.DecidedAt, Is.EqualTo(Now),
                    "moving the timestamp would make the record of when somebody decided untrue");
            }
        }

        [Test]
        public async Task TryRevokeAsync_OnAWithdrawalAlreadyMade_ChangesNothing()
        {
            await Repository.AddAsync(Consent("already", UsageDataDecision.Revoked, Now), TestContext.CurrentContext.CancellationToken);

            var withdrawn = await Repository.TryRevokeAsync("already", Now.AddDays(1), TestContext.CurrentContext.CancellationToken);

            Assert.That(withdrawn, Is.Zero);
        }

        [Test]
        public async Task PruneStaleAsync_RemovesWhatHasAgedOut_AndKeepsWhatHasNot()
        {
            await Repository.AddAsync(Consent("ancient", UsageDataDecision.Granted, Now.AddDays(-100)), TestContext.CurrentContext.CancellationToken);
            await Repository.AddAsync(Consent("recent", UsageDataDecision.Granted, Now.AddDays(-1)), TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(lastSeenBefore: Now.AddDays(-30));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.EqualTo(1));
                Assert.That(await Repository.FindByTokenHashAsync("recent", TestContext.CurrentContext.CancellationToken), Is.Not.Null,
                    "pruning on age must not take a browser that is still here with it");
            }
        }

        [Test]
        public async Task PruneStaleAsync_LeavesARowSittingExactlyOnTheThreshold()
        {
            var edge = Now.AddDays(-30);
            await Repository.AddAsync(Consent("edge", UsageDataDecision.Granted, edge), TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(lastSeenBefore: edge);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.Zero);
                Assert.That(await Repository.FindByTokenHashAsync("edge", TestContext.CurrentContext.CancellationToken), Is.Not.Null,
                    "pruning and counting have to agree about the edge, or a row stops counting as "
                    + "live on one query and is still there for the other");
            }
        }

        // Slice 02 (#5835). The dialog tells a Community reader we will come back in a few months.
        // Forgetting the row before then means the next visit is met by the question early, and by a
        // route nobody would look down: the promise is kept by the cadence and broken by housekeeping.
        [Test]
        public async Task PruneStaleAsync_KeepsARefusalThatIsStillOwedItsPromisedQuestion()
        {
            var refused = Consent("owed", UsageDataDecision.Declined, Now.AddDays(-200));
            refused.DecidedAt = Now.AddDays(-30);
            await Repository.AddAsync(refused, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180), owedNothingSince: Now.AddDays(-90));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.Zero);
                Assert.That(await Repository.FindByTokenHashAsync("owed", TestContext.CurrentContext.CancellationToken), Is.Not.Null,
                    "old enough to forget, and still owed the question we said we would ask - so it stays");
            }
        }

        [Test]
        public async Task PruneStaleAsync_ForgetsARefusalOnceItsQuestionHasFallenDue()
        {
            var refused = Consent("settled", UsageDataDecision.Declined, Now.AddDays(-200));
            refused.DecidedAt = Now.AddDays(-200);
            await Repository.AddAsync(refused, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180), owedNothingSince: Now.AddDays(-90));

            Assert.That(removed, Is.EqualTo(1),
                "a browser that refused, was owed one more question and never came back for it is a "
                + "browser that has gone. Keeping the row for ever is how this table grows without bound");
        }

        // The case the column exists for. This browser refused long ago but was asked again recently
        // and closed the dialog, so its decision is ancient while the promise it is owed is fresh -
        // and a prune reading the decision alone would forget it and ask early on the next visit.
        [Test]
        public async Task PruneStaleAsync_CountsFromTheLastQuestionRatherThanTheLastAnswer()
        {
            var refused = Consent("asked-since", UsageDataDecision.Declined, Now.AddDays(-200));
            refused.DecidedAt = Now.AddDays(-400);
            refused.AskedAt = Now.AddDays(-10);
            await Repository.AddAsync(refused, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180), owedNothingSince: Now.AddDays(-90));

            Assert.That(removed, Is.Zero);
        }

        // Where the licence makes a refusal permanent there is no question left to fall due, so the
        // row can never reach "past its re-ask date" and would age out exactly like a grant. The
        // browser that comes back then presents a token naming nothing, is indistinguishable from a
        // new one, and is asked again - which is the single thing its tier promised would not
        // happen. From that person's side the product either forgot or lied, and nothing in the logs
        // connects the dialog to a housekeeping pass six months earlier.
        [Test]
        public async Task PruneStaleAsync_KeepsARefusalTheLicenceMadeFinal_HoweverLongAgoItWas()
        {
            var refused = Consent("final", UsageDataDecision.Declined, Now.AddDays(-400));
            refused.DecidedAt = Now.AddDays(-400);
            await Repository.AddAsync(refused, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180),
                owedNothingSince: Now.AddDays(-90),
                refusalsAreFinal: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.Zero);
                Assert.That(await Repository.FindByTokenHashAsync("final", TestContext.CurrentContext.CancellationToken), Is.Not.Null,
                    "forgetting it is indistinguishable, from outside, from asking again");
            }
        }

        // A withdrawal is not a refusal: it leaves the door open on either tier, so it is owed a
        // question like any other and ages out once that question has fallen due.
        [Test]
        public async Task PruneStaleAsync_ForgetsAWithdrawal_EvenWhereRefusalsAreFinal()
        {
            var withdrawn = Consent("withdrawn", UsageDataDecision.Revoked, Now.AddDays(-400));
            withdrawn.DecidedAt = Now.AddDays(-400);
            await Repository.AddAsync(withdrawn, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180),
                owedNothingSince: Now.AddDays(-90),
                refusalsAreFinal: true);

            Assert.That(removed, Is.EqualTo(1));
        }

        [Test]
        public async Task PruneStaleAsync_ForgetsALongGoneBrowserThatAgreed_WhateverItIsOwed()
        {
            var agreed = Consent("gone", UsageDataDecision.Granted, Now.AddDays(-200));
            agreed.DecidedAt = Now.AddDays(-1);
            await Repository.AddAsync(agreed, TestContext.CurrentContext.CancellationToken);

            var removed = await PruneAsync(
                lastSeenBefore: Now.AddDays(-180), owedNothingSince: Now.AddDays(-90));

            Assert.That(removed, Is.EqualTo(1),
                "nobody is owed a question they already answered yes to, so age is the only thing "
                + "keeping this row - and it has run out");
        }

        private Task<int> PruneAsync(
            DateTime lastSeenBefore,
            DateTime? owedNothingSince = null,
            bool refusalsAreFinal = false)
        {
            // Defaulted so far ahead that nothing is still owed a question, which is what lets a
            // test about ageing be only about ageing. A test that means to exercise the promise
            // passes its own instant.
            return Repository.PruneStaleAsync(
                lastSeenBefore,
                owedNothingSince ?? Now.AddYears(10),
                refusalsAreFinal,
                TestContext.CurrentContext.CancellationToken);
        }

        private static UsageDataConsent Consent(string tokenHash, UsageDataDecision decision, DateTime lastSeenAt)
        {
            return new UsageDataConsent
            {
                TokenHash = tokenHash,
                Decision = decision,
                DecidedAt = lastSeenAt,
                LastSeenAt = lastSeenAt,
            };
        }
    }
}
