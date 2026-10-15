namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What reading an item's links said about its parent: nothing, one issue, or several. Several is kept
    /// as an answer of its own, because a wrongly guessed parent looks exactly like correct data.
    /// </summary>
    public readonly record struct ParentResolution
    {
        private const string NotBuiltYet = "SCAFFOLD: placing an item among several candidates is not built yet";

        private readonly string[]? candidates;

        private ParentResolution(string[] candidates)
        {
            this.candidates = candidates;
        }

        public static ParentResolution From(IReadOnlyList<string> distinctCounterpartKeys)
            => distinctCounterpartKeys.Count == 0 ? default : new([.. distinctCounterpartKeys]);

        // SCAFFOLD: candidates that carry the date their link was made.
        public static ParentResolution From(IReadOnlyList<ParentCandidate> candidatesWithTheirLinkDates)
            => throw new NotImplementedException($"{NotBuiltYet} ({candidatesWithTheirLinkDates.Count} candidates)");

        public IReadOnlyList<string> Candidates => candidates ?? [];

        public bool IsResolved => Candidates.Count == 1;

        public bool IsAmbiguous => Candidates.Count > 1;

        public string Key => IsResolved ? Candidates[0] : string.Empty;

        // SCAFFOLD: the pick among several candidates, against the Features this instance holds.
        public ParentPlacement Place(StoredFeatures storedFeatures, ParentNarrowing narrowing)
            => throw new NotImplementedException($"{NotBuiltYet} ({Candidates.Count} candidates, {narrowing}, {storedFeatures})");

        /// <summary>
        /// Two answers naming the same keys, in any order, are the same answer. Left to the compiler, the
        /// comparison would be between the arrays those keys are held in, and two separately built answers
        /// would never match.
        /// </summary>
        // SCAFFOLD: From must canonicalise the keys (ordinal sort, distinct) so Equals and GetHashCode become order-free.
        public bool Equals(ParentResolution other) => Candidates.SequenceEqual(other.Candidates);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var candidate in Candidates)
            {
                hash.Add(candidate);
            }

            return hash.ToHashCode();
        }
    }
}
