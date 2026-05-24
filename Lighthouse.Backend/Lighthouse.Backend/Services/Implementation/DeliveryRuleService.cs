using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DeliveryRuleService : IDeliveryRuleService
    {
        private const string EqualsOperator = "equals";
        private const string NotEqualsOperator = "notequals";
        private const string ContainsOperator = "contains";
        private const string NotContainsOperator = "notcontains";
        private const string IsEmptyOperator = "isempty";
        private const string IsNotEmptyOperator = "isnotempty";

        private readonly IRuleEvaluator<Feature> ruleEvaluator;
        private readonly IRuleFieldProvider<Feature> fieldProvider;

        public DeliveryRuleService()
            : this(new RuleEvaluator<Feature>(), new FeatureFieldProvider())
        {
        }

        public DeliveryRuleService(IRuleEvaluator<Feature> ruleEvaluator, IRuleFieldProvider<Feature> fieldProvider)
        {
            this.ruleEvaluator = ruleEvaluator;
            this.fieldProvider = fieldProvider;
        }

        public WorkItemRuleSchema GetRuleSchema(Portfolio portfolio)
        {
            var fields = fieldProvider.GetFixedFields().ToList();

            foreach (var additionalField in portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
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
                Operators = [EqualsOperator, NotEqualsOperator, ContainsOperator, NotContainsOperator, IsEmptyOperator, IsNotEmptyOperator],
                MaxRules = WorkItemRuleSet.MaxRules,
                MaxValueLength = WorkItemRuleSet.MaxValueLength
            };
        }

        public IEnumerable<Feature> GetMatchingFeaturesForRuleset(WorkItemRuleSet ruleSet, IEnumerable<Feature> features)
        {
            return ruleEvaluator.Match(ruleSet, features, fieldProvider);
        }

        public void RecomputeRuleBasedDeliveries(Portfolio portfolio, IEnumerable<Delivery> deliveries)
        {
            foreach (var delivery in deliveries.Where(d => d.SelectionMode == DeliverySelectionMode.RuleBased))
            {
                if (string.IsNullOrEmpty(delivery.RuleDefinitionJson))
                {
                    continue;
                }

                var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(delivery.RuleDefinitionJson);
                if (ruleSet == null)
                {
                    continue;
                }

                var features = GetMatchingFeaturesForRuleset(ruleSet, portfolio.Features);
                if (features.Any())
                {
                    delivery.Features.Clear();
                    delivery.Features.AddRange(features);
                }
            }
        }
    }
}
