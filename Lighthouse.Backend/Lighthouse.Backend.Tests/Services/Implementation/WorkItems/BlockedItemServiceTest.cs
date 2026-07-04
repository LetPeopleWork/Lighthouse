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
    /// provider inside the hexagon (classical TDD, no mocks). Pins the auto-migration synthesis and the
    /// Include (matched ⇒ blocked) evaluation the black-box acceptance tests only exercise by example.
    /// </summary>
    [TestFixture]
    [Category("epic-5074-blocked-items")]
    public class BlockedItemServiceTest
    {
        private static readonly JsonSerializerOptions CaseInsensitive = new() { PropertyNameCaseInsensitive = true };

        private static BlockedItemService CreateSut()
            => new(new RuleEvaluator<WorkItem>(), new WorkItemFieldProvider());

        [Test]
        public void GetEffectiveRuleSet_SynthesizesStateEqualsCondition_ForEachBlockedState()
        {
            var team = new Team { BlockedStates = ["Blocked", "On Hold"] };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            var tokens = ruleSet.Conditions.Select(c => $"{c.FieldKey} {c.Operator} {c.Value}").ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleSet.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
                Assert.That(tokens, Does.Contain("workitem.state equals Blocked"));
                Assert.That(tokens, Does.Contain("workitem.state equals On Hold"));
            }
        }

        [Test]
        public void GetEffectiveRuleSet_SynthesizesTagsContainsCondition_ForEachBlockedTag()
        {
            var team = new Team { BlockedTags = ["impediment", "waiting"] };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            var tokens = ruleSet.Conditions.Select(c => $"{c.FieldKey} {c.Operator} {c.Value}").ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleSet.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
                Assert.That(tokens, Does.Contain("workitem.tags contains impediment"));
                Assert.That(tokens, Does.Contain("workitem.tags contains waiting"));
            }
        }

        [Test]
        public void GetEffectiveRuleSet_ReturnsEmptyRuleSet_WhenLegacyConfigurationIsEmpty()
        {
            var team = new Team { BlockedStates = [], BlockedTags = [] };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ruleSet.Conditions, Is.Empty);
                Assert.That(ruleSet.Mode, Is.EqualTo(WorkItemRuleSet.ModeOr));
            }
        }

        [Test]
        public void GetEffectiveRuleSet_ReturnsStoredRuleSetVerbatim_WhenColumnAlreadySet()
        {
            var stored = new WorkItemRuleSet
            {
                Mode = WorkItemRuleSet.ModeOr,
                Conditions = [new WorkItemRuleCondition { FieldKey = "workitem.state", Operator = "equals", Value = "Stored" }],
            };
            var team = new Team
            {
                BlockedStates = ["Legacy"],
                BlockedTags = ["legacy-tag"],
                BlockedRuleSetJson = JsonSerializer.Serialize(stored),
            };

            var ruleSet = CreateSut().GetEffectiveRuleSet(team);

            var values = ruleSet.Conditions.Select(c => c.Value).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(values, Does.Contain("Stored"));
                Assert.That(values, Does.Not.Contain("Legacy"));
                Assert.That(values, Does.Not.Contain("legacy-tag"));
            }
        }

        [Test]
        public void GetEffectiveRuleSet_IsIdempotent_WhenReadRepeatedly()
        {
            var team = new Team { BlockedStates = ["Blocked"], BlockedTags = ["impediment"] };
            var sut = CreateSut();

            var first = JsonSerializer.Serialize(sut.GetEffectiveRuleSet(team), CaseInsensitive);
            var second = JsonSerializer.Serialize(sut.GetEffectiveRuleSet(team), CaseInsensitive);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void IsBlocked_ReturnsFalse_WhenRuleSetIsEmpty()
        {
            var team = new Team { BlockedStates = [], BlockedTags = [] };
            var item = new WorkItem { State = "In Progress", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.False);
        }

        [Test]
        public void IsBlocked_ReturnsTrue_WhenItemStateMatchesASynthesizedCondition()
        {
            var team = new Team { BlockedStates = ["Blocked"], BlockedTags = [] };
            var item = new WorkItem { State = "Blocked", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.True);
        }

        [Test]
        public void IsBlocked_ReturnsFalse_WhenItemStateDoesNotMatchAnyCondition()
        {
            var team = new Team { BlockedStates = ["Blocked"], BlockedTags = [] };
            var item = new WorkItem { State = "In Progress", Tags = [] };

            Assert.That(CreateSut().IsBlocked(item, team), Is.False);
        }

        [Test]
        public void IsBlocked_ReturnsTrue_WhenItemTagMatchesASynthesizedTagCondition()
        {
            var team = new Team { BlockedStates = [], BlockedTags = ["Blocked"] };
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
        public void IsBlocked_ForFeature_ReturnsTrue_WhenPortfolioBlockedStateMatches()
        {
            var portfolio = new Portfolio { BlockedStates = ["On Hold"], BlockedTags = [] };
            var feature = new Feature { State = "On Hold", Tags = [] };

            Assert.That(CreateSut().IsBlocked(feature, portfolio), Is.True);
        }

        [Test]
        public void IsBlocked_ForFeature_ReturnsFalse_WhenPortfolioHasNoBlockedConfiguration()
        {
            var portfolio = new Portfolio { BlockedStates = [], BlockedTags = [] };
            var feature = new Feature { State = "On Hold", Tags = [] };

            Assert.That(CreateSut().IsBlocked(feature, portfolio), Is.False);
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
