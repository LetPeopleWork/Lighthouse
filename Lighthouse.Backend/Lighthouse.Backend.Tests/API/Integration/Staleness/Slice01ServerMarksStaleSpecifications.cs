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

        private void GivenAWorkItemHasBeenInItsStateUntilTodayFor(int team, string referenceId, string state, int days)
            => SeedWorkItem(team, referenceId, state, EnteredDaysBefore(days, InstanceToday));

        /// <summary>
        /// A Work Item that had sat in <paramref name="state"/> for <paramref name="days"/> days on the day
        /// asked about, then moved to <paramref name="movedTo"/> the day after - where it is stored now.
        /// </summary>
        private void GivenAWorkItemThatHadBeenInAStateForDaysOnTheDayAskedAboutAndMovedOnTheDayAfter(int team, string referenceId, string state, int days, string movedTo)
        {
            var enteredAt = EnteredDaysBefore(days);
            var movedOnAt = NoonOn(AsOfDay.AddDays(1));

            var item = SeedWorkItem(team, referenceId, movedTo, movedOnAt);
            SeedStateTransition(item, "New", state, enteredAt);
            SeedStateTransition(item, state, movedTo, movedOnAt);
        }

        private void GivenAWorkItemWithNoRecordOfWhenItEnteredItsState(int team, string referenceId)
            => SeedWorkItem(team, referenceId, Review, enteredStateAt: null);

        private void GivenTheWorkItemHasBeenBlockedFor(int workItem, int days)
            => SeedBlockedSince(workItem, EnteredDaysBefore(days));

        private int GivenAFeatureHasBeenInItsStateFor(int portfolio, string referenceId, string state, int days)
            => SeedFeature(portfolio, referenceId, state, EnteredDaysBefore(days));

        private void GivenTheFeatureHasBeenBlockedInThePortfolioFor(int feature, int portfolio, int days)
            => SeedFeatureBlockedSince(feature, portfolio, EnteredDaysBefore(days));

        private int GivenAFeatureHasBeenInItsStateUntilTodayFor(int portfolio, string referenceId, string state, int days)
            => SeedFeature(portfolio, referenceId, state, EnteredDaysBefore(days, InstanceToday));

        private void GivenTheFeatureHasBeenBlockedInThePortfolioUntilTodayFor(int feature, int portfolio, int days)
            => SeedFeatureBlockedSince(feature, portfolio, EnteredDaysBefore(days, InstanceToday));

        private (List<StoredWorkItem> WorkItems, List<StoredBlockedSpell> BlockedSpells) GivenWhatLighthouseHasStored()
            => (TheStoredWorkItems(), TheStoredBlockedSpells());

        // --- When ---

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsRead(int team) => ReadTeamWip(team, AsOf);

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsReadAsOf(int team, DateTime asOf) => ReadTeamWip(team, asOf);

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsReadWithoutADay(int team) => ReadTeamWipWithoutADate(team);

        private Task<ReadAnswer> WhenTheTeamsBlockedItemsAreReadForTheDay(int team) => ReadTeamBlockedItemsOn(team, AsOf);

        private Task<ReadAnswer> WhenThePortfoliosWorkInProgressIsRead(int portfolio) => ReadPortfolioWip(portfolio, AsOf);

        private Task<ReadAnswer> WhenTheTeamsWorkInProgressIsReadForToday(int team) => ReadTeamWip(team, TodayAtNoon);

        private Task<ReadAnswer> WhenThePortfoliosWorkInProgressIsReadForToday(int portfolio) => ReadPortfolioWip(portfolio, TodayAtNoon);

        // --- Then ---

        private static void ThenTheWorkItemIsStaleBecause(ReadAnswer read, string referenceId, params ReasonSeen[] reasons)
            => ThenTheWorkItemReads(read, referenceId, expectedStale: true, [.. reasons]);

        private static void ThenTheWorkItemIsNotStale(ReadAnswer read, string referenceId)
            => ThenTheWorkItemReads(read, referenceId, expectedStale: false, []);

        private static void ThenTheWorkItemReads(ReadAnswer read, string referenceId, bool expectedStale, List<ReasonSeen> expectedReasons)
        {
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
            => Assert.That(StalenessOf(read, referenceId).HasAnAnswer, Is.False,
                $"A read that does not judge staleness must leave it out: a 'false' there would tell a client the item is not stale. Body: {read.Body}");

        private void ThenTheReadLeftWhatLighthouseHasStoredUnchanged((List<StoredWorkItem> WorkItems, List<StoredBlockedSpell> BlockedSpells) before)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(before.WorkItems, Is.Not.Empty, "positive control: there has to be something stored for the read to leave alone.");
                Assert.That(TheStoredWorkItems(), Is.EqualTo(before.WorkItems),
                    "A read only answers: the stored state, when it was entered and the stale flag a refresh keeps must stay as they were.");
                Assert.That(TheStoredBlockedSpells(), Is.EqualTo(before.BlockedSpells),
                    "A read must not open, close or move a blocked spell.");
            }
        }

        private static void ThenTheReadHasNoWorkItems(ReadAnswer read)
            => Assert.That(ItemCount(read), Is.Zero, read.Body);
    }
}
