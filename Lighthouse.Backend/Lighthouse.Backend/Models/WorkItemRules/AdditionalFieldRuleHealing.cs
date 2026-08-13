namespace Lighthouse.Backend.Models.WorkItemRules
{
    /// <summary>
    /// Removes rule conditions naming a field the rule builder no longer offers. Editing a
    /// connection's additional fields deletes the ones missing from the payload and re-adds them
    /// under fresh ids, so a rule saved earlier can name a field that is gone. Such a rule reads as
    /// a blank row on screen and fails validation hard enough to reject the whole settings save,
    /// leaving no way to clear it from the UI. Dropping it as the settings are read means the next
    /// save writes the cleaned set back.
    ///
    /// The schema is the authority on what exists, and it must be one built from a connection whose
    /// additional fields were actually loaded: an unloaded collection is empty rather than absent,
    /// and taking that at face value would discard every additional-field rule the owner has.
    /// </summary>
    public static class AdditionalFieldRuleHealing
    {
        public static WorkItemRuleSet WithoutFieldsMissingFrom(WorkItemRuleSet ruleSet, WorkItemRuleSchema schema)
        {
            var offeredKeys = schema.Fields
                .Select(field => field.FieldKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return new WorkItemRuleSet
            {
                Version = ruleSet.Version,
                Mode = ruleSet.Mode,
                Conditions = [.. ruleSet.Conditions.Where(condition => offeredKeys.Contains(condition.FieldKey))],
            };
        }
    }
}
