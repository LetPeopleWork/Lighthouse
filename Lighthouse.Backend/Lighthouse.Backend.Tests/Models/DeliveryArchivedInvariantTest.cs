using System.Runtime.CompilerServices;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryArchivedInvariantTest
    {
        private const string TheNameItClosedWith = "Q3 Release";
        private const string TheFeatureItClosedWith = "Original";
        private const string ARuleChosenByHand = "{\"conditions\":[]}";
        private const string ReleaseSourceKey = "jira-release";
        private const string TheReleaseItFollowed = "10412";

        private static readonly DateTime ClosingInstant = new(2026, 8, 22, 17, 45, 0, DateTimeKind.Utc);

        [Test]
        public void Archive_LiveDelivery_RecordsWhenItClosed()
        {
            var delivery = LiveDelivery();

            delivery.Archive(ClosingInstant);

            Assert.That(delivery.ArchivedOn, Is.EqualTo(ClosingInstant));
        }

        [Test]
        public void Archive_AlreadyArchivedDelivery_IsRefused()
        {
            var delivery = LiveDelivery();
            delivery.Archive(ClosingInstant);

            Assert.Throws<DeliveryArchivedException>(() => delivery.Archive(ClosingInstant.AddDays(1)));
        }

        [Test]
        public void Archive_AlreadyArchivedDelivery_KeepsTheFirstClosingInstant()
        {
            var delivery = LiveDelivery();
            delivery.Archive(ClosingInstant);

            Assert.Throws<DeliveryArchivedException>(() => delivery.Archive(ClosingInstant.AddDays(1)));
            Assert.That(delivery.ArchivedOn, Is.EqualTo(ClosingInstant));
        }

        [Test]
        public void Unarchive_ArchivedDelivery_ForgetsWhenItClosed()
        {
            var delivery = LiveDelivery();
            delivery.Archive(ClosingInstant);

            delivery.Unarchive();

            Assert.That(delivery.ArchivedOn, Is.Null);
        }

        [Test]
        public void Unarchive_LiveDelivery_IsRefused()
        {
            var delivery = LiveDelivery();

            Assert.Throws<DeliveryArchivedException>(delivery.Unarchive);
        }

        [Test]
        public void ReplaceFeatures_LiveDelivery_SwapsTheWholeSet()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Name = "Original" }]);

            var replacement = new Feature { Name = "Replacement" };
            delivery.ReplaceFeatures([replacement]);

            Assert.That(delivery.Features.Single(), Is.SameAs(replacement));
        }

        [Test]
        public void ReplaceFeatures_UnarchivedDelivery_IsAllowedAgain()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Name = "Original" }]);
            delivery.Archive(ClosingInstant);

            delivery.Unarchive();
            var replacement = new Feature { Name = "Replacement" };
            delivery.ReplaceFeatures([replacement]);

            Assert.That(delivery.Features.Single(), Is.SameAs(replacement));
        }

        /// <summary>
        /// Every change an archived Delivery must refuse, in one place, each row carrying the check
        /// that the refusal left the Delivery exactly as it closed. A half-applied change to something
        /// frozen is worse than none: the closure record was pinned against what the Delivery was.
        ///
        /// The list is written out by hand and nothing checks it is complete, so a method added to
        /// Delivery has to be added here in the same change - left out, its archived behaviour is
        /// covered by nothing at all.
        /// </summary>
        [TestCaseSource(nameof(EveryChangeAnArchivedDeliveryRefuses))]
        public void A_change_asked_of_an_archived_Delivery_is_refused_and_leaves_it_as_it_closed(
            Func<Delivery> anArchivedDelivery, Action<Delivery> change, Action<Delivery> stillAsItClosed)
        {
            var delivery = anArchivedDelivery();

            Assert.Throws<DeliveryArchivedException>(() => change(delivery));

            stillAsItClosed(delivery);
        }

        private static IEnumerable<TestCaseData> EveryChangeAnArchivedDeliveryRefuses()
        {
            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDelivery,
                    (Action<Delivery>)(delivery => delivery.Rename("Renamed After Closing")),
                    (Action<Delivery>)(delivery => Assert.That(delivery.Name, Is.EqualTo(TheNameItClosedWith))))
                .SetName("Rename");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDelivery,
                    (Action<Delivery>)(delivery => delivery.Reschedule(delivery.Date.AddDays(30))),
                    (Action<Delivery>)(delivery => Assert.That(delivery.Date, Is.EqualTo(TestToday.AFutureDate))))
                .SetName("Reschedule");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDelivery,
                    (Action<Delivery>)(delivery => delivery.SelectFeaturesByRule(ARuleChosenByHand, 1)),
                    (Action<Delivery>)(delivery =>
                    {
                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                            Assert.That(delivery.RuleDefinitionJson, Is.Null);
                        }
                    }))
                .SetName("SelectFeaturesByRule");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryChosenByRule,
                    (Action<Delivery>)(delivery => delivery.SelectFeaturesByHand()),
                    (Action<Delivery>)(delivery => Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased))))
                .SetName("SelectFeaturesByHand");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryHoldingAFeature,
                    (Action<Delivery>)(delivery => delivery.ReplaceFeatures([new Feature { Name = "Replacement" }])),
                    (Action<Delivery>)(delivery => Assert.That(delivery.Features.Single().Name, Is.EqualTo(TheFeatureItClosedWith))))
                .SetName("ReplaceFeatures");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDelivery,
                    (Action<Delivery>)(delivery => delivery.BindToSource(ReleaseSourceKey, TheReleaseItFollowed)),
                    (Action<Delivery>)(delivery =>
                    {
                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                            Assert.That(delivery.SourceKey, Is.Null);
                        }
                    }))
                .SetName("BindToSource");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryFollowingARelease,
                    (Action<Delivery>)(delivery => delivery.Unbind()),
                    (Action<Delivery>)(delivery =>
                    {
                        using (Assert.EnterMultipleScope())
                        {
                            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.SourceBound));
                            Assert.That(delivery.SourceReference, Is.EqualTo(TheReleaseItFollowed));
                        }
                    }))
                .SetName("Unbind");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryFollowingARelease,
                    (Action<Delivery>)(delivery => delivery.SetForecastPublishing(true)),
                    (Action<Delivery>)(delivery => Assert.That(delivery.PublishForecastToSource, Is.False)))
                .SetName("SetForecastPublishing");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryFollowingARelease,
                    (Action<Delivery>)(delivery => delivery.RecordPublishRefusal("Refused after closing", DateOnly.FromDateTime(ClosingInstant))),
                    (Action<Delivery>)(delivery => Assert.That(delivery.LastPublishRefusalReason, Is.Null)))
                .SetName("RecordPublishRefusal");

            yield return new TestCaseData(
                    (Func<Delivery>)ArchivedDeliveryFollowingARelease,
                    (Action<Delivery>)(delivery => delivery.ClearPublishRefusal()),
                    (Action<Delivery>)(delivery => Assert.That(delivery.LastPublishRefusalReason, Is.Null)))
                .SetName("ClearPublishRefusal");
        }

        [Test]
        public void Rename_UnarchivedDelivery_IsAllowedAgain()
        {
            var delivery = ArchivedDelivery();
            delivery.Unarchive();

            delivery.Rename("Brought Back");

            Assert.That(delivery.Name, Is.EqualTo("Brought Back"));
        }

        [TestCaseSource(nameof(EveryChangeADeliveryCanBeAsked))]
        public void EveryChange_OnALiveDelivery_MovesTheConcurrencyToken(Action<Delivery> change)
        {
            var delivery = LiveDelivery();
            var tokenBefore = delivery.ConcurrencyToken;

            change(delivery);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(tokenBefore));
        }

        private static IEnumerable<TestCaseData> EveryChangeADeliveryCanBeAsked()
        {
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Rename("Renamed"))).SetName("Rename");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Reschedule(delivery.Date.AddDays(1)))).SetName("Reschedule");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.SelectFeaturesByHand())).SetName("SelectFeaturesByHand");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.SelectFeaturesByRule("{}", 1))).SetName("SelectFeaturesByRule");
            // A set that differs from what the Delivery already holds. Replacing an empty set with an
            // empty set moves nothing, and is covered on its own below.
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.ReplaceFeatures([new Feature { Id = 11, Name = "Checkout" }]))).SetName("ReplaceFeatures");
            yield return new TestCaseData((Action<Delivery>)(delivery => delivery.Archive(ClosingInstant))).SetName("Archive");
        }

        [Test]
        public void SetForecastPublishing_OnALiveBoundDelivery_MovesTheConcurrencyToken()
        {
            var delivery = LiveDeliveryFollowingARelease();
            var tokenBefore = delivery.ConcurrencyToken;

            delivery.SetForecastPublishing(true);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(tokenBefore));
        }

        /// <summary>
        /// The point of the mutators is that an archived Delivery cannot be changed, not that every
        /// caller remembers to ask first. These properties may be given a value when a Delivery is
        /// created and never assigned afterwards, which is what a setter usable only in an object
        /// initializer means; an ordinary setter would put the refusal back in the hands of whoever
        /// writes the next call site.
        ///
        /// The four source fields are here for the same reason and one more: a later pass that wants
        /// to record when it last heard from the Release will reach for an ordinary setter on the two
        /// it needs, and there is no reason for that to make the other two assignable with it.
        /// </summary>
        [TestCase(nameof(Delivery.Name))]
        [TestCase(nameof(Delivery.Date))]
        [TestCase(nameof(Delivery.SelectionMode))]
        [TestCase(nameof(Delivery.RuleDefinitionJson))]
        [TestCase(nameof(Delivery.RuleSchemaVersion))]
        [TestCase(nameof(Delivery.SourceKey))]
        [TestCase(nameof(Delivery.SourceReference))]
        [TestCase(nameof(Delivery.SourceLastSyncedOn))]
        [TestCase(nameof(Delivery.SourceUnavailableReason))]
        public void Delivery_CannotBeAssignedWhatArchivingFreezesOnceItExists(string propertyName)
        {
            var setter = typeof(Delivery).GetProperty(propertyName)!.SetMethod!;
            var settableOnlyWhenCreated = setter.ReturnParameter
                .GetRequiredCustomModifiers()
                .Any(modifier => modifier == typeof(IsExternalInit));

            Assert.That(settableOnlyWhenCreated, Is.True, $"{propertyName} can be assigned after the Delivery exists");
        }

        [Test]
        public void ReplaceFeatures_SameSetAgain_LeavesTheVersionAlone()
        {
            var delivery = LiveDelivery();
            var checkout = new Feature { Id = 11, Name = "Checkout" };
            var search = new Feature { Id = 12, Name = "Search" };
            delivery.ReplaceFeatures([checkout, search]);

            var versionAnOpenEditorHolds = delivery.ConcurrencyToken;

            // The order a rule matches in is not stable, and neither is the instance.
            delivery.ReplaceFeatures([new Feature { Id = 12, Name = "Search" }, new Feature { Id = 11, Name = "Checkout" }]);

            Assert.That(delivery.ConcurrencyToken, Is.EqualTo(versionAnOpenEditorHolds));
        }

        [Test]
        public void ReplaceFeatures_ADifferentSet_MovesTheVersion()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Id = 11, Name = "Checkout" }]);

            var before = delivery.ConcurrencyToken;

            delivery.ReplaceFeatures([new Feature { Id = 11, Name = "Checkout" }, new Feature { Id = 12, Name = "Search" }]);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(before));
        }

        [Test]
        public void ReplaceFeatures_FeaturesNeverSaved_MovesTheVersion()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Name = "First" }]);

            var before = delivery.ConcurrencyToken;

            delivery.ReplaceFeatures([new Feature { Name = "Second" }]);

            Assert.That(delivery.ConcurrencyToken, Is.Not.EqualTo(before));
        }

        private static Delivery LiveDelivery()
        {
            return new Delivery(TheNameItClosedWith, TestToday.AFutureDate, 1);
        }

        private static Delivery ArchivedDelivery()
        {
            var delivery = LiveDelivery();
            delivery.Archive(ClosingInstant);

            return delivery;
        }

        private static Delivery ArchivedDeliveryChosenByRule()
        {
            var delivery = LiveDelivery();
            delivery.SelectFeaturesByRule(ARuleChosenByHand, 1);
            delivery.Archive(ClosingInstant);

            return delivery;
        }

        private static Delivery ArchivedDeliveryHoldingAFeature()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Name = TheFeatureItClosedWith }]);
            delivery.Archive(ClosingInstant);

            return delivery;
        }

        private static Delivery LiveDeliveryFollowingARelease()
        {
            var delivery = LiveDelivery();
            delivery.BindToSource(ReleaseSourceKey, TheReleaseItFollowed);

            return delivery;
        }

        private static Delivery ArchivedDeliveryFollowingARelease()
        {
            var delivery = LiveDelivery();
            delivery.BindToSource(ReleaseSourceKey, TheReleaseItFollowed);
            delivery.Archive(ClosingInstant);

            return delivery;
        }
    }
}
