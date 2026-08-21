using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Dependencies;

namespace Lighthouse.Backend.Services.Implementation.Dependencies
{
    /// <summary>
    /// Decides where one Feature's dependencies were read from, and turns them into references.
    ///
    /// A Portfolio that names a field is declaring that field authoritative, so the tracker's own link is
    /// not consulted at all while it is set - the same posture the parent setting beside it takes.
    ///
    /// This lives apart from any one tracker because the rule belongs to the Portfolio rather than to the
    /// tracker. Written inside a tracker, it is a rule the next tracker has to be told about; the first
    /// time that was done it was told to one of three, and the other two accepted the setting and ignored
    /// it, which reads from the outside exactly like a field everyone left empty.
    ///
    /// Nothing here has been saved yet, so every reference names Feature nought until the reconciler keys
    /// it to the row the Feature lands on.
    /// </summary>
    public static class DependencySourceSelector
    {
        private const int TheFeatureHasNoRowYet = 0;

        /// <summary>
        /// Whether this Portfolio still reads the tracker's own link. Asked by anything that has to behave
        /// differently once a field is named - what to fetch, and what is worth reporting about links
        /// nobody consulted - so that those answers cannot drift apart from what is actually read.
        /// </summary>
        public static bool ReadsTheTrackersOwnLink(Portfolio portfolio)
            => !portfolio.DependencyOverrideAdditionalFieldDefinitionId.HasValue;

        /// <summary>
        /// What one Feature waits on, for a tracker that can serve a field a Portfolio names. The caller
        /// supplies the references it found on the tracker's own link; whether those are used is decided
        /// here.
        /// </summary>
        public static List<FeatureDependencyReference> TheDependenciesOf(
            Portfolio portfolio, WorkItemBase workItem, IReadOnlyList<string> fromTheTrackersOwnLink)
        {
            if (ReadsTheTrackersOwnLink(portfolio))
            {
                return AsReferences(fromTheTrackersOwnLink, DependencySource.TrackerLink);
            }

            var typedIntoTheField = workItem.GetAdditionalFieldValue(portfolio.DependencyOverrideAdditionalFieldDefinitionId);

            return AsReferences(DependencyFieldReferences.In(typedIntoTheField), DependencySource.PortfolioField);
        }

        /// <summary>
        /// What one Feature waits on, for a tracker that cannot serve a named field at all - it exposes no
        /// fields of its own for a Portfolio to point at, so there is nothing for the setting to read.
        ///
        /// Such a tracker keeps reading its own links whatever the setting says. Treating the setting as
        /// authoritative here would hand the Portfolio an empty field and drop every link it does have,
        /// which is a worse answer than ignoring a setting that cannot be honoured. Named rather than
        /// implied, so that a tracker opting out of the choice has to say it is doing so.
        /// </summary>
        public static List<FeatureDependencyReference> TheTrackersOwnLinksOnly(IReadOnlyList<string> fromTheTrackersOwnLink)
            => AsReferences(fromTheTrackersOwnLink, DependencySource.TrackerLink);

        private static List<FeatureDependencyReference> AsReferences(IReadOnlyList<string> references, DependencySource source)
        {
            var asReferences = new List<FeatureDependencyReference>(references.Count);

            foreach (var reference in references)
            {
                asReferences.Add(new FeatureDependencyReference(TheFeatureHasNoRowYet, reference, source));
            }

            return asReferences;
        }
    }
}
