using System.Collections;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces.Repositories
{
    /// <summary>
    /// The Deliveries a background pass is still allowed to write to. Both writers that run with
    /// nobody watching - the daily recorder and rule re-matching - are handed one of these instead of
    /// a plain list, so leaving out a Delivery that has been retired is done once, where the rows are
    /// read, rather than remembered separately at each of them.
    ///
    /// It is a marker, not a proof: the elements are ordinary Deliveries and the check below runs at
    /// run time. What it buys is that there is one place to look to see whether the check is right.
    /// </summary>
    public sealed class RecordableDeliveries : IReadOnlyList<Delivery>
    {
        private readonly List<Delivery> deliveries;

        internal RecordableDeliveries(List<Delivery> deliveries)
        {
            this.deliveries = [.. deliveries];

            var retired = this.deliveries.Find(delivery => delivery.ArchivedOn is not null);

            if (retired is not null)
            {
                throw new ArgumentException(
                    $"Delivery {retired.Id} was retired on {retired.ArchivedOn:d} and nothing may record against it",
                    nameof(deliveries));
            }
        }

        public int Count => deliveries.Count;

        public Delivery this[int index] => deliveries[index];

        public IEnumerator<Delivery> GetEnumerator()
        {
            return deliveries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
