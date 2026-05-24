using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.WorkItemRules;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItemRules
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
                .Returns(new List<WorkItemRuleFieldDefinition>
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
        [TestCase("notContains", "Auth", "Payment Module", true)]
        [TestCase("notContains", "auth", "Authentication Module", false)]
        [TestCase("notContains", "Auth", "", true)]
        [TestCase("isEmpty", "", "", true)]
        [TestCase("isEmpty", "ignored", "", true)]
        [TestCase("isEmpty", "", "Anything", false)]
        [TestCase("isNotEmpty", "", "Anything", true)]
        [TestCase("isNotEmpty", "ignored", "X", true)]
        [TestCase("isNotEmpty", "", "", false)]
        public void Match_OperatorSemantics_MatchesItemsCaseInsensitively(
            string op, string conditionValue, string itemFieldValue, bool shouldMatch)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = op, Value = conditionValue }]
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
        [TestCase("notContains", "Pri", new[] { "Backlog" }, true)]
        [TestCase("notContains", "Pri", new[] { "Priority" }, false)]
        [TestCase("notContains", "Pri", new string[0], true)]
        [TestCase("isEmpty", "", new string[0], true)]
        [TestCase("isEmpty", "ignored", new string[0], true)]
        [TestCase("isEmpty", "", new[] { "Anything" }, false)]
        [TestCase("isNotEmpty", "", new[] { "Anything" }, true)]
        [TestCase("isNotEmpty", "ignored", new[] { "X" }, true)]
        [TestCase("isNotEmpty", "", new string[0], false)]
        public void Match_TagsField_DelegatesToTagsProviderCaseInsensitively(
            string op, string conditionValue, string[] tags, bool shouldMatch)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TagsFieldKey, Operator = op, Value = conditionValue }]
            };
            var feature = new Feature { Name = "F1" };
            fieldProvider.Setup(p => p.GetFieldValue(feature, TagsFieldKey)).Returns(string.Empty);
            fieldProvider.Setup(p => p.GetTagsForField(feature, TagsFieldKey)).Returns(tags);

            var result = subject.Match(ruleSet, [feature], fieldProvider.Object).ToList();

            Assert.That(result, shouldMatch ? Does.Contain(feature) : Is.Empty);
        }

        [Test]
        public void Match_ModeOmitted_DefaultsToAndSemantics()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" },
                    new WorkItemRuleCondition { FieldKey = StateFieldKey, Operator = "equals", Value = "Active" },
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
        public void Match_ModeOr_MatchesItemWhenAnyConditionPasses()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = "or",
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" },
                    new WorkItemRuleCondition { FieldKey = StateFieldKey, Operator = "equals", Value = "Active" },
                ]
            };
            var typeOnlyMatch = new Feature { Name = "T" };
            var stateOnlyMatch = new Feature { Name = "S" };
            var noMatch = new Feature { Name = "N" };
            fieldProvider.Setup(p => p.GetFieldValue(typeOnlyMatch, TypeFieldKey)).Returns("Epic");
            fieldProvider.Setup(p => p.GetFieldValue(typeOnlyMatch, StateFieldKey)).Returns("Done");
            fieldProvider.Setup(p => p.GetFieldValue(stateOnlyMatch, TypeFieldKey)).Returns("Story");
            fieldProvider.Setup(p => p.GetFieldValue(stateOnlyMatch, StateFieldKey)).Returns("Active");
            fieldProvider.Setup(p => p.GetFieldValue(noMatch, TypeFieldKey)).Returns("Story");
            fieldProvider.Setup(p => p.GetFieldValue(noMatch, StateFieldKey)).Returns("Done");

            var result = subject.Match(ruleSet, [typeOnlyMatch, stateOnlyMatch, noMatch], fieldProvider.Object).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That(result, Does.Contain(typeOnlyMatch));
                Assert.That(result, Does.Contain(stateOnlyMatch));
                Assert.That(result, Does.Not.Contain(noMatch));
            }
        }

        [Test]
        public void Match_ModeAndExplicit_BehavesLikeOmittedMode()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = "and",
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" },
                    new WorkItemRuleCondition { FieldKey = StateFieldKey, Operator = "equals", Value = "Active" },
                ]
            };
            var matching = new Feature { Name = "M" };
            fieldProvider.Setup(p => p.GetFieldValue(matching, TypeFieldKey)).Returns("Epic");
            fieldProvider.Setup(p => p.GetFieldValue(matching, StateFieldKey)).Returns("Active");

            var result = subject.Match(ruleSet, [matching], fieldProvider.Object).ToList();

            Assert.That(result, Does.Contain(matching));
        }

        [Test]
        public void Match_MultipleConditions_RequiresAllToMatch()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" },
                    new WorkItemRuleCondition { FieldKey = StateFieldKey, Operator = "equals", Value = "Active" },
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
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" }]
            };
            var feature = new Feature { Name = "F1" };
            fieldProvider.Setup(p => p.GetFieldValue(feature, TypeFieldKey)).Returns("Story");
            var items = new List<Feature> { feature };

            var matched = subject.Match(ruleSet, items, fieldProvider.Object).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(items, Has.Count.EqualTo(1));
                Assert.That(matched, Is.Not.Null);
            }
        }

        [Test]
        public void IsValid_HappyPathSingleCondition_ReturnsTrue()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.True);
        }

        [Test]
        public void IsValid_ZeroConditions_ReturnsFalse()
        {
            var ruleSet = new WorkItemRuleSet { Conditions = [] };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_ConditionCountExceedsCap_ReturnsFalse()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = Enumerable.Range(1, WorkItemRuleSet.MaxRules + 1)
                    .Select(_ => new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = "Epic" })
                    .ToList()
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_ValueExceedsLengthCap_ReturnsFalse()
        {
            var oversized = new string('x', WorkItemRuleSet.MaxValueLength + 1);
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "equals", Value = oversized }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_UnknownFieldKey_ReturnsFalse()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = "unknown.field", Operator = "equals", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void IsValid_IsEmptyOperatorWithEmptyValue_ReturnsTrue()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "isEmpty", Value = string.Empty }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.True);
        }

        [Test]
        public void IsValid_IsNotEmptyOperatorWithEmptyValue_ReturnsTrue()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "isNotEmpty", Value = string.Empty }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.True);
        }

        [Test]
        public void IsValid_UnknownOperator_ReturnsFalse()
        {
            var ruleSet = new WorkItemRuleSet
            {
                Conditions = [new WorkItemRuleCondition { FieldKey = TypeFieldKey, Operator = "greaterThan", Value = "Epic" }]
            };

            var valid = subject.IsValid(ruleSet, BuildSchema());

            Assert.That(valid, Is.False);
        }

        [Test]
        public void Match_RuleSetFailsValidation_ReturnsEmpty()
        {
            var ruleSet = new WorkItemRuleSet { Conditions = [] };
            var feature = new Feature { Name = "F1" };

            var result = subject.Match(ruleSet, [feature], fieldProvider.Object);

            Assert.That(result, Is.Empty);
        }

        private static WorkItemRuleSchema BuildSchema()
        {
            return new WorkItemRuleSchema
            {
                Fields =
                [
                    new WorkItemRuleFieldDefinition { FieldKey = TypeFieldKey, DisplayName = "Type" },
                    new WorkItemRuleFieldDefinition { FieldKey = NameFieldKey, DisplayName = "Name" },
                    new WorkItemRuleFieldDefinition { FieldKey = StateFieldKey, DisplayName = "State" },
                    new WorkItemRuleFieldDefinition { FieldKey = TagsFieldKey, DisplayName = "Tags", IsMultiValue = true },
                ],
                Operators = ["equals", "notEquals", "contains", "notContains", "isEmpty", "isNotEmpty"],
                MaxRules = WorkItemRuleSet.MaxRules,
                MaxValueLength = WorkItemRuleSet.MaxValueLength,
            };
        }
    }
}
