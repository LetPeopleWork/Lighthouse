namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// Where one record's parent was chosen from: the key (empty when no source yielded one), the position of
    /// the source that won, and whether the choice needs the Features Lighthouse holds to be final.
    /// </summary>
    // SCAFFOLD: Story 6501. A plain value so the acceptance tests can read a choice.
    public sealed record ParentChoice(string Key, int? WinnerPosition, bool NeedsStoredFeatures);
}
