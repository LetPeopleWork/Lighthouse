using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.Helpers
{
    public static class StateMappingValidator
    {
        private const string OneCategoryAdvice = "Keep a mapping's states in one category.";

        public static StateMappingValidationResult ValidateSettings(SettingsOwnerDtoBase settings, IReadOnlyCollection<StateMapping>? storedMappings = null)
        {
            var mappings = settings.StateMappingsAsSent();
            var lists = new StateCategoryLists(settings.ToDoStates, settings.DoingStates, settings.DoneStates, mappings);

            var errors = Validate(mappings).Errors;
            errors.AddRange(ShadowingErrors(lists));
            errors.AddRange(ChainErrors(mappings));
            errors.AddRange(mappings
                .Where(mapping => IsChanged(mapping, storedMappings ?? []))
                .SelectMany(mapping => CategoryErrors(lists, mapping)));

            return new StateMappingValidationResult(errors);
        }

        public static StateMappingValidationResult Validate(List<StateMapping> mappings)
        {
            var errors = new List<string>();

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrWhiteSpace(mapping.Name))
                {
                    errors.Add("A state mapping name is required.");
                }

                if (mapping.States.Count == 0)
                {
                    errors.Add($"State mapping '{mapping.Name}' must contain at least one state.");
                }
            }

            var duplicateNames = mappings
                .Where(m => !string.IsNullOrWhiteSpace(m.Name))
                .GroupBy(m => m.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicateName in duplicateNames)
            {
                errors.Add($"Duplicate state mapping name '{duplicateName}'.");
            }

            var allSourceStates = mappings
                .SelectMany(m => m.States.Select(s => new { MappingName = m.Name, State = s }))
                .ToList();

            var duplicateSourceStates = allSourceStates
                .GroupBy(x => x.State, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var duplicate in duplicateSourceStates)
            {
                var involvedMappings = string.Join(", ", duplicate.Select(d => $"'{d.MappingName}'").Distinct());
                errors.Add($"Source state '{duplicate.Key}' appears in multiple mappings: {involvedMappings}.");
            }

            return new StateMappingValidationResult(errors);
        }

        private static IEnumerable<string> ShadowingErrors(StateCategoryLists lists)
            => ListsOf(lists).SelectMany(list => list.States
                .Where(entry => !string.IsNullOrWhiteSpace(entry))
                .Where(entry => lists.ClassifyListEntry(list.States, entry) == ListEntryKind.Shadowing)
                .Select(entry => ShadowingSentence(lists, list.Category, entry)));

        private static string ShadowingSentence(StateCategoryLists lists, StateCategories category, string entry)
        {
            var name = lists.GroupNamed(entry)!.Name.Trim();
            var state = entry.Trim();

            // In Doing the user may have meant to group the state too, so the settings page offers that fix there.
            return category == StateCategories.Doing
                ? $"Mapping \"{name}\" has the same name as the Doing state \"{state}\". Add \"{name}\" to this mapping to group it, or choose another name."
                : $"Mapping \"{name}\" has the same name as the {LabelOf(category)} state \"{state}\". Choose another name.";
        }

        private static IEnumerable<string> ChainErrors(List<StateMapping> mappings)
            => from named in mappings
               where !string.IsNullOrWhiteSpace(named.Name)
               from grouping in mappings
               where !ReferenceEquals(named, grouping) && StatesOf(grouping).Exists(state => StateCategoryLists.IsSameState(state, named.Name))
               select $"Mapping \"{named.Name.Trim()}\" has the same name as a state grouped by mapping \"{grouping.Name.Trim()}\". Choose another name.";

        // A group stored before this rule existed may span categories; refusing it unchanged would block every unrelated save.
        private static bool IsChanged(StateMapping mapping, IReadOnlyCollection<StateMapping> storedMappings)
            => !storedMappings.Any(stored =>
                StateCategoryLists.IsSameState(stored.Name, mapping.Name)
                && HoldTheSameStates(StatesOf(stored), StatesOf(mapping)));

        private static bool HoldTheSameStates(List<string> left, List<string> right)
            => left.TrueForAll(state => right.Exists(other => StateCategoryLists.IsSameState(state, other)))
               && right.TrueForAll(state => left.Exists(other => StateCategoryLists.IsSameState(state, other)));

        private static IEnumerable<string> CategoryErrors(StateCategoryLists lists, StateMapping mapping)
        {
            var normalisedLists = ListsOf(lists)
                .Select(list => (list.Category, States: lists.NormalisedStatesFor(list.States)))
                .ToList();

            var placed = StatesOf(mapping)
                .Select(state => new PlacedState(state.Trim(), normalisedLists
                    .Where(list => list.States.Exists(listed => StateCategoryLists.IsSameState(listed, state)))
                    .Select(list => list.Category)
                    .ToList()))
                .Where(entry => entry.Categories.Count > 0)
                .ToList();

            var name = mapping.Name.Trim();
            var inSeveral = placed.Find(entry => entry.Categories.Count > 1);

            if (inSeveral != null)
            {
                return [$"Mapping \"{name}\" groups \"{inSeveral.State}\", which is in {ListedCategories(inSeveral.Categories)}. {OneCategoryAdvice}"];
            }

            var first = placed.FirstOrDefault();
            var other = placed.Find(entry => entry.Categories[0] != first?.Categories[0]);

            if (first == null || other == null)
            {
                return [];
            }

            return [$"Mapping \"{name}\" groups states from different categories: \"{first.State}\" is in {LabelOf(first.Categories[0])} and \"{other.State}\" is in {LabelOf(other.Categories[0])}. {OneCategoryAdvice}"];
        }

        private static string ListedCategories(List<StateCategories> categories)
        {
            var labels = categories.Select(LabelOf).ToList();

            return labels.Count == 2
                ? $"both {labels[0]} and {labels[1]}"
                : $"{string.Join(", ", labels[..^1])} and {labels[^1]}";
        }

        private static List<string> StatesOf(StateMapping mapping)
            => mapping.States?.Where(state => !string.IsNullOrWhiteSpace(state)).ToList() ?? [];

        private static IEnumerable<(StateCategories Category, IReadOnlyList<string> States)> ListsOf(StateCategoryLists lists)
            => new[] { StateCategories.ToDo, StateCategories.Doing, StateCategories.Done }
                .Select(category => (category, lists.StoredStatesOf(category)));

        private static string LabelOf(StateCategories category) => category switch
        {
            StateCategories.ToDo => "To Do",
            StateCategories.Doing => "Doing",
            _ => "Done",
        };

        private sealed record PlacedState(string State, List<StateCategories> Categories);
    }

    public class StateMappingValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        public List<string> Errors { get; }

        public StateMappingValidationResult(List<string> errors)
        {
            Errors = errors;
        }
    }
}
