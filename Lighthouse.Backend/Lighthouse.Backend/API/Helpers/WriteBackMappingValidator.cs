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
                if (mapping.AdditionalFieldDefinitionId is null or 0)
                {
                    errors.Add("An additional field is required for every write-back mapping.");
                }

                if (ForecastSources.Contains(mapping.ValueSource) &&
                    mapping.TargetValueType == WriteBackTargetValueType.FormattedText &&
                    string.IsNullOrEmpty(mapping.DateFormat))
                {
                    errors.Add($"DateFormat is required when TargetValueType is FormattedText for forecast sources (field id: '{mapping.AdditionalFieldDefinitionId}').");
                }
            }

            var duplicates = mappings
                .Where(m => m.AdditionalFieldDefinitionId is not null and not 0)
                .GroupBy(m => new { m.AdditionalFieldDefinitionId, m.AppliesTo })
                .Where(g => g.Count() > 1)
                .Select(g => g.First().AdditionalFieldDefinitionId)
                .ToList();

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate additional field (id: {duplicate}) found for the same scope. Each mapping must target a unique field per scope.");
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
