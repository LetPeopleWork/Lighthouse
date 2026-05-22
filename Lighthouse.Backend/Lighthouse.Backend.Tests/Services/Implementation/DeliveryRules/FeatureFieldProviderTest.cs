using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.DeliveryRules;
using Lighthouse.Backend.Services.Implementation.DeliveryRules;

namespace Lighthouse.Backend.Tests.Services.Implementation.DeliveryRules
{
    [TestFixture]
    public class FeatureFieldProviderTest
    {
        private FeatureFieldProvider subject;

        [SetUp]
        public void SetUp()
        {
            subject = new FeatureFieldProvider();
        }

        [TestCase("feature.type", "Epic")]
        [TestCase("feature.state", "Active")]
        [TestCase("feature.name", "Authentication")]
        [TestCase("feature.referenceid", "REF-123")]
        [TestCase("feature.parentreferenceid", "PARENT-1")]
        public void GetFieldValue_FixedFieldKey_ReturnsMatchingPropertyValue(string fieldKey, string expectedValue)
        {
            var feature = new Feature
            {
                Type = "Epic",
                State = "Active",
                Name = "Authentication",
                ReferenceId = "REF-123",
                ParentReferenceId = "PARENT-1",
            };

            var result = subject.GetFieldValue(feature, fieldKey);

            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void GetFieldValue_FixedFieldKey_IsCaseInsensitive()
        {
            var feature = new Feature { Type = "Epic" };

            var result = subject.GetFieldValue(feature, "FEATURE.TYPE");

            Assert.That(result, Is.EqualTo("Epic"));
        }

        [Test]
        public void GetFieldValue_UnknownFieldKey_ReturnsEmptyString()
        {
            var feature = new Feature { Type = "Epic" };

            var result = subject.GetFieldValue(feature, "feature.unknown");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldPresent_ReturnsValue()
        {
            var feature = new Feature();
            feature.AdditionalFieldValues[42] = "High";

            var result = subject.GetFieldValue(feature, "additionalField.42");

            Assert.That(result, Is.EqualTo("High"));
        }

        [Test]
        public void GetFieldValue_AdditionalFieldNull_ReturnsEmptyString()
        {
            var feature = new Feature();
            feature.AdditionalFieldValues[42] = null;

            var result = subject.GetFieldValue(feature, "additionalField.42");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldMissing_ReturnsEmptyString()
        {
            var feature = new Feature();

            var result = subject.GetFieldValue(feature, "additionalField.99");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldKeyWithNonIntegerId_ReturnsEmptyString()
        {
            var feature = new Feature { Type = "Epic" };

            var result = subject.GetFieldValue(feature, "additionalField.notAnInt");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetTagsForField_TagsKey_ReturnsFeatureTags()
        {
            var feature = new Feature { Tags = ["Priority", "Q1"] };

            var result = subject.GetTagsForField(feature, "feature.tags");

            Assert.That(result, Is.EquivalentTo(new[] { "Priority", "Q1" }));
        }

        [Test]
        public void GetTagsForField_NonTagsKey_ReturnsEmpty()
        {
            var feature = new Feature { Tags = ["Priority"] };

            var result = subject.GetTagsForField(feature, "feature.type");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFixedFields_ReturnsSixFeatureFieldsVerbatim()
        {
            var fields = subject.GetFixedFields();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fields, Has.Count.EqualTo(6));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.type", DisplayName: "Type", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.state", DisplayName: "State", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.name", DisplayName: "Name", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.referenceid", DisplayName: "Reference ID", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.parentreferenceid", DisplayName: "Parent Reference ID", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<DeliveryRuleFieldDefinition>(f => f is { FieldKey: "feature.tags", DisplayName: "Tags", IsMultiValue: true }));
            }
        }
    }
}
