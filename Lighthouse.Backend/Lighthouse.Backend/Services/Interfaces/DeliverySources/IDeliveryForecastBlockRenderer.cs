using Lighthouse.Backend.Models.DeliverySources;

namespace Lighthouse.Backend.Services.Interfaces.DeliverySources
{
    /// <summary>
    /// Composes the published block and merges it into a description that a human also writes in.
    /// Pure - no I/O - so every marker rule is a unit test rather than an integration test.
    /// </summary>
    public interface IDeliveryForecastBlockRenderer
    {
        /// <summary>
        /// The block as it is written into the remote description. Composing it and finding it again
        /// live together on purpose: matched apart, the two can drift into a state where every write
        /// appends and the description fills up with forecasts.
        /// </summary>
        string Render(DeliveryForecastBlock block);

        /// <summary>
        /// Returns the description as it should be written back: the existing text with the block
        /// replaced in place when its markers are found intact, and the block appended otherwise.
        /// Appending is deliberate - inferring a range to delete from broken markers risks removing text
        /// Lighthouse did not write, and a visible duplicate is the better failure.
        /// </summary>
        string MergeInto(string? existingDescription, string blockText);
    }
}
