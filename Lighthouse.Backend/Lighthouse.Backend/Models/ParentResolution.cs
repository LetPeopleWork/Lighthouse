namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// What reading an item's links said about its parent: nothing, one issue, or several. Several is not
    /// guessed at here, because a wrongly guessed parent looks exactly like correct data. Place decides
    /// under which of them the item hangs, or says why it hangs under none.
    /// </summary>
    public readonly record struct ParentResolution
    {
        private readonly ParentCandidate[]? candidates;

        private ParentResolution(ParentCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public static ParentResolution From(IReadOnlyList<string> distinctCounterpartKeys)
            => From([.. distinctCounterpartKeys.Select(key => new ParentCandidate(key, null))]);

        public static ParentResolution From(IReadOnlyList<ParentCandidate> candidatesWithTheirLinkDates)
            => candidatesWithTheirLinkDates.Count == 0 ? default : new([.. InKeyOrder(candidatesWithTheirLinkDates)]);

        /// <summary>
        /// Jira lists links in no promised order, so the keys are sorted to make two readings of the same
        /// links one answer. Two links to the same issue are one candidate, dated by the older link.
        /// </summary>
        private static IEnumerable<ParentCandidate> InKeyOrder(IReadOnlyList<ParentCandidate> candidatesWithTheirLinkDates)
            => candidatesWithTheirLinkDates
                .GroupBy(candidate => candidate.Key, StringComparer.Ordinal)
                .Select(sameIssue => new ParentCandidate(sameIssue.Key, sameIssue.Min(candidate => candidate.LinkedAt)))
                .OrderBy(candidate => candidate.Key, StringComparer.Ordinal);

        public IReadOnlyList<ParentCandidate> CandidatesWithTheirLinkDates => candidates ?? [];

        public IReadOnlyList<string> Candidates => [.. CandidatesWithTheirLinkDates.Select(candidate => candidate.Key)];

        public bool IsResolved => CandidatesWithTheirLinkDates.Count == 1;

        public bool IsAmbiguous => CandidatesWithTheirLinkDates.Count > 1;

        public string Key => IsResolved ? CandidatesWithTheirLinkDates[0].Key : string.Empty;

        /// <summary>
        /// Where an item whose links name several issues hangs. One link or none is not a question: the
        /// caller stores it as it is. A Team's items hang under Features, so every candidate Lighthouse does
        /// not hold as a Feature is set aside first. Among what is left, the link made first wins,
        /// then the Feature created first, then the lowest key, so the same links always give the same
        /// answer and only an empty narrowing leaves the item unplaced.
        /// </summary>
        public ParentPlacement Place(StoredFeatures storedFeatures, ParentNarrowing narrowing)
        {
            if (!IsAmbiguous)
            {
                return ParentPlacement.NotAsked;
            }

            var ranked = CandidatesWithTheirLinkDates
                .Where(candidate => narrowing == ParentNarrowing.DoNotNarrow || storedFeatures.Holds(candidate.Key))
                .OrderBy(candidate => candidate.LinkedAt is null)
                .ThenBy(candidate => candidate.LinkedAt)
                .ThenBy(candidate => storedFeatures.CreatedDateOf(candidate.Key) is null)
                .ThenBy(candidate => storedFeatures.CreatedDateOf(candidate.Key))
                .ThenBy(candidate => candidate.Key, StringComparer.Ordinal)
                .ToList();

            if (ranked.Count == 0)
            {
                return ParentPlacement.NotPlacedBecause(ParentNotPlacedReason.NoneIsAStoredFeature);
            }

            var winner = ranked[0];
            var (rule, decidedOn) = ranked.Count == 1
                ? (ParentPlacementRule.OnlyFeatureAmongLinks, null)
                : WhatSetsTheWinnerApart(winner, ranked[1], storedFeatures);

            return new ParentPlacement
            {
                Outcome = ParentPlacementOutcome.Placed,
                Key = winner.Key,
                DecidedBy = rule,
                DecidedOn = decidedOn,
                SetAside = [.. CandidatesWithTheirLinkDates.Where(candidate => candidate != winner)],
            };
        }

        /// <summary>
        /// The rule named is the first one on which the winner beats the runner-up, so the line never
        /// credits a rule that did not actually decide.
        /// </summary>
        private static (ParentPlacementRule Rule, DateTime? DecidedOn) WhatSetsTheWinnerApart(
            ParentCandidate winner, ParentCandidate runnerUp, StoredFeatures storedFeatures)
        {
            if (winner.LinkedAt != runnerUp.LinkedAt)
            {
                return (ParentPlacementRule.OldestLink, winner.LinkedAt);
            }

            var createdOfTheWinner = storedFeatures.CreatedDateOf(winner.Key);

            return createdOfTheWinner != storedFeatures.CreatedDateOf(runnerUp.Key)
                ? (ParentPlacementRule.EarliestCreated, createdOfTheWinner)
                : (ParentPlacementRule.LowestKey, null);
        }

        /// <summary>
        /// Two answers naming the same keys, in any order, are the same answer: From sorts them, so they
        /// compare in one order. Left to the compiler, the comparison would be between the arrays those keys
        /// are held in, and two separately built answers would never match.
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
