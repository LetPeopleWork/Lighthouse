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
        /// AC-04.2 for the Delivery that has never been through a refresh. Binding reads the source
        /// successfully, so this Delivery HAS heard from it — and a Release deleted an hour later would
        /// otherwise leave a notice saying it had never been read, on values the source really did give
        /// it that morning.
        /// </summary>
        [Test]
        public async Task A_Delivery_bound_today_can_say_when_it_last_heard_from_its_source_before_any_refresh()
        {
            var portfolio = await GivenADeliveryJustBoundToItsRelease(ApiLatestPrefix);

            var asItWasCreated = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            Assert.That(asItWasCreated.SourceLastSyncedOn, Is.Not.Null,
                "binding resolved the source, so the Delivery has heard from it and can say when.");
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
        /// A Release that is still gone on the next refresh is not news. This is the end-to-end
        /// backstop for that: the version an open browser is holding must survive a refresh that
        /// learned nothing, or the person reading the notice - who is the likeliest of anyone to have
        /// this Delivery open - gets "somebody else changed this" on their next save, every interval.
        /// </summary>
        [Test]
        public async Task A_Release_that_is_still_gone_on_the_next_refresh_says_nothing_new_about_it()
        {
            var portfolio = await GivenADeliveryThatHasHeardFromItsRelease(ApiLatestPrefix);
            GivenTheReleaseHasBeenDeletedInJira();
            await ThePortfolioRefreshRuns(portfolio);
            var afterItWasFirstReported = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            await ThePortfolioRefreshRuns(portfolio);

            var afterTheSecondRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                ThenTheDeliverySaysItsSourceIsFinished(afterItWasFirstReported, "SourceNotFound");
                ThenNothingAboutTheDeliveryMoved(afterItWasFirstReported, afterTheSecondRefresh);
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
            var whileItWasGone = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            GivenTheReleaseIsBackAndRescheduled();
            await ThePortfolioRefreshRuns(portfolio);

            var afterItCameBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                // Without this the scenario reduces to "a sync works", which earlier slices already
                // cover - it would pass just as well with the whole broken-source pass deleted.
                ThenTheDeliverySaysItsSourceIsFinished(whileItWasGone, "SourceNotFound");
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

            using (Assert.EnterMultipleScope())
            {
                // The state has to be the flagged one for "released FROM it" to mean anything; without
                // this the scenario is just the unbind that slice 01b already covers.
                ThenTheDeliverySaysItsSourceIsFinished(whileFlagged, "SourceNotFound");
                ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack);
            }
        }
    }
}
