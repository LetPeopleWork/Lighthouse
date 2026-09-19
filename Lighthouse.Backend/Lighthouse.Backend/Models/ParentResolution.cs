namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What reading an item's links said about its parent: nothing, one issue, or several issues with
    /// nothing to choose between them.
    ///
    /// An item has at most one parent, so several candidates cannot be narrowed down by keeping one and
    /// dropping the rest. A wrongly chosen parent moves work under something it does not belong to,
    /// corrupts the size of that thing and every forecast drawn from it, and looks exactly like correct
    /// data - so the several-candidates case is carried as an answer of its own, naming the keys that
    /// caused it, rather than collapsed into a guess or into silence.
    /// </summary>
    public readonly record struct ParentResolution
    {
        private readonly string[]? candidates;

        private ParentResolution(string[] candidates)
        {
            this.candidates = candidates;
        }

        public static ParentResolution From(IReadOnlyList<string> distinctCounterpartKeys)
            => distinctCounterpartKeys.Count == 0 ? default : new([.. distinctCounterpartKeys]);

        public IReadOnlyList<string> Candidates => candidates ?? [];

        public bool IsResolved => Candidates.Count == 1;

        public bool IsAmbiguous => Candidates.Count > 1;

        public string Key => IsResolved ? Candidates[0] : string.Empty;

        /// <summary>
        /// Two answers naming the same keys are the same answer. Left to the compiler, the comparison
        /// would be between the arrays those keys are held in, and two separately built answers would
        /// never match.
        /// </summary>
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
