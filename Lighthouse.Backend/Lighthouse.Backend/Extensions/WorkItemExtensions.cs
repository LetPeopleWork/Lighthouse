using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Extensions
{
    public static class WorkItemExtensions
    {
        public static string? GetAdditionalFieldValue(this WorkItemBase workItem, int? fieldId)
        {
            return !fieldId.HasValue ? null : workItem.AdditionalFieldValues[fieldId.Value];
        }
    }
}