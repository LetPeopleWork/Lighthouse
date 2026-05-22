using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.DeliveryRules;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DeliveryRuleService : IDeliveryRuleService
    {
        private const string EqualsOperator = "equals";
        private const string NotEqualsOperator = "notequals";
        private const string ContainsOperator = "contains";

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

        public DeliveryRuleSchema GetRuleSchema(Portfolio portfolio)
        {
            var fields = fieldProvider.GetFixedFields().ToList();

            foreach (var additionalField in portfolio.WorkTrackingSystemConnection.AdditionalFieldDefinitions)
            {
                fields.Add(new DeliveryRuleFieldDefinition
                {
                    FieldKey = $"additionalField.{additionalField.Id}",
                    DisplayName = additionalField.DisplayName,
                    IsMultiValue = false
                });
            }

            return new DeliveryRuleSchema
            {
                Fields = fields,
                Operators = [EqualsOperator, NotEqualsOperator, ContainsOperator],
                MaxRules = DeliveryRuleSet.MaxRules,
                MaxValueLength = DeliveryRuleSet.MaxValueLength
            };
        }

        public IEnumerable<Feature> GetMatchingFeaturesForRuleset(DeliveryRuleSet ruleSet, IEnumerable<Feature> features)
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

                var ruleSet = JsonSerializer.Deserialize<DeliveryRuleSet>(delivery.RuleDefinitionJson);
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
