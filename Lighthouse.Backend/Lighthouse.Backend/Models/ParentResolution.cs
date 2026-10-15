namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What reading an item's links said about its parent: nothing, one issue, or several. Several is kept
    /// as an answer of its own, because a wrongly guessed parent looks exactly like correct data.
    /// </summary>
    public readonly record struct ParentResolution
    {
        private const string NotBuiltYet = "SCAFFOLD: placing an item among several candidates is not built yet";

        private readonly ParentCandidate[]? candidates;

        private ParentResolution(ParentCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public static ParentResolution From(IReadOnlyList<string> distinctCounterpartKeys)
            => From([.. distinctCounterpartKeys.Select(key => new ParentCandidate(key, null))]);

        // SCAFFOLD: candidates that carry the date their link was made.
        public static ParentResolution From(IReadOnlyList<ParentCandidate> candidatesWithTheirLinkDates)
            => candidatesWithTheirLinkDates.Count == 0 ? default : new([.. candidatesWithTheirLinkDates]);

        public IReadOnlyList<ParentCandidate> CandidatesWithTheirLinkDates => candidates ?? [];

        public IReadOnlyList<string> Candidates => [.. CandidatesWithTheirLinkDates.Select(candidate => candidate.Key)];

        public bool IsResolved => Candidates.Count == 1;

        public bool IsAmbiguous => Candidates.Count > 1;

        public string Key => IsResolved ? Candidates[0] : string.Empty;

        /// <summary>
        /// Where an item whose links name several issues hangs. A Team's items hang under Features, so every
        /// candidate this instance does not hold as a Feature is set aside first, and exactly one left over
        /// is the answer.
        /// </summary>
        public ParentPlacement Place(StoredFeatures storedFeatures, ParentNarrowing narrowing)
        {
            if (narrowing == ParentNarrowing.DoNotNarrow)
            {
                throw new NotImplementedException($"{NotBuiltYet} ({Candidates.Count} candidates, {narrowing})");
            }

            var features = CandidatesWithTheirLinkDates.Where(candidate => storedFeatures.Holds(candidate.Key)).ToList();

            return features.Count switch
            {
                0 => ParentPlacement.NotPlacedBecause(ParentNotPlacedReason.NoneIsAStoredFeature),
                1 => new ParentPlacement
                {
                    Outcome = ParentPlacementOutcome.Placed,
                    Key = features[0].Key,
                    DecidedBy = ParentPlacementRule.OnlyFeatureAmongLinks,
                    SetAside = [.. CandidatesWithTheirLinkDates.Where(candidate => candidate != features[0])],
                },
                _ => ParentPlacement.NotPlacedBecause(ParentNotPlacedReason.MoreThanOneStoredFeature),
            };
        }

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
