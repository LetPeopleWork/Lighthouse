namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What placing one item whose links name several issues decided: under which of them it hangs, which
    /// rule settled it, and which candidates were set aside - or that it could not be placed, and why. It
    /// is a plan rather than a write, so the rule can be read and checked without touching a record.
    /// </summary>
    public sealed record ParentPlacement
    {
        public static ParentPlacement NotAsked { get; } = new();

        public static ParentPlacement NotPlacedBecause(ParentNotPlacedReason reason)
            => new() { Outcome = ParentPlacementOutcome.NotPlaced, WhyNotPlaced = reason };

        public ParentPlacementOutcome Outcome { get; init; }

        public string Key { get; init; } = string.Empty;

        public ParentPlacementRule DecidedBy { get; init; }

        public ParentNotPlacedReason WhyNotPlaced { get; init; }

        public IReadOnlyList<ParentCandidate> SetAside { get; init; } = [];
    }

    public enum ParentPlacementOutcome
    {
        NotAsked,
        Placed,
        NotPlaced,
    }

    /// <summary>
    /// Whether candidates that are not a stored Feature are set aside first. A Team's items hang under
    /// Features, so the rest can go. A Portfolio's Features hang under the level above, which is never one
    /// of its own Features, so nothing there can be set aside on that ground.
    /// </summary>
    public enum ParentNarrowing
    {
        Narrow,
        DoNotNarrow,
    }

    public enum ParentPlacementRule
    {
        None,
        OnlyFeatureAmongLinks,
        OldestLink,
        EarliestCreated,
        LowestKey,
    }

    public enum ParentNotPlacedReason
    {
        None,
        NoneIsAStoredFeature,
        MoreThanOneStoredFeature,
    }

    /// <summary>
    /// Every Feature this instance holds, by reference id, with the date it was created when that is known.
    /// Read once per fetch and only ever asked questions, so whoever places a parent cannot write through it.
    /// </summary>
    public sealed class StoredFeatures
    {
        private readonly Dictionary<string, DateTime?> createdDates;

        public StoredFeatures(IReadOnlyDictionary<string, DateTime?> createdDatesByReferenceId)
        {
            createdDates = new Dictionary<string, DateTime?>(createdDatesByReferenceId, StringComparer.Ordinal);
        }

        public bool Holds(string referenceId) => createdDates.ContainsKey(referenceId);

        /// <summary>
        /// Unknown for a Feature this instance does not hold, and for the empty created date Jira sends for
        /// some migrated issues: read at face value, that would be the earliest date there is.
        /// </summary>
        public DateTime? CreatedDateOf(string referenceId)
            => createdDates.TryGetValue(referenceId, out var created) && created != DateTime.MinValue ? created : null;
    }
}
