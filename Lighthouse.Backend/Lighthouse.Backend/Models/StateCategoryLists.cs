namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// A To Do, Doing and Done list read against a set of mappings. Owners read their own lists through it, and a
    /// validator can read the lists of a settings payload through it without building an owner to hold them.
    /// </summary>
    public sealed class StateCategoryLists(
        IReadOnlyList<string> toDoStates,
        IReadOnlyList<string> doingStates,
        IReadOnlyList<string> doneStates,
        IReadOnlyList<StateMapping> stateMappings)
    {
        private static readonly StateCategories[] ListedCategories = [StateCategories.ToDo, StateCategories.Doing, StateCategories.Done];

        /// <summary>The stored list of a category, as it was given.</summary>
        public IReadOnlyList<string> StoredStatesOf(StateCategories category) => category switch
        {
            StateCategories.ToDo => toDoStates,
            StateCategories.Doing => doingStates,
            StateCategories.Done => doneStates,
            _ => [],
        };

        /// <summary>The To Do, Doing and Done lists in that order, each as it was given.</summary>
        public IEnumerable<(StateCategories Category, IReadOnlyList<string> States)> StoredLists()
            => ListedCategories.Select(category => (category, StoredStatesOf(category)));

        /// <summary>The category's raw states as the charts show them: each state replaced by its mapping's name, the group at its first member's position.</summary>
        public List<string> DisplayStatesFor(StateCategories category)
            => WithoutRepeats(NormalisedStatesFor(StoredStatesOf(category)).Select(MapRawStateToMappedName));

        /// <summary>The category of a raw tracker state, read through the normaliser, so a list that still holds a group's name answers for the group's states too.</summary>
        public StateCategories CategoryOfRawState(string rawState)
            => ListedCategories.FirstOrDefault(
                category => NormalisedStatesFor(StoredStatesOf(category)).Exists(state => IsSameState(state, rawState)),
                StateCategories.Unknown);

        /// <summary>
        /// The category of a name an item or a stored transition carries: a raw state, or a mapping's name.
        /// A mapping is judged by the states of it that are listed; when they disagree, or none is listed, it is Unknown.
        /// </summary>
        public StateCategories CategoryOfDisplayName(string displayName)
        {
            var group = GroupNamed(displayName);

            if (group == null)
            {
                return CategoryOfRawState(displayName);
            }

            var listedCategories = MembersOf(group)
                .Select(CategoryOfRawState)
                .Where(category => category != StateCategories.Unknown)
                .Distinct()
                .ToList();

            return listedCategories.Count == 1 ? listedCategories[0] : StateCategories.Unknown;
        }

        /// <summary>A list with every mapping name replaced by the mapping's raw states, in order; a list of raw states passes through unchanged.</summary>
        public List<string> NormalisedStatesFor(IReadOnlyList<string> storedList)
        {
            var entries = storedList.Where(entry => !string.IsNullOrWhiteSpace(entry)).ToList();
            var expanded = entries.SelectMany(entry => ExpandListEntry(entries, entry));

            return WithoutRepeats(expanded);
        }

        private IEnumerable<string> ExpandListEntry(List<string> list, string entry)
        {
            var kind = ClassifyListEntry(list, entry);

            if (kind == ListEntryKind.NotAGroupName)
            {
                return [entry];
            }

            var members = MembersOf(GroupNamed(entry)!);

            if (kind == ListEntryKind.StateOfItsOwnMapping)
            {
                return members.Where(state => !list.Exists(listed => IsSameState(listed, state))).Prepend(entry);
            }

            return members;
        }

        /// <summary>Whether a list entry is a state of the tracker or stands for a mapping's states, judged against the list it sits in.</summary>
        public ListEntryKind ClassifyListEntry(IReadOnlyList<string> list, string entry)
        {
            var group = GroupNamed(entry);

            if (group == null)
            {
                return ListEntryKind.NotAGroupName;
            }

            var members = MembersOf(group);

            if (members.Exists(state => IsSameState(state, entry)))
            {
                return ListEntryKind.StateOfItsOwnMapping;
            }

            // A name that is also a state grouped by another mapping is read as that state, so a chained
            // mapping expands one level once and then stays put.
            if (GroupContaining(entry) != null)
            {
                return ListEntryKind.NotAGroupName;
            }

            return members.Exists(state => list.Any(listed => IsSameState(listed, state)))
                ? ListEntryKind.Shadowing
                : ListEntryKind.CollapsedGroup;
        }

        /// <summary>The name of the mapping that groups a raw state, or the state itself when no mapping groups it.</summary>
        public string MapRawStateToMappedName(string rawState)
            => GroupContaining(rawState)?.Name ?? rawState;

        /// <summary>The states a mapping groups, read by its name; a name no mapping carries stands for itself.</summary>
        public List<string> RawStatesOf(IEnumerable<string> entries)
            => [.. entries.SelectMany(entry => stateMappings.FirstOrDefault(mapping => IsSameState(mapping.Name, entry)) is { } mapping
                ? MembersOf(mapping)
                : [entry])];

        /// <summary>The mapping a list entry names, when that mapping groups at least one state.</summary>
        public StateMapping? GroupNamed(string entry)
            => stateMappings.FirstOrDefault(mapping => IsSameState(mapping.Name, entry) && MembersOf(mapping).Count > 0);

        private StateMapping? GroupContaining(string rawState)
            => stateMappings.FirstOrDefault(mapping => MembersOf(mapping).Exists(member => IsSameState(member, rawState)));

        /// <summary>The states a mapping groups. A mapping can come back from storage with no states, or with null and blank ones; those name no state of the tracker.</summary>
        public static List<string> MembersOf(StateMapping mapping)
            => mapping.States?.Where(state => !string.IsNullOrWhiteSpace(state)).ToList() ?? [];

        // Trackers and users pad and recase state names freely, so case and surrounding whitespace never tell two states apart.
        public static bool IsSameState(string? left, string? right)
            => string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);

        private static List<string> WithoutRepeats(IEnumerable<string> states)
            => [.. states.DistinctBy(state => state.Trim(), StringComparer.OrdinalIgnoreCase)];
    }
}
