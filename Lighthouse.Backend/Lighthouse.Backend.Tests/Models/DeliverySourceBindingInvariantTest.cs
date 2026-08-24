using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// What a Delivery that follows a Release elsewhere may and may not be asked to do. The far side
    /// owns the name, the date and the Feature list, so a hand-written change to any of them is
    /// refused rather than merged, and releasing the Delivery - not editing it - is the way back to
    /// choosing those three yourself.
    /// </summary>
    public class DeliverySourceBindingInvariantTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string ReleaseName = "2026 Q4";
        private const string SourceBoundCode = "delivery-source-bound";
        private const string RenamedByHand = "Renamed By Hand";
        private const string FeatureAddedByHand = "Added By Hand";
        private const string ARuleChosenByHand = "{\"conditions\":[]}";

        private static readonly DateTime ReleaseDate = TestToday.AFutureDate;

        private static readonly string[] FeaturesTheReleaseNames = ["Checkout", "Search"];

        private static readonly string[] JustTheFeatureAddedByHand = [FeatureAddedByHand];

        [Test]
        public void A_Delivery_made_to_follow_a_Release_records_where_it_follows_and_which_Release_it_follows_there()
        {
            var delivery = ADeliveryChosenByHand();

            delivery.BindToSource(ReleaseSourceKey, ReleaseId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound));
                Assert.That(delivery.SourceKey, Is.EqualTo(ReleaseSourceKey));
                Assert.That(delivery.SourceReference, Is.EqualTo(ReleaseId));
            }
        }

        /// <summary>
        /// The Release is held by its id rather than its name, so renaming it on the far side leaves
        /// the Delivery pointing at the same Release.
        /// </summary>
        [Test]
        public void A_Delivery_holds_the_Release_by_something_a_rename_on_the_far_side_cannot_move()
        {
            var delivery = ADeliveryChosenByHand();

            delivery.BindToSource(ReleaseSourceKey, ReleaseId);

            Assert.That(delivery.SourceReference, Is.Not.EqualTo(ReleaseName));
        }

        [Test]
        public void A_Delivery_that_has_only_just_started_following_a_Release_claims_nothing_about_having_heard_from_it()
        {
            var delivery = ADeliveryChosenByHand();

            delivery.BindToSource(ReleaseSourceKey, ReleaseId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
            }
        }

        [TestCaseSource(nameof(EveryHandWriteToWhatTheReleaseOwns))]
        public void A_source_bound_Delivery_refuses_every_hand_write_to_the_fields_the_Release_owns(Action<Delivery> handWrite)
        {
            var delivery = ADeliveryFollowingARelease();

            Assert.Throws<DeliverySourceBoundException>(() => handWrite(delivery));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(FeaturesTheReleaseNames));
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound));
                Assert.That(delivery.SourceKey, Is.EqualTo(ReleaseSourceKey));
                Assert.That(delivery.SourceReference, Is.EqualTo(ReleaseId));
            }
        }

        [Test]
        public void The_refusal_names_the_Delivery_and_says_which_of_the_two_opposite_things_to_do_about_it()
        {
            var delivery = ADeliveryFollowingARelease();

            var refusal = Assert.Throws<DeliverySourceBoundException>(() => delivery.Rename("Renamed By Hand"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(refusal?.DeliveryId, Is.EqualTo(delivery.Id));
                Assert.That(refusal?.Code, Is.EqualTo(SourceBoundCode));
            }
        }

        [Test]
        public void Releasing_a_Delivery_hands_back_the_name_date_and_Features_the_Release_last_gave_it()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.Unbind();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(delivery.Name, Is.EqualTo(ReleaseName));
                Assert.That(delivery.Date, Is.EqualTo(ReleaseDate));
                Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(FeaturesTheReleaseNames));
            }
        }

        /// <summary>
        /// A Delivery that says it is chosen by hand while still naming a Release is the one state
        /// this must never leave behind: the next refresh would find it and start overwriting a
        /// Delivery somebody believes is theirs to edit.
        /// </summary>
        [Test]
        public void Releasing_a_Delivery_leaves_no_trace_of_the_Release_it_used_to_follow()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.Unbind();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceKey, Is.Null);
                Assert.That(delivery.SourceReference, Is.Null);
            }
        }

        [TestCaseSource(nameof(EveryHandWriteOnADeliveryThatFollowsNothing))]
        public void A_Delivery_that_follows_no_Release_carries_out_every_hand_write(Func<Delivery> aDelivery, Action<Delivery> handWrite, Action<Delivery> assertItLanded)
        {
            var delivery = aDelivery();

            handWrite(delivery);

            using (Assert.EnterMultipleScope())
            {
                assertItLanded(delivery);
                Assert.That(delivery.SelectionMode, Is.Not.EqualTo(DeliverySelectionMode.SourceBound));
                Assert.That(delivery.SourceKey, Is.Null);
                Assert.That(delivery.SourceReference, Is.Null);
            }
        }

        [Test]
        public void A_released_Delivery_still_claims_nothing_about_having_heard_from_the_Release()
        {
            var delivery = ADeliveryFollowingARelease();

            delivery.Unbind();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceLastSyncedOn, Is.Null);
                Assert.That(delivery.SourceUnavailableReason, Is.Null);
            }
        }

        [TestCaseSource(nameof(EveryHandWriteToWhatTheReleaseOwns))]
        public void A_released_Delivery_accepts_every_hand_write_the_Release_used_to_refuse(Action<Delivery> handWrite)
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.Unbind();

            Assert.DoesNotThrow(() => handWrite(delivery));
        }

        [Test]
        public void A_released_Delivery_can_be_renamed_by_hand_again()
        {
            var delivery = ADeliveryFollowingARelease();
            delivery.Unbind();

            delivery.Rename("Renamed By Hand");

            Assert.That(delivery.Name, Is.EqualTo("Renamed By Hand"));
        }

        [Test]
        public void A_Delivery_following_nothing_cannot_be_released()
        {
            var delivery = ADeliveryChosenByHand();

            Assert.Throws<DeliverySourceBoundException>(delivery.Unbind);
        }

        [Test]
        public void A_Delivery_already_following_a_Release_cannot_be_pointed_at_a_second_one()
        {
            var delivery = ADeliveryFollowingARelease();

            Assert.Throws<DeliverySourceBoundException>(() => delivery.BindToSource(ReleaseSourceKey, "10999"));
        }

        [TestCaseSource(nameof(EveryMoveOnAndOffARelease))]
        public void Moving_a_Delivery_on_or_off_a_Release_moves_the_version_an_open_editor_holds(Action<Delivery> move)
        {
            var delivery = ADeliveryFollowingARelease();
            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            move(delivery);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(versionAnOpenEditorHolds));
        }

        private static IEnumerable<TestCaseData> EveryHandWriteToWhatTheReleaseOwns()
        {
            foreach (var handWrite in TheFiveHandWrites())
            {
                yield return new TestCaseData(handWrite.Apply).SetName(handWrite.Name);
            }
        }

        private static IEnumerable<TestCaseData> EveryHandWriteOnADeliveryThatFollowsNothing()
        {
            foreach (var handWrite in TheFiveHandWrites())
            {
                yield return new TestCaseData((Func<Delivery>)ADeliveryChosenByHand, handWrite.Apply, handWrite.TookHold)
                    .SetName($"{handWrite.Name} on a Delivery chosen by hand");
                yield return new TestCaseData((Func<Delivery>)ADeliveryChosenByRule, handWrite.Apply, handWrite.TookHold)
                    .SetName($"{handWrite.Name} on a Delivery chosen by rule");
            }
        }

        /// <summary>
        /// Each of the five carries the check that it landed, so the one table can be read twice:
        /// once for the Delivery that must refuse the write, once for the Delivery that must carry
        /// it out. A refusal that fired for every Delivery rather than only the bound one would
        /// satisfy the first reading and fail the second.
        /// </summary>
        private static IEnumerable<(string Name, Action<Delivery> Apply, Action<Delivery> TookHold)> TheFiveHandWrites()
        {
            yield return (
                "Rename",
                delivery => delivery.Rename(RenamedByHand),
                delivery => Assert.That(delivery.Name, Is.EqualTo(RenamedByHand)));

            yield return (
                "Reschedule",
                delivery => delivery.Reschedule(ReleaseDate.AddDays(30)),
                delivery => Assert.That(delivery.Date, Is.EqualTo(ReleaseDate.AddDays(30))));

            yield return (
                "ReplaceFeatures",
                delivery => delivery.ReplaceFeatures([new Feature { Id = 99, Name = FeatureAddedByHand }]),
                delivery => Assert.That(delivery.Features.Select(feature => feature.Name), Is.EqualTo(JustTheFeatureAddedByHand)));

            yield return (
                "SelectFeaturesByRule",
                delivery => delivery.SelectFeaturesByRule(ARuleChosenByHand, 1),
                delivery => Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased)));

            yield return (
                "SelectFeaturesByHand",
                delivery => delivery.SelectFeaturesByHand(),
                delivery => Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual)));
        }

        private static IEnumerable<TestCaseData> EveryMoveOnAndOffARelease()
        {
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Unbind())).SetName("Unbind");
            yield return new TestCaseData((Action<Delivery>)(delivery =>
            {
                delivery.Unbind();
                delivery.BindToSource(ReleaseSourceKey, ReleaseId);
            })).SetName("BindToSource");
        }

        private static Delivery ADeliveryChosenByHand()
        {
            var delivery = new Delivery(ReleaseName, ReleaseDate, 1);
            delivery.ReplaceFeatures(FeaturesTheReleaseNames.Select(name => new Feature { Name = name }).ToList());

            return delivery;
        }

        private static Delivery ADeliveryChosenByRule()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.SelectFeaturesByRule(ARuleChosenByHand, 1);

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
