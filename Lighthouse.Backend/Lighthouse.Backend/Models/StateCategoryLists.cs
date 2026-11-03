namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// A To Do, Doing and Done list read against a set of mappings. Owners read their own lists through it, and a
    /// validator can read the lists of a settings payload through it without building an owner to hold them.
    /// It reads the lists once, when first asked, and answers every later question from that reading, so hold one
    /// for as long as the lists stay as they are (a refresh, a request), and take a new one after changing them.
    /// </summary>
    public sealed class StateCategoryLists(
        IReadOnlyList<string> toDoStates,
        IReadOnlyList<string> doingStates,
        IReadOnlyList<string> doneStates,
        IReadOnlyList<StateMapping> stateMappings)
    {
        private static readonly StateCategories[] ListedCategories = [StateCategories.ToDo, StateCategories.Doing, StateCategories.Done];

        private readonly Dictionary<StateCategories, List<string>> normalisedLists = [];

        private List<string>[]? membersByMapping;

        private Dictionary<string, StateCategories>? categoryByRawState;

        private Dictionary<string, StateMapping>? groupByMember;

        // Aligned with the mappings by position, so a mapping is looked up without building its member list again.
        private List<string>[] MembersByMapping => membersByMapping ??= [.. stateMappings.Select(MembersOf)];

        // Keys are trimmed and compared ignoring case, which is how IsSameState tells two states apart. The first
        // category that lists a state answers for it, as a scan through the lists in order would.
        private Dictionary<string, StateCategories> CategoryByRawState => categoryByRawState ??= ReadCategoryByRawState();

        // The first mapping that groups a state answers for it, as a scan through the mappings in order would.
        private Dictionary<string, StateMapping> GroupByMember => groupByMember ??= ReadGroupByMember();

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
            => WithoutRepeats(NormalisedStatesOf(category).Select(MapRawStateToMappedName));

        /// <summary>The category's stored list with every mapping name replaced by the mapping's raw states, in order.</summary>
        public IReadOnlyList<string> NormalisedStatesOf(StateCategories category)
        {
            if (!normalisedLists.TryGetValue(category, out var normalised))
            {
                normalised = NormalisedStatesFor(StoredStatesOf(category));
                normalisedLists[category] = normalised;
            }

            return normalised;
        }

        /// <summary>The To Do, Doing and Done lists as the charts show them, one after the other in that order, no state twice.</summary>
        public List<string> DisplayStatesInWorkflowOrder()
            => WithoutRepeats(ListedCategories.SelectMany(DisplayStatesFor));

        /// <summary>
        /// The entry of the workflow as the charts show it that a cycle-time boundary stands for, written as the list
        /// writes it, or null when the boundary stands for none: a state a mapping groups stands for the mapping.
        /// </summary>
        public string? WorkflowEntryFor(IReadOnlyList<string> statesInOrder, string? boundary)
        {
            if (string.IsNullOrWhiteSpace(boundary))
            {
                return null;
            }

            var displayed = MapRawStateToMappedName(boundary.Trim());
            return statesInOrder.FirstOrDefault(state => IsSameState(state, displayed));
        }

        /// <summary>The category of a raw tracker state, read through the normaliser, so a list that still holds a group's name answers for the group's states too.</summary>
        public StateCategories CategoryOfRawState(string rawState)
            => rawState is not null && CategoryByRawState.TryGetValue(rawState.Trim(), out var category)
                ? category
                : StateCategories.Unknown;

        /// <summary>
        /// The category of a name an item or a stored transition carries: a raw state, or a mapping's name.
        /// A mapping is judged by the states of it that are listed; when they disagree, or none is listed, it is Unknown.
        /// </summary>
        public StateCategories CategoryOfDisplayName(string displayName)
        {
            var group = IndexOfGroupNamed(displayName);

            if (group < 0)
            {
                return CategoryOfRawState(displayName);
            }

            var listedCategories = MembersByMapping[group]
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

            var members = MembersByMapping[IndexOfGroupNamed(entry)];

            if (kind == ListEntryKind.StateOfItsOwnMapping)
            {
                return members.Where(state => !list.Exists(listed => IsSameState(listed, state))).Prepend(entry);
            }

            return members;
        }

        /// <summary>Whether a list entry is a state of the tracker or stands for a mapping's states, judged against the list it sits in.</summary>
        public ListEntryKind ClassifyListEntry(IReadOnlyList<string> list, string entry)
        {
            var group = IndexOfGroupNamed(entry);

            if (group < 0)
            {
                return ListEntryKind.NotAGroupName;
            }

            var members = MembersByMapping[group];

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

        /// <summary>
        /// A shape of these lists that older versions read in their own way, or null when there is none. A mapping
        /// with no states is reported first, because no reading of it can be rewritten as raw states.
        /// </summary>
        public LegacyListShape? LegacyShape()
        {
            if (Array.Exists(MembersByMapping, members => members.Count == 0))
            {
                return LegacyListShape.EmptyGroup;
            }

            var entries = StoredLists()
                .SelectMany(stored => stored.States.Select(entry => (Entry: entry, Kind: ClassifyListEntry(stored.States, entry))))
                .ToList();

            if (entries.Exists(classified => classified.Kind == ListEntryKind.Shadowing))
            {
                return LegacyListShape.Shadowing;
            }

            return entries.Exists(classified => classified.Kind != ListEntryKind.NotAGroupName && GroupsAnotherGroup(classified.Entry))
                ? LegacyListShape.Chained
                : null;
        }

        private bool GroupsAnotherGroup(string groupName)
            => MembersByMapping[IndexOfGroupNamed(groupName)].Exists(member => !IsSameState(member, groupName) && IndexOfGroupNamed(member) >= 0);

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
        {
            var group = IndexOfGroupNamed(entry);
            return group < 0 ? null : stateMappings[group];
        }

        private int IndexOfGroupNamed(string entry)
        {
            for (var index = 0; index < stateMappings.Count; index++)
            {
                if (IsSameState(stateMappings[index].Name, entry) && MembersByMapping[index].Count > 0)
                {
                    return index;
                }
            }

            return -1;
        }

        private StateMapping? GroupContaining(string rawState)
            => rawState is not null && GroupByMember.TryGetValue(rawState.Trim(), out var group) ? group : null;

        private Dictionary<string, StateCategories> ReadCategoryByRawState()
        {
            var lookup = new Dictionary<string, StateCategories>(StringComparer.OrdinalIgnoreCase);
            foreach (var category in ListedCategories)
            {
                foreach (var state in NormalisedStatesOf(category))
                {
                    lookup.TryAdd(state.Trim(), category);
                }
            }

            return lookup;
        }

        private Dictionary<string, StateMapping> ReadGroupByMember()
        {
            var lookup = new Dictionary<string, StateMapping>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < stateMappings.Count; index++)
            {
                foreach (var member in MembersByMapping[index])
                {
                    lookup.TryAdd(member.Trim(), stateMappings[index]);
                }
            }

            return lookup;
        }

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
