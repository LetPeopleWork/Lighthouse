namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Raised when something tries to change a Delivery that has been archived, to archive one that
    /// already is, or to un-archive one that never was. An archived Delivery is the record of what was
    /// true on the day it closed, so a later change to it would rewrite history rather than continue it.
    /// </summary>
    public sealed class DeliveryArchivedException : InvalidOperationException
    {
        private DeliveryArchivedException(int deliveryId, string message)
            : base(message)
        {
            DeliveryId = deliveryId;
        }

        public int DeliveryId { get; }

        public static DeliveryArchivedException AlreadyArchived(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, $"Delivery {deliveryId} is already archived.");
        }

        public static DeliveryArchivedException NotArchived(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, $"Delivery {deliveryId} is not archived.");
        }

        public static DeliveryArchivedException CannotBeChanged(int deliveryId)
        {
            return new DeliveryArchivedException(deliveryId, $"Delivery {deliveryId} is archived and cannot be changed.");
        }
    }
}
