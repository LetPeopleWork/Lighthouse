using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// What the Release is allowed to write onto the Delivery that follows it, which is exactly the
    /// three things a hand is refused. The refresh runs with nobody watching, so the two things that
    /// matter beyond the values themselves are that a refresh which found nothing new leaves the
    /// version an open browser is holding alone, and that a Delivery somebody retired is never
    /// written to at all.
    /// </summary>
    public class DeliverySourceSyncInvariantTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string ReleaseName = "2026 Q4";
        private const string TheNameTheReleaseNowHas = "2026 Q4 (slipped)";
        private const int TheDeliveryOnScreen = 4711;

        private static readonly DateTime ReleaseDate = TestToday.AFutureDate;

        private static readonly DateTime TheDateTheReleaseNowHas = TestToday.AFutureDate.AddDays(14);

        private static readonly DateTime HeardFromTheReleaseAt = TestToday.AmbientAsUtcMidnight.AddHours(9);

        private static readonly string[] FeaturesTheReleaseNames = ["Checkout", "Search"];

        private static readonly string[] TheFeaturesTheReleaseNowNames = ["Checkout", "Search", "Payments"];

        [Test]
        public void A_Delivery_following_a_Release_takes_the_name_date_and_Features_the_Release_now_has()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.SyncFromSource(TheNameTheReleaseNowHas, TheDateTheReleaseNowHas, FeaturesNamed(TheFeaturesTheReleaseNowNames), HeardFromTheReleaseAt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(TheNameTheReleaseNowHas));
                Assert.That(delivery.Date, Is.EqualTo(TheDateTheReleaseNowHas));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(TheFeaturesTheReleaseNowNames));
            }
        }

        /// <summary>
        /// A Release that slipped past its own date is an ordinary state and the reason this Epic
        /// exists: refusing the date here would leave Lighthouse disagreeing with Jira about it, which
        /// is the exact failure the sync removes. Typing that date by hand is still refused, one layer
        /// up, where a person is doing the typing.
        /// </summary>
        [Test]
        public void A_Release_that_has_slipped_past_its_own_date_still_hands_that_date_over()
        {
            var delivery = ADeliveryFollowingARelease();
            var aDateThatHasBeenAndGone = TestToday.AmbientAsUtcMidnight.AddDays(-3);

            delivery.SyncFromSource(ReleaseName, aDateThatHasBeenAndGone, FeaturesNamed(FeaturesTheReleaseNames), HeardFromTheReleaseAt);

            Assert.That(delivery.Date, Is.EqualTo(aDateThatHasBeenAndGone));
        }

        [Test]
        public void A_refresh_that_found_nothing_new_leaves_the_version_an_open_browser_is_holding_alone()
        {
            var delivery = ADeliveryFollowingARelease();
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            delivery.SyncFromSource(ReleaseName, ReleaseDate, TheFeaturesItAlreadyHolds(delivery), HeardFromTheReleaseAt);

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionAnOpenEditorHolds));
        }

        /// <summary>
        /// When the Release was last heard from is not one of the three things it owns - it is a note
        /// about the reading, not about what was read - so a refresh that found nothing new still
        /// records it. Slice 03 shows this date on a Delivery whose Release has gone; recorded only on
        /// the refreshes that changed something, it would name the last change rather than the last
        /// successful read and the screen would overstate how stale the values are.
        /// </summary>
        [Test]
        public void A_refresh_that_found_nothing_new_still_records_that_the_Release_answered()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.SyncFromSource(ReleaseName, ReleaseDate, TheFeaturesItAlreadyHolds(delivery), HeardFromTheReleaseAt);

            Assert.That(delivery.SourceLastSyncedOn, Is.EqualTo(HeardFromTheReleaseAt));
        }

        [TestCaseSource(nameof(EachThingTheReleaseCanMoveOnItsOwn))]
        public void A_refresh_that_found_one_thing_changed_moves_the_version_an_open_browser_is_holding(
            Action<Delivery> whatTheReleaseNowSays)
        {
            var delivery = ADeliveryFollowingARelease();
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            whatTheReleaseNowSays(delivery);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(versionAnOpenEditorHolds));
        }

        private static IEnumerable<TestCaseData> EachThingTheReleaseCanMoveOnItsOwn()
        {
            yield return new TestCaseData((Action<Delivery>)(delivery =>
                    delivery.SyncFromSource(TheNameTheReleaseNowHas, ReleaseDate, TheFeaturesItAlreadyHolds(delivery), HeardFromTheReleaseAt)))
                .SetName("Only the name moved");

            yield return new TestCaseData((Action<Delivery>)(delivery =>
                    delivery.SyncFromSource(ReleaseName, TheDateTheReleaseNowHas, TheFeaturesItAlreadyHolds(delivery), HeardFromTheReleaseAt)))
                .SetName("Only the date moved");

            yield return new TestCaseData((Action<Delivery>)(delivery =>
                    delivery.SyncFromSource(ReleaseName, ReleaseDate, FeaturesNamed(TheFeaturesTheReleaseNowNames), HeardFromTheReleaseAt)))
                .SetName("Only the Features moved");
        }

        /// <summary>
        /// The collection the refresh is handed already leaves retired Deliveries out, so this is the
        /// second of two answers to the same question rather than the only one. It is here because
        /// #5698 pins a closure snapshot on a retired Delivery, and a sync that wrote to one would
        /// un-pin a record the product promises does not move again.
        /// </summary>
        [Test]
        public void A_retired_Delivery_is_never_written_to_by_the_Release_it_used_to_follow()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.Archive(TestToday.AmbientAsUtcMidnight);
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            Assert.Throws<DeliveryArchivedException>(() =>
                delivery.SyncFromSource(TheNameTheReleaseNowHas, TheDateTheReleaseNowHas, FeaturesNamed(TheFeaturesTheReleaseNowNames), HeardFromTheReleaseAt));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(FeaturesTheReleaseNames));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
                Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionAnOpenEditorHolds));
            }
        }

        /// <summary>
        /// A Delivery chosen by hand has an owner who is editing it, so a sync reaching one is a
        /// mistake in whatever selected it rather than something to apply quietly over their work.
        /// </summary>
        [Test]
        public void A_Delivery_that_follows_no_Release_refuses_to_be_synced_from_one()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.Id = TheDeliveryOnScreen;

            var refusal = Assert.Throws<DeliverySourceBoundException>(() =>
                delivery.SyncFromSource(TheNameTheReleaseNowHas, TheDateTheReleaseNowHas, FeaturesNamed(TheFeaturesTheReleaseNowNames), HeardFromTheReleaseAt));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.Code, Is.EqualTo("delivery-not-source-bound"));
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
            }
        }

        /// <summary>
        /// Every screen names a Delivery by its name, so a blank one leaves a row nobody can identify
        /// and no way back to it - the Delivery still follows the Release and refuses to be renamed.
        /// </summary>
        [TestCase(null)]
        [TestCase("")]
        public void A_Release_cannot_leave_the_Delivery_that_follows_it_without_a_name(string? nothingAtAll)
        {
            var delivery = ADeliveryFollowingARelease();

            Assert.Catch<ArgumentException>(() =>
                delivery.SyncFromSource(nothingAtAll!, TheDateTheReleaseNowHas, FeaturesNamed(TheFeaturesTheReleaseNowNames), HeardFromTheReleaseAt));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
            }
        }

        [Test]
        public void A_Release_that_now_names_no_work_at_all_empties_the_Delivery_rather_than_leaving_it_as_it_was()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.SyncFromSource(ReleaseName, ReleaseDate, [], HeardFromTheReleaseAt);

            Assert.That(delivery.Features, Is.Empty);
        }

        private static List<Feature> FeaturesNamed(IEnumerable<string> names)
        {
            return names.Select((name, index) => new Feature { Id = index + 1, Name = name }).ToList();
        }

        private static List<Feature> TheFeaturesItAlreadyHolds(Delivery delivery)
        {
            return [.. delivery.Features];
        }

        private static Delivery ADeliveryChosenByHand()
        {
            var delivery = new Delivery(ReleaseName, ReleaseDate, 1);
            delivery.ReplaceFeatures(FeaturesNamed(FeaturesTheReleaseNames));

            return delivery;
        }

        private static Delivery ADeliveryFollowingARelease()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(ReleaseSourceKey, ReleaseId);

            return delivery;
        }
    }
}
