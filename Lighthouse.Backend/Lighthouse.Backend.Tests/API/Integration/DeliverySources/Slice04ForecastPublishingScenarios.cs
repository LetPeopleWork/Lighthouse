using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance scenarios for broadcasting a Delivery's forecast onto the Release it follows. The
    /// switch is set the way a person sets it - over HTTP - and then nobody touches anything again:
    /// everything after that is the scheduled refresh, which is the only thing that can show that the
    /// broadcast happens on the run that produced the forecast rather than because a test asked for it.
    ///
    /// What reaches Jira is asserted at the connector, because the port is where the Epic's promise
    /// stops being ours: past it, a Release in somebody else's Jira has been written to.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-04")]
    public partial class Slice04ForecastPublishingTest
    {
        [TestCase(ApiV1Prefix)]
        [TestCase(ApiLatestPrefix)]
        public async Task A_Portfolio_owner_switches_publishing_on_and_the_next_refresh_puts_the_forecast_on_the_Release(string prefix)
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(prefix);
            var delivery = await TheOnlyDeliveryOf(prefix, portfolioId);

            await ThePortfolioRefreshRuns(portfolioId);

            ThenTheDeliverySaysItBroadcasts(delivery);
            ThenTheReleaseWasWrittenTo(TheRelease);
            ThenWhatReachedJiraCarriesEverythingTheBlockMustSay();
        }

        [Test]
        public async Task A_Delivery_nobody_switched_on_leaves_its_Release_alone()
        {
            var portfolioId = await GivenADeliveryFollowingTheReleaseQuietly(ApiLatestPrefix);
            var delivery = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            await ThePortfolioRefreshRuns(portfolioId);

            ThenTheDeliverySaysItDoesNotBroadcast(delivery);
            ThenNoReleaseWasWrittenTo();
        }

        /// <summary>
        /// A retired Delivery's numbers are what was true on the day it closed. Left broadcasting, it
        /// would push that frozen forecast into a live Release for as long as the Release exists.
        /// </summary>
        [Test]
        public async Task A_Delivery_that_has_been_retired_stops_broadcasting()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            var delivery = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);
            await GivenTheDeliveryHasBeenRetired(delivery.Id);

            await ThePortfolioRefreshRuns(portfolioId);

            ThenNoReleaseWasWrittenTo();
        }

        /// <summary>
        /// Letting go of the Release has to take the broadcast with it. Left standing, it would come
        /// back on by itself the moment the Delivery was pointed at a second Release - publishing to a
        /// Release nobody chose to publish to.
        /// </summary>
        [Test]
        public async Task Letting_go_of_the_Release_stops_the_broadcast_and_the_next_refresh_writes_nothing()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            var broadcasting = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            var takenBack = await WhenTheDeliveryIsTakenBackByHand(ApiLatestPrefix, broadcasting.Id);
            var afterBeingTakenBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);
            await ThePortfolioRefreshRuns(portfolioId);

            ThenTheAnswerIs(takenBack, HttpStatusCode.OK);
            ThenTheDeliverySaysItDoesNotBroadcast(afterBeingTakenBack);
            ThenNoReleaseWasWrittenTo();
        }

        /// <summary>
        /// The switch is a property of the binding, so a Delivery that follows nothing cannot carry it
        /// however insistently a payload asks. Answered rather than refused, because the same payload
        /// shape serves all three ways of choosing Features and the field simply does not apply here.
        /// </summary>
        [Test]
        public async Task A_Delivery_chosen_by_hand_cannot_be_made_to_broadcast_by_asking()
        {
            var portfolioId = await GivenADeliveryChosenByHandThatAsksToBroadcast(ApiLatestPrefix);

            var delivery = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);
            await ThePortfolioRefreshRuns(portfolioId);

            ThenTheDeliverySaysItDoesNotBroadcast(delivery);
            ThenNoReleaseWasWrittenTo();
        }

        /// <summary>
        /// The switch is the one field on this payload nobody sees being changed. Every other one shows
        /// on the screen the moment it moves; this one only shows as a Release somewhere else that
        /// quietly stopped being updated, with nothing anywhere saying why - so a payload that does not
        /// mention it leaves it alone rather than reading as "switch it off".
        /// </summary>
        [Test]
        public async Task A_payload_that_never_mentions_the_switch_leaves_it_where_it_was()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            var broadcasting = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            var saved = await WhenTheDeliveryIsSavedByAClientThatKnowsNothingAboutBroadcasting(
                ApiLatestPrefix, broadcasting.Id);
            var afterTheSave = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheAnswerIs(saved, HttpStatusCode.OK);
            ThenTheDeliverySaysItBroadcasts(afterTheSave);
        }

        /// <summary>
        /// A Jira that will not take the write is an exception report, not a fault: the refresh that
        /// produced the forecast carries every other number on the Portfolio.
        /// </summary>
        [Test]
        public async Task A_Jira_that_refuses_the_write_does_not_cost_the_refresh_anything()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenJiraRefusesToBeWrittenTo();

            await ThePortfolioRefreshRuns(portfolioId);
            var afterTheRefusal = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheRefreshWasRecordedAsHavingWorked(portfolioId);
            ThenTheDeliveryStillFollowsALiveRelease(afterTheRefusal);
        }

        /// <summary>
        /// The report the slice exists for. An administrator who switched the broadcast on and saw
        /// nothing appear in Jira reads why, in Jira's own words, on the Delivery they switched on.
        /// </summary>
        [Test]
        public async Task A_refused_write_is_reported_on_the_Delivery_in_the_words_Jira_used()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenJiraRefusesToBeWrittenTo();

            await ThePortfolioRefreshRuns(portfolioId);
            var afterTheRefusal = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheDeliveryReportsTheRefusal(afterTheRefusal);
        }

        /// <summary>
        /// AC-06.4, and the criterion that carries the slice. Reading Releases and writing to them are
        /// separate capabilities: a refused write must never stop the date sync, or an optional outbound
        /// feature could take the inbound half of the Epic down with it.
        /// </summary>
        [Test]
        public async Task A_refused_write_leaves_the_Release_date_syncing()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenJiraRefusesToBeWrittenTo();
            GivenTheReleaseHasBeenRescheduledInJira();

            await ThePortfolioRefreshRuns(portfolioId);
            var afterTheRefusal = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheDeliveryTookTheNewDateAnyway(afterTheRefusal);
            ThenTheDeliveryReportsTheRefusal(afterTheRefusal);
        }

        /// <summary>
        /// The permission was granted, or the description shortened. Either way the report has to go, or
        /// a Delivery publishing perfectly well goes on asking somebody to fix something that works.
        /// </summary>
        [Test]
        public async Task A_write_that_goes_through_afterwards_takes_the_report_off()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenJiraRefusesToBeWrittenTo();
            await ThePortfolioRefreshRuns(portfolioId);

            GivenJiraTakesWhateverItIsSent();
            await ThePortfolioRefreshRuns(portfolioId);
            var afterItWorked = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheDeliveryReportsNoRefusal(afterItWorked);
        }

        [Test]
        public async Task A_refused_write_does_not_switch_the_broadcast_off_or_stop_it_being_tried_again()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenJiraRefusesToBeWrittenTo();

            await ThePortfolioRefreshRuns(portfolioId);
            var afterTheFirstRefusal = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);
            GivenNothingHasReachedJiraYet();
            await ThePortfolioRefreshRuns(portfolioId);

            ThenTheDeliverySaysItBroadcasts(afterTheFirstRefusal);
            ThenTheReleaseWasWrittenTo(TheRelease);
            ThenItWasTriedExactlyOnce();
        }

        /// <summary>
        /// The Release was deleted between the read that resolved it and the write. That is the same
        /// fact about the same Release a failed read reports, so it puts the Delivery into the same
        /// state - and the reader is told on the screen rather than left with a Delivery that silently
        /// stopped being written to.
        /// </summary>
        [Test]
        public async Task A_Release_that_is_gone_by_the_time_the_forecast_is_written_is_said_to_be_gone()
        {
            var portfolioId = await GivenADeliveryBroadcastingToTheRelease(ApiLatestPrefix);
            GivenTheReleaseIsNoLongerThereToBeWrittenTo();

            await ThePortfolioRefreshRuns(portfolioId);
            var afterTheWrite = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolioId);

            ThenTheDeliverySaysItsReleaseIsGone(afterTheWrite);
        }
    }
}
