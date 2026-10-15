namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What placing one item whose links name several issues decided: under which of them it hangs, which
    /// rule settled it, and which candidates were set aside - or that it could not be placed, and why. It
    /// is a plan rather than a write, so the rule can be read and checked without touching a record.
    /// </summary>
    // SCAFFOLD: the result the placement tests read. Nothing produces one yet.
    public sealed record ParentPlacement
    {
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
    // SCAFFOLD: holds what it was given. Answering from it is not built yet.
    public sealed class StoredFeatures
    {
        private const string NotBuiltYet = "SCAFFOLD: reading the stored Features is not built yet";

        private readonly Dictionary<string, DateTime?> createdDates;

        public StoredFeatures(IReadOnlyDictionary<string, DateTime?> createdDatesByReferenceId)
        {
            createdDates = new Dictionary<string, DateTime?>(createdDatesByReferenceId, StringComparer.Ordinal);
        }

        public bool Holds(string referenceId)
            => throw new NotImplementedException($"{NotBuiltYet} ({createdDates.Count} held, asked about {referenceId})");

        public DateTime? CreatedDateOf(string referenceId)
            => throw new NotImplementedException($"{NotBuiltYet} ({createdDates.Count} held, asked about {referenceId})");
    }
}
