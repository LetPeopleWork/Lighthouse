using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.Helpers
{
    public static class CycleTimeDefinitionValidator
    {
        public const string EndStateMustComeAfterStartStateError = "End state must come after the start state in the workflow";

        public static CycleTimeDefinitionValidationResult ValidateSettings(SettingsOwnerDtoBase settings)
        {
            var resolver = new Team
            {
                ToDoStates = settings.ToDoStates,
                DoingStates = settings.DoingStates,
                DoneStates = settings.DoneStates,
                StateMappings = settings.StateMappings
                    .Select(mapping => new StateMapping { Name = mapping.Name, States = mapping.States })
                    .ToList(),
            };

            var orderedStates = resolver.DisplayStatesFor(StateCategories.ToDo)
                .Concat(resolver.DisplayStatesFor(StateCategories.Doing))
                .Concat(resolver.DisplayStatesFor(StateCategories.Done))
                .ToList();
            var errors = new List<string>();

            foreach (var definition in settings.CycleTimeDefinitions)
            {
                ValidateDefinition(definition, resolver, orderedStates, errors);
            }

            foreach (var duplicate in DuplicateNames(settings.CycleTimeDefinitions))
            {
                errors.Add($"Duplicate cycle time name '{duplicate}'.");
            }

            return new CycleTimeDefinitionValidationResult(errors);
        }

        private static void ValidateDefinition(
            CycleTimeDefinitionDto definition,
            Team resolver,
            List<string> orderedStates,
            List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add("A cycle time name is required.");
            }

            var startIndex = BoundaryIndex(resolver, orderedStates, definition.StartState);
            var endIndex = BoundaryIndex(resolver, orderedStates, definition.EndState);

            // A definition whose boundary state was removed is kept and shown as invalid rather than refused, so
            // only the end-after-start ordering is enforced here, and only when both boundaries are found.
            if (startIndex >= 0 && endIndex >= 0 && endIndex <= startIndex)
            {
                errors.Add(EndStateMustComeAfterStartStateError);
            }
        }

        private static int BoundaryIndex(Team resolver, List<string> orderedStates, string boundary)
        {
            if (string.IsNullOrWhiteSpace(boundary))
            {
                return -1;
            }

            var displayed = resolver.MapRawStateToMappedName(boundary.Trim());

            return orderedStates.FindIndex(state => WorkTrackingSystemOptionsOwner.IsSameState(state, displayed));
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
