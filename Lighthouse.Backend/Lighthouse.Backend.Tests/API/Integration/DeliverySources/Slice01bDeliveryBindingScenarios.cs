using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.DeliverySources
{
    /// <summary>
    /// Acceptance scenarios for a Delivery that follows a Release, driven over HTTP. The refusals and
    /// the release pull in opposite directions through one route - a bound Delivery refuses every hand
    /// write, and stopping following the Release is itself a hand write - so the order in which the
    /// route reads the mode and applies the payload decides whether both can hold. Nothing but a real
    /// request through the real pipeline can show that order.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5565-delivery-date-sync")]
    [Category("slice-01b")]
    public partial class Slice01bDeliveryBindingTest
    {
        [TestCase(ApiV1Prefix)]
        [TestCase(ApiLatestPrefix)]
        public async Task A_Portfolio_owner_creates_a_Delivery_from_a_Release_cannot_edit_what_Jira_owns_and_gets_it_all_back_on_unbind(
            string prefix)
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();

            var created = await WhenTheDeliveryIsCreatedFromTheRelease(prefix, portfolio);
            var bound = await TheOnlyDeliveryOf(prefix, portfolio);

            var handEdit = await WhenTheDeliveryIsEditedByHandWhileStillFollowingTheRelease(prefix, bound.Id, HandEdit.Rename);
            var afterTheHandEdit = await TheOnlyDeliveryOf(prefix, portfolio);

            var takenBack = await WhenTheDeliveryIsTakenBackByHand(prefix, bound.Id, ADateSomebodyPicked);
            var afterBeingTakenBack = await TheOnlyDeliveryOf(prefix, portfolio);

            ThenTheAnswerIs(created, HttpStatusCode.OK);
            ThenTheDeliverySaysWhatTheReleaseSays(bound, TheReleaseDate);
            ThenTheAnswerIs(handEdit, HttpStatusCode.OK);
            ThenTheDeliverySaysWhatTheReleaseSays(afterTheHandEdit, TheReleaseDate);
            ThenTheAnswerIs(takenBack, HttpStatusCode.OK);
            ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack, TheReleaseDate);
        }

        [TestCaseSource(nameof(EveryHandEditToWhatTheReleaseOwns))]
        public async Task A_Delivery_that_follows_a_Release_wears_none_of_what_a_payload_asks_of_it(HandEdit edit)
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();
            await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var bound = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            var handEdit = await WhenTheDeliveryIsEditedByHandWhileStillFollowingTheRelease(ApiLatestPrefix, bound.Id, edit);
            var afterTheHandEdit = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(handEdit, HttpStatusCode.OK);
            ThenTheDeliverySaysWhatTheReleaseSays(afterTheHandEdit, TheReleaseDate);
        }

        /// <summary>
        /// A Delivery is released from a Release that shipped long ago exactly as often as from one
        /// still ahead - more often, in fact, because a Release that has come and gone is when somebody
        /// stops following it. A future-date check on this path would make the past-dated ones the only
        /// Deliveries nobody could ever take back.
        /// </summary>
        [Test]
        public async Task A_Delivery_following_a_Release_that_has_already_shipped_can_still_be_taken_back_by_hand()
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();
            GivenTheReleaseShippedLongAgo();
            await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var bound = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            // The date in the payload is the one on the screen, because that is what the form has to
            // send back: the Delivery is showing the Release's date, and the Release shipped in 2024.
            var takenBack = await WhenTheDeliveryIsTakenBackByHand(
                ApiLatestPrefix, bound.Id, AReleaseDateThatHasAlreadyPassed);
            var afterBeingTakenBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(takenBack, HttpStatusCode.OK);
            ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack, AReleaseDateThatHasAlreadyPassed);
        }

        [Test]
        public async Task Somebody_who_may_only_read_a_Portfolio_may_neither_create_a_bound_Delivery_nor_change_one()
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();
            await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var bound = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            GivenTheCallerMayOnlyReadThisPortfolio(portfolio);
            var refusedCreate = await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var refusedUpdate = await WhenTheDeliveryIsTakenBackByHand(ApiLatestPrefix, bound.Id, ADateSomebodyPicked);

            GivenTheCallerMayChangeThisPortfolio(portfolio);
            var allowedUpdate = await WhenTheDeliveryIsTakenBackByHand(ApiLatestPrefix, bound.Id, ADateSomebodyPicked);
            var afterBeingTakenBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(refusedCreate, HttpStatusCode.Forbidden);
            ThenTheAnswerIs(refusedUpdate, HttpStatusCode.Forbidden);
            ThenTheAnswerIs(allowedUpdate, HttpStatusCode.OK);
            ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack, TheReleaseDate);
        }

        /// <summary>
        /// The free-tier cap counts the Deliveries a Portfolio already holds, so it lets the first one
        /// through - which on an empty Portfolio is every unlicensed instance's first attempt at
        /// following a Release. Following one is premium in its own right, and this is the request that
        /// tells the two rules apart.
        /// </summary>
        [Test]
        public async Task An_unlicensed_instance_may_still_make_its_first_Delivery_by_hand_but_not_from_a_Release()
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();
            GivenTheInstanceIsNotLicensed();

            var refusedCreate = await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var deliveriesAfterTheRefusal = await TheNumberOfDeliveriesShownFor(ApiLatestPrefix, portfolio);
            var allowedCreate = await WhenTheDeliveryIsCreatedByHand(ApiLatestPrefix, portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusedCreate.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(deliveriesAfterTheRefusal, Is.Zero);
                Assert.That(allowedCreate.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        /// <summary>
        /// A licence that lapses must not strand a Delivery mid-binding: pointing it at the Release
        /// again is refused, and taking it back by hand stays open, so the way out is never closed.
        /// </summary>
        [Test]
        public async Task A_lapsed_licence_refuses_to_follow_a_Release_and_still_lets_the_Delivery_be_taken_back()
        {
            var portfolio = GivenAJiraPortfolioWhoseReleaseCarriesWorkItTracks();
            await WhenTheDeliveryIsCreatedFromTheRelease(ApiLatestPrefix, portfolio);
            var bound = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);
            GivenTheInstanceIsNotLicensed();

            var refusedUpdate = await WhenTheDeliveryIsPointedAtTheReleaseAgain(ApiLatestPrefix, bound.Id);
            var afterTheRefusal = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);
            var takenBack = await WhenTheDeliveryIsTakenBackByHand(ApiLatestPrefix, bound.Id, ADateSomebodyPicked);
            var afterBeingTakenBack = await TheOnlyDeliveryOf(ApiLatestPrefix, portfolio);

            ThenTheAnswerIs(refusedUpdate, HttpStatusCode.Forbidden);
            ThenTheDeliverySaysWhatTheReleaseSays(afterTheRefusal, TheReleaseDate);
            ThenTheAnswerIs(takenBack, HttpStatusCode.OK);
            ThenTheDeliveryIsItsOwnAgainCarryingWhatTheReleaseLeft(afterBeingTakenBack, TheReleaseDate);
        }
    }
}
