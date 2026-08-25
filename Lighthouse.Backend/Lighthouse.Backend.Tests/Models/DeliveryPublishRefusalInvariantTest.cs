using System.Runtime.CompilerServices;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliverySources;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// What a Delivery remembers about a source that would not take its forecast. It is a statement
    /// about one Release in one project - the permission a Release write needs is held per project, so a
    /// Delivery bound to a project that refuses and one bound to a project that does not have to be able
    /// to say different things while sitting side by side.
    ///
    /// It is deliberately not the broken-source state. A credential that may not write says nothing
    /// about whether the Release is still there, and sending somebody to re-create a Release over a
    /// permission problem is the confusion this whole vocabulary exists to prevent.
    /// </summary>
    public class DeliveryPublishRefusalInvariantTest
    {
        private const string ReleaseSourceKey = "jira-release";
        private const string ReleaseId = "10412";
        private const string WhatJiraSaid = "You must have global or project administrator rights in order to modify versions.";
        private const string WhatJiraSaidNext = "The description is over 16384 characters.";

        private static readonly DateTime ReleaseDate = TestToday.AFutureDate;
        private static readonly DateOnly TheDayItWasRefused = new(2026, 8, 25);

        [Test]
        public void A_Delivery_nothing_has_refused_says_nothing_about_being_refused()
        {
            var delivery = ABroadcastingDelivery();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusedOn, Is.Null);
                Assert.That(delivery.LastPublishRefusalReason, Is.Null);
            }
        }

        /// <summary>
        /// The remote's own sentence, kept as it was said. It already names what to fix in the words the
        /// administrator will search for, and a sentence written here instead would lose exactly that.
        /// </summary>
        [Test]
        public void A_refused_Delivery_remembers_what_it_was_told_and_when()
        {
            var delivery = ABroadcastingDelivery();

            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.EqualTo(WhatJiraSaid));
                Assert.That(delivery.LastPublishRefusedOn, Is.EqualTo(InstanceCalendar.AsUtcMidnight(TheDayItWasRefused)));
            }
        }

        [Test]
        public void A_second_reason_replaces_the_first_rather_than_joining_it()
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            delivery.RecordPublishRefusal(WhatJiraSaidNext, TheDayItWasRefused.AddDays(1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.EqualTo(WhatJiraSaidNext));
                Assert.That(delivery.LastPublishRefusedOn, Is.EqualTo(InstanceCalendar.AsUtcMidnight(TheDayItWasRefused.AddDays(1))));
            }
        }

        /// <summary>
        /// A write that went through is the end of the report. Left standing, it would have a Delivery
        /// that is publishing perfectly well still telling an administrator to go and fix a permission.
        /// </summary>
        [Test]
        public void A_write_that_went_through_takes_the_refusal_off()
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            delivery.ClearPublishRefusal();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.Null);
                Assert.That(delivery.LastPublishRefusedOn, Is.Null);
            }
        }

        /// <summary>
        /// A source that refuses again tomorrow is not news. The day rather than the instant is what
        /// keeps that out of the database: the same refusal on the same day writes the same values back,
        /// so the row stays out of the save and an open browser's copy does not expire on a timer.
        /// </summary>
        [Test]
        public void The_same_refusal_on_the_same_day_leaves_the_version_alone()
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);
            var versionBefore = delivery.ConcurrencyToken;

            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionBefore));
        }

        [Test]
        public void A_Delivery_that_was_never_refused_is_not_changed_by_being_told_so_again()
        {
            var delivery = ABroadcastingDelivery();
            var versionBefore = delivery.ConcurrencyToken;

            delivery.ClearPublishRefusal();

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionBefore));
        }

        [TestCaseSource(nameof(EveryMoveThatChangesWhatIsRefused))]
        public void A_change_to_what_is_refused_moves_the_version(Action<Delivery> change)
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);
            var versionBefore = delivery.ConcurrencyToken;

            change(delivery);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(versionBefore));
        }

        private static IEnumerable<TestCaseData> EveryMoveThatChangesWhatIsRefused()
        {
            yield return new TestCaseData((Action<Delivery>)(delivery =>
                delivery.RecordPublishRefusal(WhatJiraSaidNext, TheDayItWasRefused))).SetName("A different reason");
            yield return new TestCaseData((Action<Delivery>)(delivery =>
                delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused.AddDays(1)))).SetName("The same reason on another day");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.ClearPublishRefusal())).SetName("Nothing refusing it any more");
        }

        /// <summary>
        /// Letting go of the Release takes the report with it, and so does switching the broadcast off.
        /// Both leave a Delivery that is not trying to publish anything, and a standing refusal on one of
        /// those describes a write that is not being attempted - it would come back on screen the moment
        /// somebody switched publishing on again, about an attempt nobody has made yet.
        /// </summary>
        [TestCaseSource(nameof(EveryWayOfNoLongerPublishing))]
        public void A_Delivery_that_stops_publishing_stops_reporting_a_refusal(Action<Delivery> stopPublishing)
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            stopPublishing(delivery);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.LastPublishRefusalReason, Is.Null);
                Assert.That(delivery.LastPublishRefusedOn, Is.Null);
            }
        }

        private static IEnumerable<TestCaseData> EveryWayOfNoLongerPublishing()
        {
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Unbind())).SetName("Letting go of the Release");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.SetForecastPublishing(false))).SetName("Switching the broadcast off");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Archive(TestToday.AmbientAsUtcMidnight))).SetName("Retiring the Delivery");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound))).SetName("The Release turning out to be gone");
        }

        /// <summary>
        /// The Release turning out to be gone is the one that could never be undone. A Delivery whose
        /// source is finished is not published at all, and only a successful publish clears a refusal -
        /// so the report would stand for good, beside a notice saying the Release does not exist, telling
        /// the reader to go and fix a permission on it.
        /// </summary>
        [Test]
        public void A_Release_that_turns_out_to_be_gone_does_not_leave_a_permission_report_standing_over_it()
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            delivery.MarkSourceUnavailable(DeliverySourceUnavailableReason.SourceNotFound);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SourceUnavailableReason, Is.EqualTo(DeliverySourceUnavailableReason.SourceNotFound));
                Assert.That(delivery.LastPublishRefusalReason, Is.Null);
            }
        }

        /// <summary>
        /// Retiring a Delivery is the third way of no longer publishing, and the state it would leave is
        /// unreachable: every mutator that could take the report off refuses on an archived Delivery.
        /// </summary>
        [Test]
        public void Retiring_a_Delivery_takes_the_report_with_it_rather_than_freezing_it()
        {
            var delivery = ABroadcastingDelivery();
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            delivery.Archive(TestToday.AmbientAsUtcMidnight);

            Assert.That(delivery.LastPublishRefusalReason, Is.Null);
        }

        /// <summary>
        /// Switching off a broadcast that was already off still has to take the report away. Guarded on
        /// the switch having moved, it would leave a report on a Delivery nothing publishes - and nothing
        /// left could clear it.
        /// </summary>
        [Test]
        public void Switching_off_a_broadcast_that_was_already_off_still_takes_the_report_away()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(ReleaseSourceKey, ReleaseId);
            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            delivery.SetForecastPublishing(false);

            Assert.That(delivery.LastPublishRefusalReason, Is.Null);
        }

        /// <summary>
        /// A remote may answer with anything at all - a page of HTML, a stack trace. It is stored, sent
        /// on every read of the Portfolio's Deliveries, and rendered in a box that clips, so one bad
        /// answer would otherwise cost every reader of that Portfolio the bandwidth and the layout.
        /// </summary>
        [Test]
        public void A_remote_that_answers_with_a_wall_of_text_has_only_as_much_kept_as_anybody_will_read()
        {
            var delivery = ABroadcastingDelivery();

            delivery.RecordPublishRefusal(new string('x', 5_000), TheDayItWasRefused);

            Assert.That(delivery.LastPublishRefusalReason, Has.Length.LessThan(1_000));
        }

        [Test]
        public void The_day_a_source_refused_is_stored_as_a_day_the_database_cannot_shift()
        {
            var delivery = ABroadcastingDelivery();

            delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused);

            var stored = delivery.LastPublishRefusedOn;

            Assert.That(stored, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Value.Kind, Is.EqualTo(DateTimeKind.Utc),
                    "a local-kind midnight becomes the evening before on the way to the database.");
                Assert.That(stored.Value.TimeOfDay, Is.EqualTo(TimeSpan.Zero));
            }
        }

        [Test]
        public void A_Delivery_that_follows_nothing_cannot_have_been_refused_by_anything()
        {
            var delivery = ADeliveryChosenByHand();

            var refusal = Assert.Throws<DeliverySourceBoundException>(
                () => delivery.RecordPublishRefusal(WhatJiraSaid, TheDayItWasRefused));

            Assert.That(refusal.Code, Is.EqualTo("delivery-not-source-bound"));
        }

        [TestCase("")]
        [TestCase("   ")]
        public void A_refusal_nobody_gave_a_reason_for_is_not_a_refusal_worth_showing(string nothingSaid)
        {
            var delivery = ABroadcastingDelivery();

            Assert.Throws<ArgumentException>(() => delivery.RecordPublishRefusal(nothingSaid, TheDayItWasRefused));
        }

        [Test]
        public void What_a_source_refused_cannot_be_assigned_once_the_Delivery_exists()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(IsReachableFromOutside(nameof(Delivery.LastPublishRefusedOn)), Is.False);
                Assert.That(IsReachableFromOutside(nameof(Delivery.LastPublishRefusalReason)), Is.False);
            }
        }

        private static bool IsReachableFromOutside(string propertyName)
        {
            var setter = typeof(Delivery).GetProperty(propertyName)!.SetMethod!;

            return setter.IsPublic
                || setter.ReturnParameter.GetRequiredCustomModifiers().Any(modifier => modifier == typeof(IsExternalInit));
        }

        private static Delivery ADeliveryChosenByHand()
        {
            return new Delivery("2026 Q4", ReleaseDate, 1);
        }

        private static Delivery ABroadcastingDelivery()
        {
            var delivery = ADeliveryChosenByHand();
            delivery.BindToSource(ReleaseSourceKey, ReleaseId);
            delivery.SetForecastPublishing(true);

            return delivery;
        }
    }
}
