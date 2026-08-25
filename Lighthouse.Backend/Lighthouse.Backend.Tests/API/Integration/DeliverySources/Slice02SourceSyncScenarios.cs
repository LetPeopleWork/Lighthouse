using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance scenarios for a Delivery keeping step with its Release, driven through the scheduled
    /// refresh rather than through the sync service. Nothing else can show what this slice is for: the
    /// promise is that a date moving in Jira reaches the screen with nobody asking it to, and the only
    /// evidence of that is the refresh - the one thing that does run with nobody asking - carrying it
    /// end to end, through the resolver, the aggregate, EF and the read the grid uses.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-02")]
    public partial class Slice02SourceSyncTest
    {
        [TestCase(ApiV1Prefix)]
        [TestCase(ApiLatestPrefix)]
        public async Task A_Release_that_is_renamed_and_rescheduled_in_Jira_reaches_the_Delivery_on_the_next_refresh(
            string prefix)
        {
            var portfolio = await GivenADeliveryFollowingTheRelease(prefix);

            GivenTheReleaseHasBeenRenamedAndRescheduledInJira();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(prefix, portfolio);

            ThenTheDeliverySays(afterTheRefresh, TheNameTheReleaseHasNow, TheDateTheReleaseHasNow);
        }

        /// <summary>
        /// The invariant the constructor used to carry, seen from the outside: a Release that slipped
        /// past its own date is an ordinary state, and refusing its date here would leave the Delivery
        /// showing a date Jira no longer holds - the exact disagreement this Epic exists to remove.
        /// </summary>
        [Test]
        public async Task A_Release_whose_date_has_slipped_into_the_past_still_reaches_the_Delivery()
        {
            var portfolio = await GivenADeliveryFollowingTheRelease(ApiLatestPrefix);

            GivenTheReleaseNowCarries(TheReleaseName, ADateThatHasBeenAndGone);
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheDeliverySays(afterTheRefresh, TheReleaseName, ADateThatHasBeenAndGone);
        }

        /// <summary>
        /// A refresh that changed nothing must leave the version an open browser is holding where it
        /// was. Moving it would fail that browser's next save with "somebody else changed this" on
        /// every refresh interval, for nobody's edit.
        /// </summary>
        [Test]
        public async Task A_refresh_that_found_the_Release_unchanged_leaves_the_Delivery_and_its_version_alone()
        {
            var portfolio = await GivenADeliveryFollowingTheRelease(ApiLatestPrefix);
            var beforeTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenNothingAboutTheDeliveryMoved(beforeTheRefresh, afterTheRefresh);
        }

        [Test]
        public async Task A_Release_nobody_could_read_leaves_the_Delivery_on_its_last_known_values_and_the_refresh_standing()
        {
            var portfolio = await GivenADeliveryFollowingTheRelease(ApiLatestPrefix);
            var beforeTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            GivenJiraCannotBeAskedAboutTheReleaseAtAll();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                ThenNothingAboutTheDeliveryMoved(beforeTheRefresh, afterTheRefresh);
                ThenTheRefreshWasRecordedAsHavingWorked(portfolio);
            }
        }

        /// <summary>
        /// #5698 pins a closure snapshot on a retired Delivery, so a refresh writing to one would
        /// un-pin a record the product promises does not move again.
        /// </summary>
        [Test]
        public async Task A_retired_Delivery_is_left_where_it_was_however_far_its_Release_has_moved()
        {
            var portfolio = await GivenADeliveryFollowingTheRelease(ApiLatestPrefix);
            var beforeItWasRetired = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);
            await GivenTheDeliveryHasBeenRetired(beforeItWasRetired.Id);

            GivenTheReleaseHasBeenRenamedAndRescheduledInJira();
            await ThePortfolioRefreshRuns(portfolio);

            ThenTheRetiredDeliveryStillSays(beforeItWasRetired.Id, TheReleaseName, TheReleaseDate);
        }

        /// <summary>
        /// A Delivery somebody maintains by hand has an owner editing it, and a refresh that reached
        /// one would overwrite their work on a cadence nobody set.
        /// </summary>
        [Test]
        public async Task A_Delivery_nobody_bound_to_anything_is_untouched_by_the_refresh()
        {
            var portfolio = await GivenADeliveryNobodyBoundToAnything(ApiLatestPrefix);
            var beforeTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            GivenTheReleaseHasBeenRenamedAndRescheduledInJira();
            await ThePortfolioRefreshRuns(portfolio);

            var afterTheRefresh = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenNothingAboutTheDeliveryMoved(beforeTheRefresh, afterTheRefresh);
        }
    }
}
