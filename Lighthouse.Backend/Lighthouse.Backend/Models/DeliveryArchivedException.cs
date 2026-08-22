namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Raised when something tries to change a Delivery that has been archived, to archive one that
    /// already is, or to un-archive one that never was. An archived Delivery is the record of what was
    /// true on the day it closed, so a later change to it would rewrite history rather than continue it.
    /// </summary>
    public sealed class DeliveryArchivedException : InvalidOperationException
    {
        private DeliveryArchivedException(int deliveryId, string code, string message)
            : base(message)
        {
            DeliveryId = deliveryId;
            Code = code;
        }

        public int DeliveryId { get; }

        /// <summary>
        /// What the caller is told to do about it. "Archived" and "not archived" are opposite
        /// instructions - one says bring it back first, the other says it is already back - so a
        /// screen that read a single shared code would tell somebody to do the reverse of what they
        /// need.
        /// </summary>
        public string Code { get; }

        public static DeliveryArchivedException AlreadyArchived(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, "delivery-archived", $"Delivery {deliveryId} is already archived.");
        }

        public static DeliveryArchivedException NotArchived(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, "delivery-not-archived", $"Delivery {deliveryId} is not archived.");
        }

        public static DeliveryArchivedException CannotBeChanged(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, "delivery-archived", $"Delivery {deliveryId} is archived and cannot be changed.");
        }
    }
}
