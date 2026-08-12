using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Extensions
{
    public static class WorkItemExtensions
    {
        /// <summary>
        /// The value a record carries for one configured field, or null when it carries none. A record
        /// stored before the field was configured has no entry for it, and every caller already reads
        /// null as "not set" - so the absent case answers rather than throwing.
        /// </summary>
        public static string? GetAdditionalFieldValue(this WorkItemBase workItem, int? fieldId)
            => fieldId.HasValue ? workItem.AdditionalFieldValues.GetValueOrDefault(fieldId.Value) : null;
    }
}