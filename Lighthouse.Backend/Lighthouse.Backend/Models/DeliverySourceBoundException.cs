namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Raised when something tries to change by hand what a Delivery follows from elsewhere, to point
    /// a Delivery at a second source while it still follows a first, or to release one that follows
    /// nothing. A Delivery that follows a Release shows the far side's answer, so a change made here
    /// would be overwritten by the next refresh - it is refused rather than silently lost.
    /// </summary>
    public sealed class DeliverySourceBoundException : InvalidOperationException
    {
        private DeliverySourceBoundException(int deliveryId, string code, string message)
            : base(message)
        {
            DeliveryId = deliveryId;
            Code = code;
        }

        public int DeliveryId { get; }

        /// <summary>
        /// What the caller is told to do about it. "Follows a source" and "follows nothing" are
        /// opposite instructions - one says release it first, the other says it is already released -
        /// so a screen reading a single shared code would tell somebody to do the reverse of what
        /// they need.
        /// </summary>
        public string Code { get; }

        public static DeliverySourceBoundException AlreadyBound(int deliveryId)
        {
            return new DeliverySourceBoundException(deliveryId, "delivery-source-bound", $"Delivery {deliveryId} already follows a source.");
        }

        public static DeliverySourceBoundException NotBound(int deliveryId)
        {
            return new DeliverySourceBoundException(deliveryId, "delivery-not-source-bound", $"Delivery {deliveryId} does not follow a source.");
        }

        public static DeliverySourceBoundException CannotBeChanged(int deliveryId)
        {
            return new DeliverySourceBoundException(deliveryId, "delivery-source-bound", $"Delivery {deliveryId} follows a source and cannot be changed by hand.");
        }
    }
}
