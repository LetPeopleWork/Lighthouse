using System.Globalization;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;

namespace Lighthouse.Backend.API.Helpers
{
    public static class WriteBackMappingValidator
    {
        // Every source that writes a date, at either end of a Feature. Both ends are here because the
        // rule below is about rendering a date as text, and that is the same problem whichever end the
        // date came from.
        private static readonly HashSet<WriteBackValueSource> DateWritingSources =
            [.. WriteBackValueSources.Completion, .. WriteBackValueSources.Start];

        public static WriteBackMappingValidationResult Validate(List<WriteBackMappingDefinition> mappings)
        {
            var errors = new List<string>();

            foreach (var mapping in mappings)
            {
                if (mapping.AdditionalFieldDefinitionId < 0 && mapping.AdditionalFieldDefinition is null)
                {
                    errors.Add($"The write-back mapping targets an unknown additional field (id: {mapping.AdditionalFieldDefinitionId}).");
                }
                else if (TargetOf(mapping) is null)
                {
                    errors.Add("An additional field is required for every write-back mapping.");
                }

                if (DateWritingSources.Contains(mapping.ValueSource) &&
                    mapping.TargetValueType == WriteBackTargetValueType.FormattedText)
                {
                    if (string.IsNullOrEmpty(mapping.DateFormat))
                    {
                        errors.Add($"DateFormat is required when TargetValueType is FormattedText for forecast sources (field id: '{mapping.AdditionalFieldDefinitionId}').");
                    }
                    else if (!CanFormatADate(mapping.DateFormat))
                    {
                        errors.Add($"DateFormat '{mapping.DateFormat}' is not a date format .NET understands (field id: '{mapping.AdditionalFieldDefinitionId}').");
                    }
                }
            }

            var duplicates = mappings
                .Select(m => new { Target = TargetOf(m), m.AppliesTo })
                .Where(m => m.Target is not null)
                .GroupBy(m => m)
                .Where(g => g.Count() > 1)
                .Select(g => Describe(g.Key.Target!))
                .ToList();

            foreach (var duplicate in duplicates)
            {
                errors.Add($"Duplicate additional field ({duplicate}) found for the same scope. Each mapping must target a unique field per scope.");
            }

            return new WriteBackMappingValidationResult(errors);
        }

        // A field saved in the same request has no id yet, so the field object itself identifies it.
        private static object? TargetOf(WriteBackMappingDefinition mapping)
        {
            if (mapping.AdditionalFieldDefinition is { Id: 0 } newField)
            {
                return newField;
            }

            return mapping.AdditionalFieldDefinitionId is > 0 ? mapping.AdditionalFieldDefinitionId : null;
        }

        private static string Describe(object target)
        {
            return target is AdditionalFieldDefinition newField ? $"new field: '{newField.Reference}'" : $"id: {target}";
        }

        /// <summary>
        /// The format box takes free text, and an unusable one is not discovered until a write-back round
        /// tries to use it - by which point it throws somewhere the caller can only answer by abandoning the
        /// round. Asking a sample date to render is the cheapest way to find out here, where the admin is
        /// still looking at the field they typed it into.
        ///
        /// This catches only what actually throws - a lone character that is not a standard specifier, an
        /// unclosed quote. Text .NET does not recognise is copied through as a literal, so a format that is
        /// merely wrong still renders, and telling wrong from deliberate is not something a validator can do.
        /// </summary>
        private static bool CanFormatADate(string format)
        {
            try
            {
                _ = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).ToString(format, CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
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
