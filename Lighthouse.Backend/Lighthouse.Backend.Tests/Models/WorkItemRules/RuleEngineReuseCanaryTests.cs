using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Models.WorkItemRules
{
    /// <summary>
    /// Drives DDD-7 (cross-cutting invariant #6 — rule-engine JSON-shape reuse).
    /// CI gate: failing the canary means the rule-engine generalisation has drifted
    /// and must be remediated before merge.
    /// </summary>
    [TestFixture]
    public class RuleEngineReuseCanaryTests
    {
        [Test]
        public void TypeEqualsBugRuleSet_DeserialisesIdenticallyAcrossBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "Type", "Operator": "Equals", "Value": "Bug" }
              ]
            }
            """;

            var deliveryRulesView = DeserialiseAsDeliveryRulesConsumer(json);
            var forecastFilterView = DeserialiseAsForecastFilterConsumer(json);

            AssertRuleSetsEqual(deliveryRulesView, forecastFilterView);
        }

        [Test]
        public void TagsContainsMaintenanceRuleSet_DeserialisesIdenticallyAcrossBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "Tags", "Operator": "Contains", "Value": "maintenance" }
              ]
            }
            """;

            var deliveryRulesView = DeserialiseAsDeliveryRulesConsumer(json);
            var forecastFilterView = DeserialiseAsForecastFilterConsumer(json);

            AssertRuleSetsEqual(deliveryRulesView, forecastFilterView);
        }

        [Test]
        public void ParentReferenceIdEqualsEmptyRuleSet_DeserialisesIdenticallyAcrossBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "ParentReferenceId", "Operator": "Equals", "Value": "" }
              ]
            }
            """;

            var deliveryRulesView = DeserialiseAsDeliveryRulesConsumer(json);
            var forecastFilterView = DeserialiseAsForecastFilterConsumer(json);

            AssertRuleSetsEqual(deliveryRulesView, forecastFilterView);
        }

        [Test]
        public void MultiRuleSet_DeserialisesIdenticallyAcrossBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "Type", "Operator": "Equals", "Value": "Bug" },
                { "FieldKey": "Tags", "Operator": "Contains", "Value": "maintenance" },
                { "FieldKey": "State", "Operator": "NotEquals", "Value": "Removed" }
              ]
            }
            """;

            var deliveryRulesView = DeserialiseAsDeliveryRulesConsumer(json);
            var forecastFilterView = DeserialiseAsForecastFilterConsumer(json);

            AssertRuleSetsEqual(deliveryRulesView, forecastFilterView);
        }

        [Test]
        public void AdditionalFieldRuleSet_DeserialisesIdenticallyAcrossBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "CustomField.Priority", "Operator": "Equals", "Value": "High" }
              ]
            }
            """;

            var deliveryRulesView = DeserialiseAsDeliveryRulesConsumer(json);
            var forecastFilterView = DeserialiseAsForecastFilterConsumer(json);

            AssertRuleSetsEqual(deliveryRulesView, forecastFilterView);
        }

        [Test]
        public void RuleSetExceedingMaxRules_FailsValidationOnBothConsumers()
        {
            var conditions = Enumerable.Range(0, WorkItemRuleSet.MaxRules + 1)
                .Select(i => $$"""{ "FieldKey": "Type", "Operator": "Equals", "Value": "Bug{{i}}" }""");
            var json = $$"""
            {
              "Version": 1,
              "Conditions": [ {{string.Join(",", conditions)}} ]
            }
            """;

            var (deliveryValid, forecastValid) = ValidateOnBothConsumers(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryValid, Is.False, "Delivery-rules consumer should reject > MaxRules conditions");
                Assert.That(forecastValid, Is.False, "Forecast-filter consumer should reject > MaxRules conditions");
            }
        }

        [Test]
        public void RuleValueExceedingMaxLength_FailsValidationOnBothConsumers()
        {
            var oversizeValue = new string('x', WorkItemRuleSet.MaxValueLength + 1);
            var json = $$"""
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "Name", "Operator": "Contains", "Value": "{{oversizeValue}}" }
              ]
            }
            """;

            var (deliveryValid, forecastValid) = ValidateOnBothConsumers(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(deliveryValid, Is.False, "Delivery-rules consumer should reject oversize value");
                Assert.That(forecastValid, Is.False, "Forecast-filter consumer should reject oversize value");
            }
        }

        [Test]
        public void UnknownFieldKeyRuleSet_RejectedIdenticallyOnBothConsumers()
        {
            const string json = """
            {
              "Version": 1,
              "Conditions": [
                { "FieldKey": "totally.unknown.key", "Operator": "Equals", "Value": "Bug" }
              ]
            }
            """;

            var (deliveryValid, forecastValid) = ValidateOnBothConsumers(json);

            Assert.That(deliveryValid, Is.EqualTo(forecastValid), "JSON-shape parity (DDD-7): both consumers must agree on the verdict for the same JSON");
            Assert.That(deliveryValid, Is.False, "Unknown field key must be rejected by both consumers");
        }

        private static WorkItemRuleSet DeserialiseAsDeliveryRulesConsumer(string json)
        {
            return JsonSerializer.Deserialize<WorkItemRuleSet>(json)!;
        }

        private static WorkItemRuleSet DeserialiseAsForecastFilterConsumer(string json)
        {
            return JsonSerializer.Deserialize<WorkItemRuleSet>(json)!;
        }

        private static (bool deliveryValid, bool forecastValid) ValidateOnBothConsumers(string json)
        {
            var ruleSet = JsonSerializer.Deserialize<WorkItemRuleSet>(json)!;
            var deliverySchema = BuildSchemaFromFixedFields(new FeatureFieldProvider());
            var forecastSchema = BuildSchemaFromFixedFields(new WorkItemFieldProvider());
            var deliveryEvaluator = new RuleEvaluator<Feature>();
            var forecastEvaluator = new RuleEvaluator<WorkItem>();
            return (deliveryEvaluator.IsValid(ruleSet, deliverySchema), forecastEvaluator.IsValid(ruleSet, forecastSchema));
        }

        private static WorkItemRuleSchema BuildSchemaFromFixedFields<T>(IRuleFieldProvider<T> fieldProvider) where T : class
        {
            return new WorkItemRuleSchema
            {
                Fields = fieldProvider.GetFixedFields().ToList(),
                Operators = ["equals", "notequals", "contains"],
                MaxRules = WorkItemRuleSet.MaxRules,
                MaxValueLength = WorkItemRuleSet.MaxValueLength,
            };
        }

        private static void AssertRuleSetsEqual(WorkItemRuleSet expected, WorkItemRuleSet actual)
        {
            Assert.That(actual.Version, Is.EqualTo(expected.Version), "Version mismatch across consumers");
            Assert.That(actual.Conditions, Has.Count.EqualTo(expected.Conditions.Count), "Condition count mismatch across consumers");

            for (var i = 0; i < expected.Conditions.Count; i++)
            {
                var expectedCondition = expected.Conditions[i];
                var actualCondition = actual.Conditions[i];

                Assert.That(actualCondition.FieldKey, Is.EqualTo(expectedCondition.FieldKey), $"FieldKey mismatch at index {i}");
                Assert.That(actualCondition.Operator, Is.EqualTo(expectedCondition.Operator), $"Operator mismatch at index {i}");
                Assert.That(actualCondition.Value, Is.EqualTo(expectedCondition.Value), $"Value mismatch at index {i}");
            }
        }
    }
}
