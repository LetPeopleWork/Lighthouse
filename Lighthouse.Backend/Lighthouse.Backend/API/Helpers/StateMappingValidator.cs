using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.Helpers
{
    public static class StateMappingValidator
    {
        public static StateMappingValidationResult Validate(List<StateMapping> mappings, List<string> allDirectStates)
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
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
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

            var directStateSet = new HashSet<string>(allDirectStates, StringComparer.OrdinalIgnoreCase);

            foreach (var mapping in mappings.Where(m => !string.IsNullOrWhiteSpace(m.Name) && directStateSet.Contains(m.Name)))
            {
                errors.Add($"State mapping name '{mapping.Name}' collides with a direct state name.");
            }

            return new StateMappingValidationResult(errors);
        }
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
