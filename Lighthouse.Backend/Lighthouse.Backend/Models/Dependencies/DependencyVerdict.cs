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
    }
}
