using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// What a Delivery says when the Release it follows stops answering for good. The values it is
    /// showing are kept exactly as the Release last set them - they are the reason the Delivery is
    /// still worth reading - and what changes is only that it now says where they came from and that
    /// nothing is maintaining them any more.
    ///
    /// The distinction this rests on is between a source that resolved to nothing and a source that
    /// could not be reached. Only the first may reach the aggregate: a Delivery flagged over a network
    /// blip would tell an operator a Release had been deleted when it is sitting there.
    /// </summary>
    public class DeliverySourceUnavailableInvariantTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string ReleaseName = "2026 Q4";
        private const int TheDeliveryOnScreen = 4711;

        private static readonly DateTime ReleaseDate = TestToday.AFutureDate;

        private static readonly DateTime HeardFromTheReleaseAt = TestToday.AmbientAsUtcMidnight;

        private static readonly string[] FeaturesTheReleaseNames = ["Checkout", "Search"];

        [TestCaseSource(nameof(EveryReasonASourceIsFinished))]
        public void A_Delivery_whose_source_is_finished_says_so_and_keeps_everything_the_source_gave_it(
            DeliverySourceUnavailableReason reason)
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();

            delivery.MarkSourceUnavailable(reason);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(reason));
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(FeaturesTheReleaseNames));
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound),
                    "nothing unbinds on its own - the way off is a person asking for it.");
                Assert.That(delivery.SourceReference, Is.EqualTo(ReleaseId));
            }
        }

        /// <summary>
        /// The date it last heard from the source is what the screen shows beside the notice, so it has
        /// to survive the source going away. Cleared, the Delivery would say its values are stale
        /// without being able to say since when.
        /// </summary>
        [Test]
        public void A_Delivery_whose_source_is_finished_still_says_when_it_last_heard_from_it()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();

            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(HeardFromTheReleaseAt));
        }

        private static IEnumerable<TestCaseData> EveryReasonASourceIsFinished()
        {
            yield return new TestCaseData(DeliverySourceUnavailableReason.SourceNotFound)
                .SetName("The Release was deleted");
            yield return new TestCaseData(DeliverySourceUnavailableReason.SourceHasNoDate)
                .SetName("The Release lost its date");
            yield return new TestCaseData(DeliverySourceUnavailableReason.CapabilityWithdrawn)
                .SetName("The connection stopped offering Releases");
        }

        /// <summary>
        /// The one reason that may never be written down. It means today's attempt told us nothing, and
        /// a Delivery carrying it would tell an operator a Release is finished on the evidence of a
        /// network blip - which is the whole failure this vocabulary exists to prevent.
        /// </summary>
        [Test]
        public void A_source_that_merely_could_not_be_reached_is_refused_as_a_reason_to_flag_anything()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();

            Assert.Throws<ArgumentException>(() =>
                delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceReadFailed));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        /// <summary>
        /// A Release that is still gone on the next refresh is not news. Saying so again would move the
        /// version an open browser is holding on every refresh interval, for a state that has not
        /// changed - and the Delivery is one somebody is likely to have open, because it is the one
        /// telling them something is wrong.
        /// </summary>
        [Test]
        public void Saying_a_source_is_finished_twice_leaves_the_version_an_open_browser_holds_alone()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionAnOpenEditorHolds));
        }

        [Test]
        public void A_source_that_is_finished_for_a_different_reason_than_before_says_the_new_one()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceHasNoDate);
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.SourceNotFound));
                Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(versionAnOpenEditorHolds));
            }
        }

        /// <summary>
        /// A Release that comes back is the case that never happens in testing and always happens in
        /// production: somebody restores it, or the credential regains sight of the project. Left
        /// flagged, the Delivery would go on saying its date is unmaintained while the date moves
        /// underneath the notice.
        /// </summary>
        [Test]
        public void A_source_that_answers_again_stops_the_Delivery_saying_it_is_finished()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            delivery.SyncFromSource(ReleaseName, ReleaseDate, delivery.Features.ToList(), HeardFromTheReleaseAt);

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        /// <summary>
        /// Clearing the notice is a change even when the values it sat beside are identical, because
        /// what the screen says about them is what changed.
        /// </summary>
        [Test]
        public void A_source_answering_again_is_a_change_even_when_it_says_exactly_what_it_said_before()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            delivery.SyncFromSource(ReleaseName, ReleaseDate, delivery.Features.ToList(), HeardFromTheReleaseAt);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(versionAnOpenEditorHolds));
        }

        [Test]
        public void A_Delivery_that_follows_no_source_cannot_be_told_its_source_is_finished()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.Id = TheDeliveryOnScreen;

            var refusal = Assert.Throws<DeliverySourceBoundException>(() =>
                delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.Code, Is.EqualTo("delivery-not-source-bound"));
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
            }
        }

        [Test]
        public void A_retired_Delivery_is_not_told_anything_about_the_source_it_used_to_follow()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.Archive(TestToday.AmbientAsUtcMidnight);

            Assert.Throws<DeliveryArchivedException>(() =>
                delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound));

            Assert.That(delivery.SourceUnavailableReason, Is.Null);
        }

        /// <summary>
        /// Releasing a flagged Delivery is the way out the notice offers, so it has to leave nothing of
        /// the notice behind - a Manual Delivery still carrying a reason its source is finished would
        /// say a source it no longer has is broken.
        /// </summary>
        [Test]
        public void Releasing_a_flagged_Delivery_takes_the_notice_with_it()
        {
            var delivery = ADeliveryThatHasHeardFromItsRelease();
            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            delivery.Unbind();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(FeaturesTheReleaseNames));
            }
        }

        private static Delivery ADeliveryChosenByHand()
        {
            var delivery = new Delivery(ReleaseName, ReleaseDate, 1);
            delivery.ReplaceFeatures(FeaturesTheReleaseNames.Select((name, index) => new Feature { Id = index + 1, Name = name }).ToList());

            return delivery;
        }

        private static Delivery ADeliveryThatHasHeardFromItsRelease()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(ReleaseSourceKey, ReleaseId);
            delivery.SyncFromSource(ReleaseName, ReleaseDate, delivery.Features.ToList(), HeardFromTheReleaseAt);

            return delivery;
        }
    }
}
