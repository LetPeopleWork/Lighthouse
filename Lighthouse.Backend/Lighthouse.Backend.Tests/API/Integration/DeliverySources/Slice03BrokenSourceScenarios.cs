using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance scenarios for a Delivery whose source has stopped answering, driven through the
    /// scheduled refresh. The distinction the whole slice rests on cannot be shown any other way: a
    /// remote that answered "this is gone" and a remote that could not be answered at all reach this
    /// pass by different routes, and only one of them may leave a mark.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-03")]
    public partial class Slice03BrokenSourceTest
    {
        [TestCase(ApiV1Prefix)]
        [TestCase(ApiLatestPrefix)]
        public async Task A_Release_deleted_in_Jira_leaves_the_Delivery_saying_so_and_holding_everything_it_was_given(
            string prefix)
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(prefix);

            GivenTheReleaseHasBeenDeletedInJira();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(prefix, portfolio);

            ThenTheDeliverySaysItsSourceIsFinished(afterTheRefresh, "SourceNotFound");
        }

        /// <summary>
        /// AC-04.6. Two of the three Releases on the demo instance carry no date, so a Release losing
        /// one is ordinary rather than exotic - and it is a different sentence, because the Release is
        /// sitting right there and somebody can put it right in a minute.
        /// </summary>
        [Test]
        public async Task A_Release_whose_date_was_cleared_says_that_rather_than_that_it_is_gone()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);

            GivenTheReleaseLostItsDateInJira();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheDeliverySaysItsSourceIsFinished(afterTheRefresh, "SourceHasNoDate");
        }

        /// <summary>
        /// AC-04.5, and the reason this slice needed the previous one to settle the vocabulary. A
        /// remote that could not be reached has said nothing about whether the Release exists; marking
        /// the Delivery on that evidence turns every bad minute at Jira into a deleted Release, and the
        /// reader has no way to tell the difference.
        /// </summary>
        [Test]
        public async Task A_Release_that_merely_could_not_be_read_leaves_no_mark_on_the_Delivery_at_all()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);
            var beforeTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            GivenTheReleaseCouldNotBeReadThisTime();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                ThenTheDeliverySaysNothingIsWrongWithItsSource(afterTheRefresh);
                ThenNothingAboutTheDeliveryMoved(beforeTheRefresh, afterTheRefresh);
            }
        }

        [Test]
        public async Task A_connection_that_stops_offering_Releases_degrades_the_Delivery_rather_than_erroring()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);

            GivenTheConnectionNoLongerOffersReleases();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                ThenTheDeliverySaysItsSourceIsFinished(afterTheRefresh, "CapabilityWithdrawn");
                ThenTheRefreshWasRecordedAsHavingWorked(portfolio);
            }
        }

        /// <summary>
        /// Somebody restores the Release, or the credential regains sight of the project. Nothing in
        /// testing ever does this and production does it regularly; left flagged, the Delivery would go
        /// on saying its date is unmaintained while the date moves underneath the notice.
        /// </summary>
        [Test]
        public async Task A_Release_that_comes_back_takes_the_notice_off_the_Delivery_again()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);
            GivenTheReleaseHasBeenDeletedInJira();
            await ThePortfolioRefreshRuns(portfolio);

            GivenTheReleaseIsBackAndRescheduled();
            await ThePortfolioRefreshRuns(portfolio);

            var afterItCameBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                ThenTheDeliverySaysNothingIsWrongWithItsSource(afterItCameBack);
                ThenTheDeliverySays(afterItCameBack, TheReleaseName, TheDateTheReleaseHasNow);
            }
        }

        /// <summary>
        /// AC-04.3. The way out has to work from the state the notice is offering it in, and it has to
        /// hand back everything the Release left - that is why somebody releases a Delivery rather than
        /// deleting it.
        /// </summary>
        [Test]
        public async Task Releasing_a_flagged_Delivery_hands_back_its_values_and_takes_the_notice_with_it()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);
            GivenTheReleaseHasBeenDeletedInJira();
            await ThePortfolioRefreshRuns(portfolio);
            var whileFlagged = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            await WhenTheDeliveryIsTakenBackByHand(ApiLatestPrefix, whileFlagged.Id);

            var afterBeingTakenBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack);
        }
    }
}
