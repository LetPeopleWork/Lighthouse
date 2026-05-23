using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation.Forecast
{
    public sealed class ForecastFilterRuleService : IForecastFilterRuleService
    {
        private const string EqualsOperator = "equals";
        private const string NotEqualsOperator = "notequals";
        private const string ContainsOperator = "contains";

        private readonly IRuleEvaluator<WorkItem> ruleEvaluator;
        private readonly IRuleFieldProvider<WorkItem> fieldProvider;
        private readonly ILicenseService licenseService;

        public ForecastFilterRuleService(
            IRuleEvaluator<WorkItem> ruleEvaluator,
            IRuleFieldProvider<WorkItem> fieldProvider,
            ILicenseService licenseService)
        {
            this.ruleEvaluator = ruleEvaluator;
            this.fieldProvider = fieldProvider;
            this.licenseService = licenseService;
        }

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
                Operators = [EqualsOperator, NotEqualsOperator, ContainsOperator],
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

            var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(team.ForecastFilterRuleSetJson);
            if (ruleSet == null || ruleSet.Conditions.Count == 0)
            {
                return null;
            }

            return ruleSet;
        }

        public IEnumerable<WorkItem> Filter(IEnumerable<WorkItem> items, WorkItemRuleSet ruleSet)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold (D8 semantics: matched items are EXCLUDED). Step 01-08 will call IRuleEvaluator<WorkItem>.Match then exclude.");
        }

        public bool ValidateRuleSet(WorkItemRuleSet ruleSet, Team team)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold. Step 01-08 will call IRuleEvaluator<WorkItem>.IsValid against GetSchema(team).");
        }
    }
}
