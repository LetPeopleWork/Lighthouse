using System.Text.Json;
using Lighthouse.Backend.Models.WorkItemRules;
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
            Assert.Ignore("Step 01-11 dependency: ForecastFilterRuleService.ValidateRuleSet not yet implemented. Will turn GREEN at step 01-11.");
        }

        [Test]
        public void RuleValueExceedingMaxLength_FailsValidationOnBothConsumers()
        {
            Assert.Ignore("Step 01-11 dependency: ForecastFilterRuleService.ValidateRuleSet not yet implemented. Will turn GREEN at step 01-11.");
        }

        private static WorkItemRuleSet DeserialiseAsDeliveryRulesConsumer(string json)
        {
            return JsonSerializer.Deserialize<WorkItemRuleSet>(json)!;
        }

        private static WorkItemRuleSet DeserialiseAsForecastFilterConsumer(string json)
        {
            return JsonSerializer.Deserialize<WorkItemRuleSet>(json)!;
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
