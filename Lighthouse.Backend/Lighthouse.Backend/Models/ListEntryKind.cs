namespace Lighthouse.Backend.Models
{
    /// <summary>What an entry of a To Do, Doing or Done list is, judged against the owner's mappings and the list it sits in.</summary>
    public enum ListEntryKind
    {
        /// <summary>Not the name of any mapping, or the name of a mapping that has no states (null or empty): a state of the tracker.</summary>
        NotAGroupName,

        /// <summary>
        /// Equals the name of a mapping and is one of that mapping's own states: a state of the tracker. This wins over
        /// the two kinds below, so the entry is never a collapsed group or shadowing, even when other states of the
        /// same mapping are listed beside it.
        /// </summary>
        StateOfItsOwnMapping,

        /// <summary>Equals the name of a mapping that has states, the name is not one of that mapping's own states, and none of its states is listed in the same list.</summary>
        CollapsedGroup,

        /// <summary>Equals the name of a mapping that has states, the name is not one of that mapping's own states, and at least one of its states is listed in the same list. A state listed only in another category's list does not count.</summary>
        Shadowing,
    }
}
