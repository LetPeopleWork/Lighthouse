using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.Tests.Services.Implementation
{
    [TestFixture]
    public class DeliveryRuleServiceTest
    {
        private DeliveryRuleService subject;

        [SetUp]
        public void SetUp()
        {
            subject = new DeliveryRuleService();
        }

        [Test]
        public void ValidateAndEvaluate_EmptyRuleSet_ReturnsError()
        {
            var ruleSet = new DeliveryRuleSet { Conditions = [] };
            var features = new List<Feature> { CreateFeature("Feature1") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);
            
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ValidateAndEvaluate_TooManyRules_ReturnsError()
        {
            var conditions = Enumerable.Range(1, 21)
                .Select(_ => new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" })
                .ToList();
            var ruleSet = new DeliveryRuleSet { Conditions = conditions };
            var features = new List<Feature> { CreateFeature("Feature1") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);
            
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ValidateAndEvaluate_ValueTooLong_ReturnsError()
        {
            var longValue = new string('x', 501);
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = longValue }]
            };
            var features = new List<Feature> { CreateFeature("Feature1") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ValidateAndEvaluate_InvalidFieldKey_ReturnsError()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "invalid.field", Operator = "equals", Value = "test" }]
            };
            var features = new List<Feature> { CreateFeature("Feature1") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ValidateAndEvaluate_InvalidOperator_ReturnsError()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "greaterThan", Value = "test" }]
            };
            var features = new List<Feature> { CreateFeature("Feature1") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ValidateAndEvaluate_TypeEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" }]
            };
            var matchingFeature = CreateFeature("Feature1", type: "Epic");
            var nonMatchingFeature = CreateFeature("Feature2", type: "Story");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TypeEquals_IsCaseInsensitive()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "EPIC" }]
            };
            var matchingFeature = CreateFeature("Feature1", type: "epic");
            var features = new List<Feature> { matchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_StateEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.state", Operator = "equals", Value = "Active" }]
            };
            var matchingFeature = CreateFeature("Feature1", state: "Active");
            var nonMatchingFeature = CreateFeature("Feature2", state: "Done");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_NameEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.name", Operator = "equals", Value = "My Feature" }]
            };
            var matchingFeature = CreateFeature("My Feature");
            var nonMatchingFeature = CreateFeature("Other Feature");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_ReferenceIdEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.referenceId", Operator = "equals", Value = "REF-123" }]
            };
            var matchingFeature = CreateFeature("Feature1", referenceId: "REF-123");
            var nonMatchingFeature = CreateFeature("Feature2", referenceId: "REF-456");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_ParentReferenceIdEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.parentReferenceid", Operator = "equals", Value = "PARENT-1" }]
            };
            var matchingFeature = CreateFeature("Feature1", parentReferenceId: "PARENT-1");
            var nonMatchingFeature = CreateFeature("Feature2", parentReferenceId: "PARENT-2");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TagsEquals_MatchesFeatureWithExactTag()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.tags", Operator = "equals", Value = "Priority" }]
            };
            var matchingFeature = CreateFeature("Feature1", tags: ["Priority", "Q1"]);
            var nonMatchingFeature = CreateFeature("Feature2", tags: ["Q2"]);
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TagsEquals_IsCaseInsensitive()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.tags", Operator = "equals", Value = "PRIORITY" }]
            };
            var matchingFeature = CreateFeature("Feature1", tags: ["priority"]);
            var features = new List<Feature> { matchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TagsContains_MatchesAnyTagWithSubstring()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.tags", Operator = "contains", Value = "Pri" }]
            };
            var matchingFeature = CreateFeature("Feature1", tags: ["Priority", "Q1"]);
            var nonMatchingFeature = CreateFeature("Feature2", tags: ["Q2"]);
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TagsNotEquals_MatchesFeatureWithoutTag()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.tags", Operator = "notEquals", Value = "Blocked" }]
            };
            var matchingFeature = CreateFeature("Feature1", tags: ["Priority"]);
            var nonMatchingFeature = CreateFeature("Feature2", tags: ["Blocked"]);
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_TypeNotEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "notEquals", Value = "Bug" }]
            };
            var matchingFeature = CreateFeature("Feature1", type: "Epic");
            var nonMatchingFeature = CreateFeature("Feature2", type: "Bug");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_NameContains_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.name", Operator = "contains", Value = "Auth" }]
            };
            var matchingFeature = CreateFeature("Authentication Module");
            var nonMatchingFeature = CreateFeature("Payment Module");
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_NameContains_IsCaseInsensitive()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.name", Operator = "contains", Value = "AUTH" }]
            };
            var matchingFeature = CreateFeature("authentication module");
            var features = new List<Feature> { matchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_AdditionalFieldEquals_MatchesFeature()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "additionalField.42", Operator = "equals", Value = "High" }]
            };
            var matchingFeature = CreateFeature("Feature1", additionalFields: new Dictionary<int, string?> { { 42, "High" } });
            var nonMatchingFeature = CreateFeature("Feature2", additionalFields: new Dictionary<int, string?> { { 42, "Low" } });
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_AdditionalFieldNull_TreatedAsEmptyString()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "additionalField.42", Operator = "equals", Value = "" }]
            };
            var matchingFeature = CreateFeature("Feature1", additionalFields: new Dictionary<int, string?> { { 42, null } });
            var nonMatchingFeature = CreateFeature("Feature2", additionalFields: new Dictionary<int, string?> { { 42, "Value" } });
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_AdditionalFieldMissing_TreatedAsEmptyString()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "additionalField.42", Operator = "equals", Value = "" }]
            };
            var matchingFeature = CreateFeature("Feature1", additionalFields: new Dictionary<int, string?>());
            var nonMatchingFeature = CreateFeature("Feature2", additionalFields: new Dictionary<int, string?> { { 42, "Value" } });
            var features = new List<Feature> { matchingFeature, nonMatchingFeature };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_MultipleConditions_AppliesAndLogic()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions =
                [
                    new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" },
                    new DeliveryRuleCondition { FieldKey = "feature.state", Operator = "equals", Value = "Active" }
                ]
            };
            var matchingFeature = CreateFeature("Feature1", type: "Epic", state: "Active");
            var partialMatch1 = CreateFeature("Feature2", type: "Epic", state: "Done");
            var partialMatch2 = CreateFeature("Feature3", type: "Story", state: "Active");
            var features = new List<Feature> { matchingFeature, partialMatch1, partialMatch2 };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.ToList(), Has.Count.EqualTo(1));
                Assert.That(result, Does.Contain(matchingFeature));
            }
        }

        [Test]
        public void ValidateAndEvaluate_NoMatches_ReturnsValidWithEmptyList()
        {
            var ruleSet = new DeliveryRuleSet
            {
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "NonExistent" }]
            };
            var features = new List<Feature> { CreateFeature("Feature1", type: "Epic") };

            var result = subject.GetMatchingFeaturesForRuleset(ruleSet, features);

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetRuleSchema_ReturnsFixedFields()
        {
            var portfolio = CreatePortfolio();

            var schema = subject.GetRuleSchema(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f.FieldKey == "feature.type"));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f.FieldKey == "feature.state"));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f.FieldKey == "feature.name"));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f.FieldKey == "feature.referenceid"));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f.FieldKey == "feature.parentreferenceid"));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.tags", IsMultiValue: true }));
            }
        }

        [Test]
        public void GetRuleSchema_ReturnsOperators()
        {
            var portfolio = CreatePortfolio();

            var schema = subject.GetRuleSchema(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Operators, Does.Contain("equals"));
                Assert.That(schema.Operators, Does.Contain("notequals"));
                Assert.That(schema.Operators, Does.Contain("contains"));
            }
        }

        [Test]
        public void GetRuleSchema_ReturnsGuardrails()
        {
            var portfolio = CreatePortfolio();

            var schema = subject.GetRuleSchema(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.MaxRules, Is.EqualTo(20));
                Assert.That(schema.MaxValueLength, Is.EqualTo(500));
            }
        }

        [Test]
        public void GetRuleSchema_IncludesAdditionalFieldsFromConnection()
        {
            var portfolio = CreatePortfolioWithAdditionalFields(
                new AdditionalFieldDefinition { Id = 1, DisplayName = "Sprint" },
                new AdditionalFieldDefinition { Id = 2, DisplayName = "Priority" }
            );

            var schema = subject.GetRuleSchema(portfolio);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "additionalField.1", DisplayName: "Sprint" }));
                Assert.That(schema.Fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "additionalField.2", DisplayName: "Priority" }));
            }
        }

        private static Feature CreateFeature(
            string name,
            string type = "Feature",
            string state = "New",
            string referenceId = "",
            string parentReferenceId = "",
            List<string>? tags = null,
            Dictionary<int, string?>? additionalFields = null)
        {
            var feature = new Feature
            {
                Name = name,
                Type = type,
                State = state,
                ReferenceId = referenceId,
                ParentReferenceId = parentReferenceId,
                Tags = tags ?? []
            };

            if (additionalFields == null)
            {
                return feature;
            }

            foreach (var kvp in additionalFields)
            {
                feature.AdditionalFieldValues[kvp.Key] = kvp.Value;
            }

            return feature;
        }

        private static Portfolio CreatePortfolio()
        {
            var connection = new WorkTrackingSystemConnection();
            return new Portfolio
            {
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = connection
            };
        }

        private static Portfolio CreatePortfolioWithAdditionalFields(params AdditionalFieldDefinition[] fields)
        {
            var connection = new WorkTrackingSystemConnection();
            foreach (var field in fields)
            {
                connection.AdditionalFieldDefinitions.Add(field);
            }
            return new Portfolio
            {
                Name = "Test Portfolio",
                WorkTrackingSystemConnection = connection
            };
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_ManualDelivery_NotModified()
        {
            var portfolio = CreatePortfolio();
            portfolio.Features.Add(CreateFeature("Feature1", type: "Epic"));

            var delivery = new Delivery("Manual", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.Manual
            };
            delivery.Features.Add(CreateFeature("OriginalFeature"));

            subject.RecomputeRuleBasedDeliveries(portfolio, [delivery]);

            Assert.That(delivery.Features, Has.Count.EqualTo(1));
            Assert.That(delivery.Features[0].Name, Is.EqualTo("OriginalFeature"));
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_RuleBasedDelivery_UpdatesFeatures()
        {
            var portfolio = CreatePortfolio();
            portfolio.Features.Add(CreateFeature("Epic1", type: "Epic"));
            portfolio.Features.Add(CreateFeature("Bug1", type: "Bug"));
            portfolio.Features.Add(CreateFeature("Epic2", type: "Epic"));

            var ruleSet = new DeliveryRuleSet
            {
                Version = 1,
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" }]
            };

            var delivery = new Delivery("Rule-Based", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = System.Text.Json.JsonSerializer.Serialize(ruleSet),
                RuleSchemaVersion = 1
            };

            subject.RecomputeRuleBasedDeliveries(portfolio, [delivery]);

            Assert.That(delivery.Features, Has.Count.EqualTo(2));
            Assert.That(delivery.Features.Select(f => f.Name), Is.EquivalentTo(["Epic1", "Epic2"]));
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_NullRuleDefinitionJson_Skipped()
        {
            var portfolio = CreatePortfolio();
            portfolio.Features.Add(CreateFeature("Feature1"));

            var delivery = new Delivery("Rule-Based", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = null
            };

            subject.RecomputeRuleBasedDeliveries(portfolio, [delivery]);

            Assert.That(delivery.Features, Is.Empty);
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_ClearsExistingFeatures()
        {
            var portfolio = CreatePortfolio();
            portfolio.Features.Add(CreateFeature("NewFeature", type: "Epic"));

            var ruleSet = new DeliveryRuleSet
            {
                Version = 1,
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" }]
            };

            var delivery = new Delivery("Rule-Based", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = System.Text.Json.JsonSerializer.Serialize(ruleSet),
                RuleSchemaVersion = 1
            };
            delivery.Features.Add(CreateFeature("OldFeature"));

            subject.RecomputeRuleBasedDeliveries(portfolio, [delivery]);

            Assert.That(delivery.Features, Has.Count.EqualTo(1));
            Assert.That(delivery.Features[0].Name, Is.EqualTo("NewFeature"));
        }

        [Test]
        public void RecomputeRuleBasedDeliveries_MultipleDeliveries_AllProcessed()
        {
            var portfolio = CreatePortfolio();
            portfolio.Features.Add(CreateFeature("Epic1", type: "Epic"));
            portfolio.Features.Add(CreateFeature("Bug1", type: "Bug"));

            var epicRule = new DeliveryRuleSet
            {
                Version = 1,
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Epic" }]
            };
            var bugRule = new DeliveryRuleSet
            {
                Version = 1,
                Conditions = [new DeliveryRuleCondition { FieldKey = "feature.type", Operator = "equals", Value = "Bug" }]
            };

            var delivery1 = new Delivery("Epic Delivery", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = System.Text.Json.JsonSerializer.Serialize(epicRule),
                RuleSchemaVersion = 1
            };
            var delivery2 = new Delivery("Bug Delivery", DateTime.UtcNow.AddDays(30), 1)
            {
                SelectionMode = DeliverySelectionMode.RuleBased,
                RuleDefinitionJson = System.Text.Json.JsonSerializer.Serialize(bugRule),
                RuleSchemaVersion = 1
            };

            subject.RecomputeRuleBasedDeliveries(portfolio, [delivery1, delivery2]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(delivery1.Features, Has.Count.EqualTo(1));
                Assert.That(delivery1.Features[0].Name, Is.EqualTo("Epic1"));
                Assert.That(delivery2.Features, Has.Count.EqualTo(1));
                Assert.That(delivery2.Features[0].Name, Is.EqualTo("Bug1"));
            }
        }
    }
}
