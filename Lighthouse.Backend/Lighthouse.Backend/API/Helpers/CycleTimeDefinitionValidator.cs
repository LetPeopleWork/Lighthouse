using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.Helpers
{
    public static class CycleTimeDefinitionValidator
    {
        public const string EndStateMustComeAfterStartStateError = "End state must come after the start state in the workflow";

        public static CycleTimeDefinitionValidationResult ValidateSettings(SettingsOwnerDtoBase settings, IReadOnlyCollection<CycleTimeDefinition>? storedDefinitions = null)
        {
            var lists = settings.CategoryListsAsSent();

            var orderedStates = lists.DisplayStatesInWorkflowOrder();
            var errors = new List<string>();

            foreach (var definition in settings.CycleTimeDefinitions.Where(sent => IsChanged(sent, storedDefinitions ?? [])))
            {
                ValidateDefinition(definition, lists, orderedStates, errors);
            }

            foreach (var duplicate in DuplicateNames(settings.CycleTimeDefinitions))
            {
                errors.Add($"Duplicate cycle time name '{duplicate}'.");
            }

            return new CycleTimeDefinitionValidationResult(errors);
        }

        private static void ValidateDefinition(
            CycleTimeDefinitionDto definition,
            StateCategoryLists lists,
            List<string> orderedStates,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add("A cycle time name is required.");
            }

            var startIndex = BoundaryIndex(lists, orderedStates, definition.StartState);
            var endIndex = BoundaryIndex(lists, orderedStates, definition.EndState);

            // A definition whose boundary state was removed is kept and shown as invalid rather than refused, so
            // only the end-after-start ordering is enforced here, and only when both boundaries are found.
            if (startIndex >= 0 && endIndex >= 0 && endIndex <= startIndex)
            {
                errors.Add(EndStateMustComeAfterStartStateError);
            }
        }

        // A definition stored before a state group covered both its boundaries can now order them as one position.
        // The settings page sends stored definitions back on every save, so refusing one unchanged would block all saves.
        private static bool IsChanged(CycleTimeDefinitionDto sent, IReadOnlyCollection<CycleTimeDefinition> storedDefinitions)
            => !storedDefinitions.Any(stored =>
                stored.Id == sent.Id
                && StateCategoryLists.IsSameState(stored.StartState, sent.StartState)
                && StateCategoryLists.IsSameState(stored.EndState, sent.EndState));

        private static int BoundaryIndex(StateCategoryLists lists, List<string> orderedStates, string boundary)
        {
            if (string.IsNullOrWhiteSpace(boundary))
            {
                return -1;
            }

            var displayed = lists.MapRawStateToMappedName(boundary.Trim());

            return orderedStates.FindIndex(state => StateCategoryLists.IsSameState(state, displayed));
        }

        private static IEnumerable<string> DuplicateNames(IEnumerable<CycleTimeDefinitionDto> definitions)
        {
            return definitions
                .Where(definition => !string.IsNullOrWhiteSpace(definition.Name))
                .GroupBy(definition => definition.Name.Trim(), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key);
        }
    }

    public class CycleTimeDefinitionValidationResult
    {
        public CycleTimeDefinitionValidationResult(List<string> errors)
        {
            Errors = errors;
        }

        public bool IsValid => Errors.Count == 0;

        public List<string> Errors { get; }
    }
}
