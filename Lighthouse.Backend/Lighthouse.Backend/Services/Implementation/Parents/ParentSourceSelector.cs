using Lighthouse.Backend.Extensions;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Implementation.Parents
{
    /// <summary>
    /// Decides where one record's parent was read from.
    ///
    /// A Team or a Portfolio that names something in Parent Override Field is declaring it authoritative,
    /// so the parent the tracker reports for itself is not consulted at all while it is set. What was
    /// named is either a field of the tracker's or one of its link types, and those are read in entirely
    /// different places - which is the whole of what this decides.
    ///
    /// This lives apart from any one tracker because the rule belongs to the Team and the Portfolio rather
    /// than to the tracker, and apart from the sibling that decides the same thing for dependencies
    /// because the two encode different knowledge: a record waits on any number of things, only a
    /// Portfolio has them, and a bad entry among them is skipped. A record hangs under at most one thing,
    /// both grains have one, and several candidates are refused rather than narrowed.
    ///
    /// It sits here rather than beside the work item services because a connector has to call it, and a
    /// connector is a leaf input adapter that is not allowed to reach into those - a boundary the
    /// architecture tests enforce.
    /// </summary>
    public static class ParentSourceSelector
    {
        /// <summary>
        /// Whether this Team or Portfolio still reads the parent the tracker reports for itself. Asked by
        /// anything that has to behave differently once the override is set - what to fetch as well as
        /// what to read it from - so that those answers cannot drift apart from what is actually read.
        /// </summary>
        public static bool ReadsTheTrackersOwnParent(IWorkItemQueryOwner owner)
            => !owner.ParentOverrideAdditionalFieldDefinitionId.HasValue;

        /// <summary>
        /// What one record hangs under, or nothing when the override yielded nothing - in which case the
        /// caller is left holding whatever the tracker reported for itself, exactly as before.
        ///
        /// The caller supplies what the tracker's links said; whether that is used is decided here.
        /// </summary>
        public static string TheParentOf(
            IWorkItemQueryOwner owner, WorkItemBase workItem, ParentSource source, ParentResolution fromTheMatchingLinks)
        {
            if (ReadsTheTrackersOwnParent(owner))
            {
                return string.Empty;
            }

            if (source == ParentSource.ALinkTypeTheOverrideNames)
            {
                // Several issues answering the same link type name none of them, because a record hangs
                // under one thing and there is nothing to choose between them. Keeping one would move work
                // under something it does not belong to, corrupt the size of that thing and every forecast
                // drawn from it, and look exactly like correct data.
                return fromTheMatchingLinks.Key;
            }

            return workItem.GetAdditionalFieldValue(owner.ParentOverrideAdditionalFieldDefinitionId) ?? string.Empty;
        }
    }

    /// <summary>
    /// The two things an administrator can leave in Parent Override Field. The box takes a name and says
    /// nothing about which of the two it turned out to be, so whoever resolved it against the tracker has
    /// to say, rather than every reader guessing from the shape of what came back.
    /// </summary>
    public enum ParentSource
    {
        AFieldTheOverrideNames,
        ALinkTypeTheOverrideNames,
    }
}
