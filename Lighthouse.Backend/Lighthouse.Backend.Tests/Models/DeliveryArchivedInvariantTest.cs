using System.Runtime.CompilerServices;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class DeliveryArchivedInvariantTest
    {
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
        public void ReplaceFeatures_ArchivedDelivery_IsRefused()
        {
            var delivery = LiveDelivery();
            delivery.ReplaceFeatures([new Feature { Name = "Original" }]);
            delivery.Archive(ClosingInstant);

            Assert.Throws<DeliveryArchivedException>(() => delivery.ReplaceFeatures([new Feature { Name = "Replacement" }]));
        }

        [Test]
        public void ReplaceFeatures_ArchivedDelivery_LeavesTheFeaturesItClosedWith()
        {
            var delivery = LiveDelivery();
            var original = new Feature { Name = "Original" };
            delivery.ReplaceFeatures([original]);
            delivery.Archive(ClosingInstant);

            Assert.Throws<DeliveryArchivedException>(() => delivery.ReplaceFeatures([new Feature { Name = "Replacement" }]));
            Assert.That(delivery.Features.Single(), Is.SameAs(original));
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

        [Test]
        public void Rename_ArchivedDelivery_IsRefusedAndLeavesTheNameItClosedWith()
        {
            var delivery = ArchivedDelivery();

            Assert.Throws<DeliveryArchivedException>(() => delivery.Rename("Renamed After Closing"));
            Assert.That(delivery.Name, Is.EqualTo("Q3 Release"));
        }

        [Test]
        public void Reschedule_ArchivedDelivery_IsRefusedAndLeavesTheDateItClosedWith()
        {
            var delivery = ArchivedDelivery();
            var dateAtClosure = delivery.Date;

            Assert.Throws<DeliveryArchivedException>(() => delivery.Reschedule(dateAtClosure.AddDays(30)));
            Assert.That(delivery.Date, Is.EqualTo(dateAtClosure));
        }

        [Test]
        public void SelectFeaturesByRule_ArchivedDelivery_IsRefusedAndLeavesTheRuleItClosedWith()
        {
            var delivery = ArchivedDelivery();

            Assert.Throws<DeliveryArchivedException>(() => delivery.SelectFeaturesByRule("{\"conditions\":[]}", 1));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.Manual));
                Assert.That(delivery.RuleDefinitionJson, Is.Null);
            }
        }

        [Test]
        public void SelectFeaturesByHand_ArchivedDelivery_IsRefused()
        {
            var delivery = LiveDelivery();
            delivery.SelectFeaturesByRule("{\"conditions\":[]}", 1);
            delivery.Archive(ClosingInstant);

            Assert.Throws<DeliveryArchivedException>(delivery.SelectFeaturesByHand);
            Assert.That(delivery.SelectionMode, Is.EqualTo(DeliverySelectionMode.RuleBased));
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

        /// <summary>
        /// The point of the mutators is that an archived Delivery cannot be changed, not that every
        /// caller remembers to ask first. These properties may be given a value when a Delivery is
        /// created and never assigned afterwards, which is what a setter usable only in an object
        /// initializer means; an ordinary setter would put the refusal back in the hands of whoever
        /// writes the next call site.
        /// </summary>
        [TestCase(nameof(Delivery.Name))]
        [TestCase(nameof(Delivery.Date))]
        [TestCase(nameof(Delivery.SelectionMode))]
        [TestCase(nameof(Delivery.RuleDefinitionJson))]
        [TestCase(nameof(Delivery.RuleSchemaVersion))]
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
            return new Delivery("Q3 Release", TestToday.AFutureDate, 1, TestToday.Ambient);
        }

        private static Delivery ArchivedDelivery()
        {
            var delivery = LiveDelivery();
            delivery.Archive(ClosingInstant);

            return delivery;
        }
    }
}
