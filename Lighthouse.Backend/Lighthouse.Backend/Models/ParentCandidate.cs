namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// One issue an item's links name as somewhere to hang it, with the moment the link that still stands
    /// was made. The moment is missing when the tracker never recorded one, which is the usual case for a
    /// link made by an import, a migration or an automation.
    /// </summary>
    // SCAFFOLD: the shape the placement tests read. Nothing builds one yet.
    public readonly record struct ParentCandidate(string Key, DateTime? LinkedAt);
}
