using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.API.Helpers
{
    public static class WriteBackMappingValidator
    {
        private static readonly HashSet<WriteBackValueSource> ForecastSources =
        [
            WriteBackValueSource.ForecastPercentile50,
            WriteBackValueSource.ForecastPercentile70,
            WriteBackValueSource.ForecastPercentile85,
            WriteBackValueSource.ForecastPercentile95,
        ];

        public static WriteBackMappingValidationResult Validate(List<WriteBackMappingDefinition> mappings)
        {
            var errors = new List<string>();

            foreach (var mapping in mappings)
            {
                if (string.IsNullOrEmpty(mapping.TargetFieldReference))
                {
                    errors.Add("TargetFieldReference is required for every write-back mapping.");
                }

                if (ForecastSources.Contains(mapping.ValueSource) &&
                    mapping.TargetValueType == WriteBackTargetValueType.FormattedText &&
                    string.IsNullOrEmpty(mapping.DateFormat))
                {
                    errors.Add($"DateFormat is required when TargetValueType is FormattedText for forecast sources (field: '{mapping.TargetFieldReference}').");
                }
            }

            var duplicates = mappings
                .Where(m => !string.IsNullOrEmpty(m.TargetFieldReference))
                .GroupBy(m => m.TargetFieldReference, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate TargetFieldReference '{duplicate}' found. Each mapping must target a unique field.");
            }

            return new WriteBackMappingValidationResult(errors);
        }
    }

    public class WriteBackMappingValidationResult
    {
        public bool IsValid => Errors.Count == 0;

        public List<string> Errors { get; }

        public WriteBackMappingValidationResult(List<string> errors)
        {
            Errors = errors;
        }
    }
}
