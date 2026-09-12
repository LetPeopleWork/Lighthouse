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
        public async Task AnyLiveGrantAsync_WithAGrantInsideTheWindow_IsTrue()
        {
            await Repository.AddAsync(Consent("live", UsageDataDecision.Granted, Now.AddDays(-1)), TestContext.CurrentContext.CancellationToken);

            var anyone = await Repository.AnyLiveGrantAsync(Now.AddDays(-30), TestContext.CurrentContext.CancellationToken);

            Assert.That(anyone, Is.True);
        }

        [Test]
        public async Task AnyLiveGrantAsync_WithAGrantThatHasAgedOut_IsFalse()
        {
            await Repository.AddAsync(Consent("old", UsageDataDecision.Granted, Now.AddDays(-45)), TestContext.CurrentContext.CancellationToken);

            var anyone = await Repository.AnyLiveGrantAsync(Now.AddDays(-30), TestContext.CurrentContext.CancellationToken);

            Assert.That(anyone, Is.False,
                "a browser that stopped visiting stops counting - clearing storage sends nothing, so "
                + "silence is the only signal there is");
        }

        [Test]
        public async Task AnyLiveGrantAsync_WithAGrantExactlyOnTheWindowEdge_IsFalse()
        {
            var edge = Now.AddDays(-30);
            await Repository.AddAsync(Consent("edge", UsageDataDecision.Granted, edge), TestContext.CurrentContext.CancellationToken);

            var anyone = await Repository.AnyLiveGrantAsync(edge, TestContext.CurrentContext.CancellationToken);

            Assert.That(anyone, Is.False,
                "the window is the last thirty days, not the last thirty days and one more moment. "
                + "Somewhere a grant has to stop counting, and the edge is where it does");
        }

        [Test]
        public async Task AnyLiveGrantAsync_WithOnlyRefusalsAndWithdrawals_IsFalse()
        {
            await Repository.AddAsync(Consent("no", UsageDataDecision.Declined, Now), TestContext.CurrentContext.CancellationToken);
            await Repository.AddAsync(Consent("undone", UsageDataDecision.Revoked, Now), TestContext.CurrentContext.CancellationToken);

            var anyone = await Repository.AnyLiveGrantAsync(Now.AddDays(-30), TestContext.CurrentContext.CancellationToken);

            Assert.That(anyone, Is.False);
        }

        [Test]
        public async Task PruneStaleAsync_RemovesWhatHasAgedOut_AndKeepsWhatHasNot()
        {
            await Repository.AddAsync(Consent("ancient", UsageDataDecision.Granted, Now.AddDays(-100)), TestContext.CurrentContext.CancellationToken);
            await Repository.AddAsync(Consent("recent", UsageDataDecision.Granted, Now.AddDays(-1)), TestContext.CurrentContext.CancellationToken);

            var removed = await Repository.PruneStaleAsync(Now.AddDays(-30), TestContext.CurrentContext.CancellationToken);

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

            var removed = await Repository.PruneStaleAsync(edge, TestContext.CurrentContext.CancellationToken);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.Zero);
                Assert.That(await Repository.FindByTokenHashAsync("edge", TestContext.CurrentContext.CancellationToken), Is.Not.Null,
                    "pruning and counting have to agree about the edge, or a row stops counting as "
                    + "live on one query and is still there for the other");
            }
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
