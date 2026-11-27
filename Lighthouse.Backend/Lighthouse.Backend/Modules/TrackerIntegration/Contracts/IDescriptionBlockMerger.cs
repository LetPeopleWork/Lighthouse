namespace Lighthouse.Backend.Modules.TrackerIntegration.Contracts
{
    /// <summary>
    /// Merges a block of text into a description a human also writes in. Tracker integration owns this port so a
    /// connector can write the block without depending on the module that composes it; that module implements it.
    /// </summary>
    public interface IDescriptionBlockMerger
    {
        /// <summary>
        /// Returns the description as it should be written back: the existing text with the block
        /// replaced in place when its markers are found intact, and the block appended otherwise.
        /// Appending is deliberate - inferring a range to delete from broken markers risks removing text
        /// Lighthouse did not write, and a visible duplicate is the better failure.
        /// </summary>
        string MergeInto(string? existingDescription, string blockText);
    }
}
