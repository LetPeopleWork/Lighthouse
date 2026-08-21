namespace Lighthouse.Backend.Models.Dependencies
{
    /// <summary>
    /// What was decided about one dependency: a reason not to act on it, if there is one, and separately
    /// whether the Feature waited on sits below the Feature waiting. Both come out of the same decision, so
    /// there is nowhere else to read either half from and nothing that can disagree. Position sits beside the
    /// reason rather than among them because Lighthouse never reorders anything to satisfy a dependency, so
    /// it is worth saying out loud and is not grounds for leaving the dependency out.
    /// </summary>
    public sealed class DependencyVerdict
    {
        public DependencyVerdict(
            string dependentReferenceId,
            string blockerReferenceId,
            NotHonouredReason? reason,
            bool blockerPositionedBelow)
        {
            DependentReferenceId = dependentReferenceId;
            BlockerReferenceId = blockerReferenceId;
            Reason = reason;
            BlockerPositionedBelow = blockerPositionedBelow;
        }

        public string DependentReferenceId { get; }

        public string BlockerReferenceId { get; }

        public NotHonouredReason? Reason { get; }

        public bool BlockerPositionedBelow { get; }

        public bool IsHonoured => Reason is null;

        public bool HasNothingWrongWithIt => IsHonoured && !BlockerPositionedBelow;

        /// <summary>
        /// Whether a reader should be told about this. Setting a Portfolio's dependencies aside is a choice
        /// somebody made, not a broken link, and a warning on every Feature in that Portfolio would teach
        /// them to stop reading the column - so it is the one reason that says nothing on the row. The list
        /// still names it, per entry, where the reader went looking for it.
        /// </summary>
        public bool IsWorthWarningAbout => Reason != NotHonouredReason.IgnoredByPortfolio && !HasNothingWrongWithIt;
    }
}
