using Lighthouse.Backend.Models.DeliveryRules;

namespace Lighthouse.Backend.Services.Interfaces.DeliveryRules
{
    /// <summary>
    /// Field accessor for <see cref="IRuleEvaluator{T}"/>. Implementations map a typed item
    /// (e.g. <c>Feature</c>, <c>WorkItem</c>) to scalar field values, tag collections, and the
    /// fixed-field schema. Connector-defined <c>additionalField.{id}</c> fields are resolved by
    /// <see cref="GetFieldValue"/> using the dotted key syntax matching the existing
    /// delivery-rules pattern.
    /// </summary>
    public interface IRuleFieldProvider<T> where T : class
    {
        string GetFieldValue(T item, string fieldKey);

        IReadOnlyList<string> GetTagsForField(T item, string fieldKey);

        IReadOnlyList<DeliveryRuleFieldDefinition> GetFixedFields();
    }
}
