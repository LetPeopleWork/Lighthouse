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

        private static Delivery LiveDelivery()
        {
            return new Delivery("Q3 Release", TestToday.AFutureDate, 1, TestToday.Ambient);
        }
    }
}
