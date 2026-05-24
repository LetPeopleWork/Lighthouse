using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public sealed class ForecastFilterRuleService(
        IRuleEvaluator<WorkItem> ruleEvaluator,
        IRuleFieldProvider<WorkItem> fieldProvider,
        ILicenseService licenseService)
        : IForecastFilterRuleService
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

        public WorkItemRuleSchema GetSchema(Team team)
        {
            var fields = fieldProvider.GetFixedFields().ToList();

            foreach (var additionalField in team.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
            {
                fields.Add(new WorkItemRuleFieldDefinition
                {
                    FieldKey = $"additionalField.{additionalField.Id}",
                    DisplayName = additionalField.DisplayName,
                    IsMultiValue = false
                });
            }

            return new WorkItemRuleSchema
            {
                Fields = fields,
                Operators = [.. RuleOperators.All],
                MaxRules = WorkItemRuleSet.MaxRules,
                MaxValueLength = WorkItemRuleSet.MaxValueLength
            };
        }

        public WorkItemRuleSet? GetEffectiveRuleSet(Team team)
        {
            if (!licenseService.CanUsePremiumFeatures())
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(team.ForecastFilterRuleSetJson))
            {
                return null;
            }
            
            var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(team.ForecastFilterRuleSetJson, JsonSerializerOptions);
            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return null;
            }

            return ruleSet;
        }

        public IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet)
        {
            var materialised = items.ToList();
            var matched = ruleEvaluator.Match(ruleSet, materialised, fieldProvider);
            return materialised.Except(matched, WorkItemReferenceIdComparer.Instance).ToList();
        }

        public bool ValidateRuleSet(WorkItemRuleSet ruleSet, Team team)
        {
            return ruleEvaluator.IsValid(ruleSet, GetSchema(team));
        }

        private sealed class WorkItemReferenceIdComparer : IEqualityComparer<WorkItem>
        {
            internal static readonly WorkItemReferenceIdComparer Instance = new();

            public bool Equals(WorkItem? x, WorkItem? y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x is null || y is null)
                {
                    return false;
                }

                return string.Equals(x.ReferenceId, y.ReferenceId, StringComparison.Ordinal);
            }

            public int GetHashCode(WorkItem obj)
            {
                return obj.ReferenceId.GetHashCode(StringComparison.Ordinal);
            }
        }
    }
}
