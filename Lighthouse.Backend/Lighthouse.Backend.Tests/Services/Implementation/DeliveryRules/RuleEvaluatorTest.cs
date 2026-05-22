using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation.DeliveryRules;
using Lighthouse.Backend.Services.Interfaces.DeliveryRules;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliveryRules
{
    [TestFixture]
    public class RuleEvaluatorTest
    {
        private const string TypeFieldKey = "feature.type";
        private const string NameFieldKey = "feature.name";
        private const string StateFieldKey = "feature.state";
        private const string TagsFieldKey = "feature.tags";

        private RuleEvaluator<Feature> subject;
        private Mock<IRuleFieldProvider<Feature>> fieldProvider;

        [SetUp]
        public void SetUp()
        {
            subject = new RuleEvaluator<Feature>();
            fieldProvider = new Mock<IRuleFieldProvider<Feature>>();
            fieldProvider
                .Setup(p => p.GetFixedFields())
                .Returns(new List<DeliveryRuleFieldDefinition>
                {
                    new() { FieldKey = TypeFieldKey, DisplayName = "Type", IsMultiValue = false },
                    new() { FieldKey = NameFieldKey, DisplayName = "Name", IsMultiValue = false },
                    new() { FieldKey = StateFieldKey, DisplayName = "State", IsMultiValue = false },
                    new() { FieldKey = TagsFieldKey, DisplayName = "Tags", IsMultiValue = true },
                });
        }

        [TestCase("equals", "Epic", "Epic", true)]
        [TestCase("equals", "EPIC", "epic", true)]
        [TestCase("equals", "Epic", "Story", false)]
        [TestCase("notEquals", "Bug", "Epic", true)]
        [TestCase("notEquals", "BUG", "bug", false)]
        [TestCase("notEquals", "Bug", "Bug", false)]
        [TestCase("contains", "Auth", "Authentication Module", true)]
        [TestCase("contains", "AUTH", "authentication module", true)]
        [TestCase("contains", "Auth", "Payment Module", false)]
        public void Match_OperatorSemantics_MatchesItemsCaseInsensitively(
            string op, string conditionValue, string itemFieldValue, bool shouldMatch)
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = op, Value = conditionValue }]
            };
            var feature = new Feature { Name = "F1" };
            fieldProvider.Setup(p => p.GetFieldValue(feature, TypeFieldKey)).Returns(itemFieldValue);

            var result = subject.Match(ruleSet, [feature], fieldProvider.Object).ToList();

            Assert.That(result, shouldMatch ? Does.Contain(feature) : Is.Empty);
        }

        [TestCase("equals", "Priority", new[] { "Priority", "Q1" }, true)]
        [TestCase("equals", "PRIORITY", new[] { "priority" }, true)]
        [TestCase("equals", "Priority", new[] { "Q1", "Q2" }, false)]
        [TestCase("notEquals", "Blocked", new[] { "Priority" }, true)]
        [TestCase("notEquals", "Blocked", new[] { "Blocked", "Priority" }, false)]
        [TestCase("contains", "Pri", new[] { "Priority" }, true)]
        [TestCase("contains", "Pri", new[] { "Backlog" }, false)]
        public void Match_TagsField_DelegatesToTagsProviderCaseInsensitively(
            string op, string conditionValue, string[] tags, bool shouldMatch)
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TagsFieldKey, Operator = op, Value = conditionValue }]
            };
            var feature = new Feature { Name = "F1" };
            fieldProvider.Setup(p => p.GetFieldValue(feature, TagsFieldKey)).Returns(string.Empty);
            fieldProvider.Setup(p => p.GetTagsForField(feature, TagsFieldKey)).Returns(tags);

            var result = subject.Match(ruleSet, [feature], fieldProvider.Object).ToList();

            Assert.That(result, shouldMatch ? Does.Contain(feature) : Is.Empty);
        }

        [Test]
        public void Match_MultipleConditions_RequiresAllToMatch()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions =
                [
                    new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" },
                    new DeliveryRuleCondition { FieldKey = StateFieldKey, Operator = "equals", Value = "Active" },
                ]
            };
            var matching = new Feature { Name = "M" };
            var partial = new Feature { Name = "P" };
            fieldProvider.Setup(p => p.GetFieldValue(matching, TypeFieldKey)).Returns("Epic");
            fieldProvider.Setup(p => p.GetFieldValue(matching, StateFieldKey)).Returns("Active");
            fieldProvider.Setup(p => p.GetFieldValue(partial, TypeFieldKey)).Returns("Epic");
            fieldProvider.Setup(p => p.GetFieldValue(partial, StateFieldKey)).Returns("Done");

            var result = subject.Match(ruleSet, [matching, partial], fieldProvider.Object).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matching));
            }
        }

        [Test]
        public void Match_DoesNotMutateInputCollection()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" }]
            };
            var feature = new Feature { Name = "F1" };
            fieldProvider.Setup(p => p.GetFieldValue(feature, TypeFieldKey)).Returns("Story");
            var items = new List<Feature> { feature };

            subject.Match(ruleSet, items, fieldProvider.Object).ToList();

            Assert.That(items, Has.Count.EqualTo(1));
        }

        [Test]
        public void IsValid_HappyPathSingleCondition_ReturnsTrue()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.True);
        }

        [Test]
        public void IsValid_ZeroConditions_ReturnsFalse()
        {
            var ruleSet = new DeliveryRuleSet { Conditions = [] };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_ConditionCountExceedsCap_ReturnsFalse()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = Enumerable.Range(1, DeliveryRuleSet.MaxRules + 1)
                    .Select(_ => new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" })
                    .ToList()
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_ValueExceedsLengthCap_ReturnsFalse()
        {
            var oversized = new string('x', DeliveryRuleSet.MaxValueLength + 1);
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = oversized }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_UnknownFieldKey_ReturnsFalse()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "unknown.field", Operator = "equals", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_UnknownOperator_ReturnsFalse()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = TypeFieldKey, Operator = "greaterThan", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void Match_RuleSetFailsValidation_ReturnsEmpty()
        {
            var ruleSet = new DeliveryRuleSet { Conditions = [] };
            var feature = new Feature { Name = "F1" };

            var result = subject.Match(ruleSet, [feature], fieldProvider.Object);

            Assert.That(result, Is.Empty);
        }

        private static DeliveryRuleSchema BuildSchema()
        {
            return new DeliveryRuleSchema
            {
                Fields =
                [
                    new DeliveryRuleFieldDefinition { FieldKey = TypeFieldKey, DisplayName = "Type" },
                    new DeliveryRuleFieldDefinition { FieldKey = NameFieldKey, DisplayName = "Name" },
                    new DeliveryRuleFieldDefinition { FieldKey = StateFieldKey, DisplayName = "State" },
                    new DeliveryRuleFieldDefinition { FieldKey = TagsFieldKey, DisplayName = "Tags", IsMultiValue = true },
                ],
                Operators = ["equals", "notEquals", "contains"],
                MaxRules = DeliveryRuleSet.MaxRules,
                MaxValueLength = DeliveryRuleSet.MaxValueLength,
            };
        }
    }
}
