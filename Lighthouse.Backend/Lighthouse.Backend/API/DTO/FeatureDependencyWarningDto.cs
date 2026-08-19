using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One thing worth telling a reader about a dependency, on the row they are already looking at. It
    /// carries which dependency it is about and what is wrong with it as codes: every word a reader sees is
    /// built in their own instance's vocabulary, and a sentence sent from here is one nobody can rename.
    /// </summary>
    public class FeatureDependencyWarningDto
    {
        public FeatureDependencyWarningDto(
            string blockerReferenceId,
            string blockerName,
            NotHonouredReason? notHonouredReason,
            bool blockerPositionedBelow)
        {
            BlockerReferenceId = blockerReferenceId;
            BlockerName = blockerName;
            NotHonouredReason = notHonouredReason;
            BlockerPositionedBelow = blockerPositionedBelow;
        }

        private FeatureDependencyWarningDto(NotHonouredReason? notHonouredReason, bool blockerPositionedBelow)
        {
            BlockerReferenceId = string.Empty;
            BlockerName = string.Empty;
            NotHonouredReason = notHonouredReason;
            BlockerPositionedBelow = blockerPositionedBelow;
            IsWithheld = true;
        }

        /// <summary>
        /// A warning about a Feature this reader may not see. It still warns, because what is wrong is
        /// wrong whoever is looking, but it names nothing.
        /// </summary>
        public static FeatureDependencyWarningDto Withheld(NotHonouredReason? notHonouredReason, bool blockerPositionedBelow)
            => new(notHonouredReason, blockerPositionedBelow);

        public string BlockerReferenceId { get; }

        public string BlockerName { get; }

        public bool IsWithheld { get; }

        // Absent when nothing about the dependency itself stands against it, which is the case for the one
        // warning that is not a reason: a Feature waiting on one placed below it is untidy, not unusable.
        public NotHonouredReason? NotHonouredReason { get; }

        public bool BlockerPositionedBelow { get; }
    }
}
