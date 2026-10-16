using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.Staleness
{
    public partial class Slice01ServerMarksStaleTest
    {
        // --- Given ---

        private int GivenATeamThatCallsWorkStaleAfterFiveDaysInAStateOrThreeDaysBlocked()
            => SeedTeam(new StalenessThresholds(TimeInStateDays: 5, BlockedDays: 3));

        private int GivenATeamWithThresholds(StalenessThresholds thresholds) => SeedTeam(thresholds);

        private int GivenAPortfolioWithThresholds(StalenessThresholds thresholds) => SeedPortfolio(thresholds);

        private int GivenAWorkItemHasBeenInItsStateFor(int team, string referenceId, string state, int days)
            => SeedWorkItem(team, referenceId, state, EnteredDaysBefore(days));

        private void GivenAWorkItemWithNoRecordOfWhenItEnteredItsState(int team, string referenceId)
            => SeedWorkItem(team, referenceId, Review, enteredStateAt: null);

        private void GivenTheWorkItemHasBeenBlockedFor(int workItem, int days)
            => SeedBlockedSince(workItem, EnteredDaysBefore(days));

        private int GivenAFeatureHasBeenInItsStateFor(int portfolio, string referenceId, string state, int days)
            => SeedFeature(portfolio, referenceId, state, EnteredDaysBefore(days));

        private void GivenTheFeatureHasBeenBlockedInThePortfolioFor(int feature, int portfolio, int days)
            => SeedFeatureBlockedSince(feature, portfolio, EnteredDaysBefore(days));

        // --- When ---

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsRead(int team) => ReadTeamWip(team, AsOf);

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsReadAsOf(int team, DateTime asOf) => ReadTeamWip(team, asOf);

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsReadWithoutADay(int team) => ReadTeamWipWithoutADate(team);

        private Task<ReadAnswer> WhenTheTeamsBlockedItemsAreReadForTheDay(int team) => ReadTeamBlockedItemsOn(team, AsOf);

        private Task<ReadAnswer> WhenThePortfoliosWorkInProgressIsRead(int portfolio) => ReadPortfolioWip(portfolio, AsOf);

        // --- Then ---

        private static void ThenTheWorkItemIsStaleBecause(ReadAnswer read, string referenceId, params ReasonSeen[] reasons)
            => ThenTheWorkItemReads(read, referenceId, expectedStale: true, [.. reasons]);

        private static void ThenTheWorkItemIsNotStale(ReadAnswer read, string referenceId)
            => ThenTheWorkItemReads(read, referenceId, expectedStale: false, []);

        private static void ThenTheWorkItemReads(ReadAnswer read, string referenceId, bool expectedStale, List<ReasonSeen> expectedReasons)
        {
            Assert.That(read.Status, Is.EqualTo(HttpStatusCode.OK), read.Body);

            var seen = StalenessOf(read, referenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.IsStale, Is.EqualTo(expectedStale),
                    $"Lighthouse has to say whether '{referenceId}' is stale on this read. Body: {read.Body}");
                Assert.That(seen.Reasons, Is.EqualTo(expectedReasons),
                    $"The reasons are what the dashboard tooltip, `lh` and the skill repeat to the coach. Body: {read.Body}");
            }
        }

        private static void ThenTheReadSaysNothingAboutStaleness(ReadAnswer read, string referenceId)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Status, Is.EqualTo(HttpStatusCode.OK), read.Body);
                Assert.That(StalenessOf(read, referenceId).HasAnAnswer, Is.False,
                    $"A read that does not judge staleness must leave it out: a 'false' there would tell a client the item is not stale. Body: {read.Body}");
            }
        }

        private static void ThenTheReadHasNoWorkItems(ReadAnswer read)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.Status, Is.EqualTo(HttpStatusCode.OK), read.Body);
                Assert.That(ItemCount(read), Is.Zero, read.Body);
            }
        }
    }
}
