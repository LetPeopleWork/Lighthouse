using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    [TestFixture]
    public class ForecastFilterRuleServiceIntegrationTest
    {
        private Mock<ILicenseService> licenseServiceMock;

        [SetUp]
        public void SetUp()
        {
            licenseServiceMock = new Mock<ILicenseService>();
        }

        [Test]
        public void GetEffectiveRuleSet_FreeTenantWithPersistedRuleSet_ReturnsNull()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            var team = CreateTeam(forecastFilterRuleSetJson: SerializeRuleSet(CreateNonEmptyRuleSet()));
            var subject = CreateSubject();

            var result = subject.GetEffectiveRuleSet(team);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEffectiveRuleSet_PremiumTenantNullJson_ReturnsNull()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            var team = CreateTeam(forecastFilterRuleSetJson: null);
            var subject = CreateSubject();

            var result = subject.GetEffectiveRuleSet(team);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEffectiveRuleSet_PremiumTenantZeroConditions_ReturnsNull()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            var emptyRuleSet = new WorkItemRuleSet { Conditions = [] };
            var team = CreateTeam(forecastFilterRuleSetJson: SerializeRuleSet(emptyRuleSet));
            var subject = CreateSubject();

            var result = subject.GetEffectiveRuleSet(team);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetEffectiveRuleSet_PremiumTenantNonEmptyRuleSet_ReturnsDeserialisedRuleSet()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            var ruleSet = CreateNonEmptyRuleSet();
            var team = CreateTeam(forecastFilterRuleSetJson: SerializeRuleSet(ruleSet));
            var subject = CreateSubject();

            var result = subject.GetEffectiveRuleSet(team);

            Assert.That(result, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(result!.Conditions, Has.Count.EqualTo(1));
                Assert.That(result.Conditions[0].FieldKey, Is.EqualTo("workitem.type"));
                Assert.That(result.Conditions[0].Operator, Is.EqualTo("equals"));
                Assert.That(result.Conditions[0].Value, Is.EqualTo("Bug"));
            }
        }

        [Test]
        public void GetSchema_ReturnsFixedWorkItemFields()
        {
            var team = CreateTeam();
            var subject = CreateSubject();

            var schema = subject.GetSchema(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f.FieldKey == "workitem.type"));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f.FieldKey == "workitem.state"));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f.FieldKey == "workitem.name"));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f.FieldKey == "workitem.referenceid"));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f.FieldKey == "workitem.parentreferenceid"));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.tags", IsMultiValue: true }));
                Assert.That(schema.Operators, Does.Contain("equals"));
                Assert.That(schema.Operators, Does.Contain("notequals"));
                Assert.That(schema.Operators, Does.Contain("contains"));
                Assert.That(schema.MaxRules, Is.EqualTo(WorkItemRuleSet.MaxRules));
                Assert.That(schema.MaxValueLength, Is.EqualTo(WorkItemRuleSet.MaxValueLength));
            }
        }

        [Test]
        public void GetSchema_IncludesAdditionalFieldsFromTeamConnection()
        {
            var team = CreateTeamWithAdditionalFields(
                new AdditionalFieldDefinition { Id = 7, DisplayName = "Sprint" },
                new AdditionalFieldDefinition { Id = 13, DisplayName = "Priority" });
            var subject = CreateSubject();

            var schema = subject.GetSchema(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "additionalField.7", DisplayName: "Sprint" }));
                Assert.That(schema.Fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "additionalField.13", DisplayName: "Priority" }));
            }
        }

        [Test]
        public void Filter_MatchingRule_ExcludesMatchedItems()
        {
            var subject = CreateSubject();
            var bug = new WorkItem { ReferenceId = "BUG-1", Type = "Bug", Name = "Crash" };
            var story = new WorkItem { ReferenceId = "US-1", Type = "User Story", Name = "Feature A" };
            var anotherStory = new WorkItem { ReferenceId = "US-2", Type = "User Story", Name = "Feature B" };
            var items = new[] { bug, story, anotherStory };

            var result = subject.Filter(items, CreateNonEmptyRuleSet()).ToList();

            Assert.That(result.Select(i => i.ReferenceId), Is.EquivalentTo(new[] { "US-1", "US-2" }));
        }

        [Test]
        public void Filter_NoMatchingRule_ReturnsAllItemsUnchanged()
        {
            var subject = CreateSubject();
            var storyOne = new WorkItem { ReferenceId = "US-1", Type = "User Story", Name = "Feature A" };
            var storyTwo = new WorkItem { ReferenceId = "US-2", Type = "User Story", Name = "Feature B" };
            var items = new[] { storyOne, storyTwo };

            var result = subject.Filter(items, CreateNonEmptyRuleSet()).ToList();

            Assert.That(result.Select(i => i.ReferenceId), Is.EquivalentTo(new[] { "US-1", "US-2" }));
        }

        [Test]
        public void Filter_RuleMatchesAllItems_ReturnsEmptyEnumeration()
        {
            var subject = CreateSubject();
            var bugOne = new WorkItem { ReferenceId = "BUG-1", Type = "Bug", Name = "Crash" };
            var bugTwo = new WorkItem { ReferenceId = "BUG-2", Type = "Bug", Name = "Leak" };
            var items = new[] { bugOne, bugTwo };

            var result = subject.Filter(items, CreateNonEmptyRuleSet()).ToList();

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void LicenseDowngrade_PreservesPersistedRuleSet_GetEffectiveReturnsNull()
        {
            var ruleSet = CreateNonEmptyRuleSet();
            var team = CreateTeam(forecastFilterRuleSetJson: SerializeRuleSet(ruleSet));
            var subject = CreateSubject();

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            var beforeDowngrade = subject.GetEffectiveRuleSet(team);

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            var afterDowngrade = subject.GetEffectiveRuleSet(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beforeDowngrade, Is.Not.Null);
                Assert.That(afterDowngrade, Is.Null);
                Assert.That(team.ForecastFilterRuleSetJson, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public void LicenseReUpgrade_AfterDowngrade_GetEffectiveReturnsOriginalRuleSet()
        {
            var ruleSet = CreateNonEmptyRuleSet();
            var team = CreateTeam(forecastFilterRuleSetJson: SerializeRuleSet(ruleSet));
            var subject = CreateSubject();

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(false);
            var duringDowngrade = subject.GetEffectiveRuleSet(team);

            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            var afterReUpgrade = subject.GetEffectiveRuleSet(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(duringDowngrade, Is.Null);
                Assert.That(afterReUpgrade, Is.Not.Null);
                Assert.That(afterReUpgrade!.Conditions, Has.Count.EqualTo(1));
                Assert.That(afterReUpgrade.Conditions[0].FieldKey, Is.EqualTo("workitem.type"));
                Assert.That(afterReUpgrade.Conditions[0].Value, Is.EqualTo("Bug"));
            }
        }
        
        [Test]
        public void GetEffectiveRuleSet_PremiumTenantCamelCaseJson_ReturnsDeserialisedRuleSet()
        {
            licenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
            // Simulates JSON as stored/sent from the frontend (camelCase keys)
            const string camelCaseJson = """{"version":1,"conditions":[{"fieldKey":"workitem.type","operator":"equals","value":"Bug"}]}""";
            var team = CreateTeam(forecastFilterRuleSetJson: camelCaseJson);
            var subject = CreateSubject();

            var result = subject.GetEffectiveRuleSet(team);

            Assert.That(result, Is.Not.Null);
        }

        private ForecastFilterRuleService CreateSubject()
        {
            return new ForecastFilterRuleService(
                new RuleEvaluator<WorkItem>(),
                new WorkItemFieldProvider(),
                licenseServiceMock.Object);
        }

        private static Team CreateTeam(string? forecastFilterRuleSetJson = null)
        {
            return new Team
            {
                Name = "Test Team",
                ForecastFilterRuleSetJson = forecastFilterRuleSetJson,
                WorkTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Conn" }
            };
        }

        private static Team CreateTeamWithAdditionalFields(params AdditionalFieldDefinition[] fields)
        {
            var connection = new WorkTrackingSystemConnection { Name = "Conn" };
            foreach (var field in fields)
            {
                connection.AdditionalFieldDefinitions.Add(field);
            }
            return new Team
            {
                Name = "Test Team",
                WorkTrackingSystemConnection = connection
            };
        }

        private static WorkItemRuleSet CreateNonEmptyRuleSet()
        {
            return new WorkItemRuleSet
            {
                Conditions =
                [
                    new WorkItemRuleCondition { FieldKey = "workitem.type", Operator = "equals", Value = "Bug" }
                ]
            };
        }

        private static string SerializeRuleSet(WorkItemRuleSet ruleSet)
        {
            return JsonSerializer.Serialize(ruleSet);
        }
    }
}
