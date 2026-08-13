using Lighthouse.Backend.Models.WorkItemRules;

namespace Lighthouse.Backend.Tests.Models.WorkItemRules
{
    [TestFixture]
    public class WorkItemRuleSetJsonTest
    {
        private const string CamelCase = "{\"version\":1,\"mode\":\"or\",\"conditions\":[{\"fieldKey\":\"feature.type\",\"operator\":\"equals\",\"value\":\"Epic\"}]}";

        private const string PascalCase = "{\"Version\":1,\"Mode\":\"or\",\"Conditions\":[{\"FieldKey\":\"feature.type\",\"Operator\":\"equals\",\"Value\":\"Epic\"}]}";

        [TestCase(CamelCase)]
        [TestCase(PascalCase)]
        public void Deserialize_EitherCasing_ReadsTheConditions(string json)
        {
            var ruleSet = WorkItemRuleSetJson.Deserialize(json);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleSet, Is.Not.Null);
                Assert.That(ruleSet.Mode, Is.EqualTo("or"));
                Assert.That(ruleSet.Conditions, Has.Count.EqualTo(1));
                Assert.That(ruleSet.Conditions[0].FieldKey, Is.EqualTo("feature.type"));
                Assert.That(ruleSet.Conditions[0].Operator, Is.EqualTo("equals"));
                Assert.That(ruleSet.Conditions[0].Value, Is.EqualTo("Epic"));
            }
        }

        [Test]
        public void Serialize_WritesCamelCase()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" }],
            };

            var json = WorkItemRuleSetJson.Serialize(ruleSet);

            Assert.That(json, Is.EqualTo(CamelCase));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void Deserialize_NothingStored_ReturnsNull(string? json)
        {
            Assert.That(WorkItemRuleSetJson.Deserialize(json), Is.Null);
        }

        [Test]
        public void TryDeserialize_MalformedJson_ReturnsFalse()
        {
            var parsed = WorkItemRuleSetJson.TryDeserialize("not json at all", out var ruleSet);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(ruleSet, Is.Null);
            }
        }

        [Test]
        public void TryDeserialize_ValidJson_ReturnsTheRuleSet()
        {
            var parsed = WorkItemRuleSetJson.TryDeserialize(CamelCase, out var ruleSet);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(ruleSet?.Conditions, Has.Count.EqualTo(1));
            }
        }
    }
}
