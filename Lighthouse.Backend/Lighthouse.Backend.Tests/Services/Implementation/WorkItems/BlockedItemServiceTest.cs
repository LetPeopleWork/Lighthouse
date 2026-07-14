using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// Unit tests for the single blocked-evaluation authority (ADR-067). Real rule engine + field
    /// provider inside the hexagon (classical TDD, no mocks). BlockedRuleSetJson is the sole persisted
    /// configuration (legacy BlockedStates/BlockedTags columns have been dropped); pins the "no
    /// configuration -> empty rule set, nothing blocked" default and the Include (matched ⇒ blocked)
    /// evaluation the black-box acceptance tests only exercise by example.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedItemServiceTest
    {
        private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

        private static readonly JsonSerializerOptions CamelCase = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        private static BlockedItemService CreateSut()
            => new(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider());

        [Test]
        public void GetEffectiveRuleSet_ReturnsEmptyRuleSet_WhenNoConfigurationExists()
        {
            var team = new Team();

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleSet.Conditions, Is.Empty);
                Assert.That(ruleSet.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
            }
        }

        [Test]
        public void GetEffectiveRuleSet_ReturnsStoredRuleSetVerbatim_WhenColumnIsSet()
        {
            var stored = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = "equals", Value = "Stored" }],
            };
            var team = new Team
            {
                BlockedRuleSetJson = JsonSerializer.Serialize(stored),
            };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            var values = ruleSet.Conditions.Select(c => c.Value).ToList();
            Assert.That(values, Does.Contain("Stored"));
        }

        [Test]
        public void GetEffectiveRuleSet_IsIdempotent_WhenReadRepeatedly()
        {
            var team = new Team { BlockedRuleSetJson = RuleSetJson(("workitem.state", "equals", "Blocked"), ("workitem.tags", "contains", "impediment")) };
            var sut = CreateSut();

            var first = JsonSerializer.Serialize(sut.GetEffectiveRuleSet(team), CaseInsensitive);
            var second = JsonSerializer.Serialize(sut.GetEffectiveRuleSet(team), CaseInsensitive);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void IsBlocked_ReturnsFalse_WhenNoRuleSetConfigured()
        {
            var team = new Team();
            var item = new WorkItem { State = "In Progress", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.False);
        }

        [Test]
        public void IsBlocked_ReturnsTrue_WhenItemStateMatchesAConfiguredEqualsCondition()
        {
            var team = new Team { BlockedRuleSetJson = RuleSetJson(("workitem.state", "equals", "Blocked")) };
            var item = new WorkItem { State = "Blocked", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.True);
        }

        [Test]
        public void IsBlocked_ReturnsFalse_WhenItemStateDoesNotMatchAnyCondition()
        {
            var team = new Team { BlockedRuleSetJson = RuleSetJson(("workitem.state", "equals", "Blocked")) };
            var item = new WorkItem { State = "In Progress", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.False);
        }

        [Test]
        public void IsBlocked_ReturnsTrue_WhenItemTagMatchesAConfiguredContainsCondition()
        {
            var team = new Team { BlockedRuleSetJson = RuleSetJson(("workitem.tags", "contains", "Blocked")) };
            var item = new WorkItem { State = "In Progress", Tags = ["Blocked"] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.True);
        }

        [Test]
        public void IsBlocked_ReturnsTrue_WhenAdditionalFieldRuleMatchesANonEmptyFieldValue()
        {
            var team = new Team { BlockedRuleSetJson = AdditionalFieldNotEmptyRuleSet(42) };
            var item = new WorkItem { State = "In Progress", Tags = [], AdditionalFieldValues = { [42] = "Impediment" } };

            Assert.That(CreateSut().IsBlocked(item, team), Is.True);
        }

        [Test]
        public void IsBlocked_ReturnsFalse_WhenAdditionalFieldRuleTargetsAnEmptyFieldValue()
        {
            var team = new Team { BlockedRuleSetJson = AdditionalFieldNotEmptyRuleSet(42) };
            var item = new WorkItem { State = "In Progress", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.False);
        }

        [Test]
        public void IsBlocked_ForFeature_ReturnsTrue_WhenPortfolioRuleSetStateMatches()
        {
            var portfolio = new Portfolio { BlockedRuleSetJson = RuleSetJson(("feature.state", "equals", "On Hold")) };
            var feature = new Feature { State = "On Hold", Tags = [] };

            Assert.That(CreateSut().IsBlocked(feature, portfolio), Is.True);
        }

        [Test]
        public void IsBlocked_ForFeature_ReturnsFalse_WhenPortfolioHasNoBlockedConfiguration()
        {
            var portfolio = new Portfolio();
            var feature = new Feature { State = "On Hold", Tags = [] };

            Assert.That(CreateSut().IsBlocked(feature, portfolio), Is.False);
        }

        [Test]
        public void GetEffectiveRuleSet_DeserializesWithCaseInsensitivePropertyNames()
        {
            var stored = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = "equals", Value = "Blocked" }],
            };
            var camelJson = JsonSerializer.Serialize(stored, CamelCase);
            var upperJson = camelJson.Replace("\"mode\"", "\"MODE\"").Replace("\"conditions\"", "\"CONDITIONS\"");

            var team = new Team { BlockedRuleSetJson = upperJson };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            Assert.That(ruleSet.Conditions, Has.Count.EqualTo(1));
            Assert.That(ruleSet.Conditions[0].FieldKey, Is.EqualTo("workitem.state"));
        }

        [Test]
        public void GetEffectiveRuleSetJson_SerializesCamelCase_MatchingFrontendContract()
        {
            // Bug 3 regression: the frontend rule builder parses camelCase (version/mode/conditions/
            // fieldKey/operator/value). A PascalCase payload fails its zod parse and silently renders
            // the empty "add at least one rule" state despite a configured rule.
            var team = new Team { BlockedRuleSetJson = RuleSetJson(("workitem.state", "equals", "Blocked")) };

            var json = CreateSut().GetEffectiveRuleSetJson(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json, Does.Contain("\"version\""));
                Assert.That(json, Does.Contain("\"mode\""));
                Assert.That(json, Does.Contain("\"conditions\""));
                Assert.That(json, Does.Contain("\"fieldKey\""));
                Assert.That(json, Does.Contain("\"operator\""));
                Assert.That(json, Does.Contain("\"value\""));
                Assert.That(json, Does.Not.Contain("\"FieldKey\""),
                    "PascalCase keys break the frontend zod parse");
                Assert.That(json, Does.Not.Contain("\"Version\""));
            }
        }

        [Test]
        public void GetEffectiveRuleSetJson_RoundTripsStoredRuleSet_AsCamelCase()
        {
            var storedCamel = JsonSerializer.Serialize(
                new WorkItemRuleSet
                {
                    Mode = WorkItemRuleSet.ModeOr,
                    Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = "equals", Value = "Blocked" }],
                },
                CamelCase);
            var team = new Team { BlockedRuleSetJson = storedCamel };

            var json = CreateSut().GetEffectiveRuleSetJson(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json, Does.Contain("\"fieldKey\""));
                Assert.That(json, Does.Not.Contain("\"FieldKey\""));
            }
        }

        private static string RuleSetJson(params (string FieldKey, string Operator, string Value)[] conditions)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [.. conditions.Select(c => new WorkItemRuleCondition { FieldKey = c.FieldKey, Operator = c.Operator, Value = c.Value })],
            };

            return JsonSerializer.Serialize(ruleSet);
        }

        private static string AdditionalFieldNotEmptyRuleSet(int fieldId)
        {
            var ruleSet = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = $"additionalField.{fieldId}", Operator = "isnotempty", Value = string.Empty }],
            };

            return JsonSerializer.Serialize(ruleSet);
        }
    }
}
