namespace Lighthouse.Backend.Models.WorkItemRules
{
    /// <summary>
    /// Removes rule conditions pointing at an additional field the connection no longer defines.
    /// Editing a connection's additional fields deletes the ones missing from the payload and
    /// re-adds them under fresh ids, so a rule saved earlier can end up naming a field that is
    /// gone. Such a rule reads as a blank row on screen, matches against an always-empty value —
    /// which with a negated operator marks every item — and fails validation hard enough to
    /// reject the whole settings save. Dropping it as the rule set is read means the next save
    /// writes the cleaned set back without anyone being asked to fix it by hand.
    /// </summary>
    public static class AdditionalFieldRuleHealing
    {
        private const string AdditionalFieldPrefix = "additionalField.";

        public static WorkItemRuleSet WithoutDeletedAdditionalFields(
            WorkItemRuleSet ruleSet,
            WorkTrackingSystemConnection? connection)
        {
            // Without the definitions there is no way to tell a deleted field from an unloaded
            // one, and dropping a live rule is worse than keeping a dead one.
            if (connection?.AdditionalFieldDefinitions == null)
            {
                return ruleSet;
            }

            var definedKeys = connection.AdditionalFieldDefinitions
                .Select(definition => $"{AdditionalFieldPrefix}{definition.Id}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            ruleSet.Conditions = [.. ruleSet.Conditions.Where(condition => IsStillDefined(condition.FieldKey, definedKeys))];

            return ruleSet;
        }

        private static bool IsStillDefined(string fieldKey, HashSet<string> definedKeys)
        {
            return !fieldKey.StartsWith(AdditionalFieldPrefix, StringComparison.OrdinalIgnoreCase)
                || definedKeys.Contains(fieldKey);
        }
    }
}
