using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>
    /// One Feature another Feature is waiting on, as a reader is handed it on the row itself: enough to
    /// name it, to open it in the work tracking system, and to say what stands against the dependency if
    /// anything does. Reasons travel as codes because every word a user reads is built in their own
    /// instance's vocabulary, and a sentence sent from here is one nobody can rename.
    /// </summary>
    public class FeatureDependsOnDto
    {
        public FeatureDependsOnDto(Feature blocker, DependencySource source, DependencyVerdict? verdict)
        {
            ReferenceId = blocker.ReferenceId;
            Name = blocker.Name;
            Url = blocker.Url;
            Source = source;
            NotHonouredReason = verdict?.Reason;
            BlockerPositionedBelow = verdict?.BlockerPositionedBelow ?? false;
        }

        private FeatureDependsOnDto(DependencySource source, DependencyVerdict? verdict)
        {
            ReferenceId = string.Empty;
            Name = string.Empty;
            Source = source;
            NotHonouredReason = verdict?.Reason;
            BlockerPositionedBelow = verdict?.BlockerPositionedBelow ?? false;
            IsWithheld = true;
        }

        /// <summary>
        /// An entry for a Feature this reader may not see. It is here rather than left out because a
        /// shorter list is a list the reader cannot tell is short - and it names nothing.
        /// </summary>
        public static FeatureDependsOnDto Withheld(DependencySource source, DependencyVerdict? verdict)
            => new(source, verdict);

        public string ReferenceId { get; }

        public string Name { get; }

        public string? Url { get; }

        public DependencySource Source { get; }

        // Absent when nothing about the dependency itself stands against it. A further code meaning "fine"
        // would read as one more thing to look into, which is the opposite of what it would be saying.
        public NotHonouredReason? NotHonouredReason { get; }

        // The one thing worth saying that is no reason to leave a dependency out: the order stays the
        // reader's, and an order that reads oddly is still one Lighthouse can work with.
        public bool BlockerPositionedBelow { get; }

        public bool IsWithheld { get; }
    }
}
