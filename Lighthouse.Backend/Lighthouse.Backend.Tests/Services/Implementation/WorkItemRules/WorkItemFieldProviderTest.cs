using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WorkItemRules;
using Lighthouse.Backend.Services.Implementation.WorkItemRules;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItemRules
{
    [TestFixture]
    public class WorkItemFieldProviderTest
    {
        private static readonly string[] PriorityAndQ1Tags = ["Priority", "Q1"];

        private WorkItemFieldProvider subject;

        [SetUp]
        public void SetUp()
        {
            subject = new WorkItemFieldProvider();
        }

        [TestCase("workitem.type", "Bug")]
        [TestCase("workitem.state", "Active")]
        [TestCase("workitem.name", "Login fails on Safari")]
        [TestCase("workitem.referenceid", "WI-987")]
        [TestCase("workitem.parentreferenceid", "PARENT-77")]
        public void GetFieldValue_FixedFieldKey_ReturnsMatchingPropertyValue(string fieldKey, string expectedValue)
        {
            var workItem = new WorkItem
            {
                Type = "Bug",
                State = "Active",
                Name = "Login fails on Safari",
                ReferenceId = "WI-987",
                ParentReferenceId = "PARENT-77",
            };

            var result = subject.GetFieldValue(workItem, fieldKey);

            Assert.That(result, Is.EqualTo(expectedValue));
        }

        [Test]
        public void GetFieldValue_FixedFieldKey_IsCaseInsensitive()
        {
            var workItem = new WorkItem { Type = "Bug" };

            var result = subject.GetFieldValue(workItem, "WORKITEM.TYPE");

            Assert.That(result, Is.EqualTo("Bug"));
        }

        [Test]
        public void GetFieldValue_UnknownFieldKey_ReturnsEmptyString()
        {
            var workItem = new WorkItem { Type = "Bug" };

            var result = subject.GetFieldValue(workItem, "workitem.unknown");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldPresent_ReturnsValue()
        {
            var workItem = new WorkItem();
            workItem.AdditionalFieldValues[42] = "High";

            var result = subject.GetFieldValue(workItem, "additionalField.42");

            Assert.That(result, Is.EqualTo("High"));
        }

        [Test]
        public void GetFieldValue_AdditionalFieldNull_ReturnsEmptyString()
        {
            var workItem = new WorkItem();
            workItem.AdditionalFieldValues[42] = null;

            var result = subject.GetFieldValue(workItem, "additionalField.42");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldMissing_ReturnsEmptyString()
        {
            var workItem = new WorkItem();

            var result = subject.GetFieldValue(workItem, "additionalField.99");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFieldValue_AdditionalFieldKeyWithNonIntegerId_ReturnsEmptyString()
        {
            var workItem = new WorkItem { Type = "Bug" };

            var result = subject.GetFieldValue(workItem, "additionalField.notAnInt");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetTagsForField_TagsKey_ReturnsWorkItemTags()
        {
            var workItem = new WorkItem { Tags = ["Priority", "Q1"] };

            var result = subject.GetTagsForField(workItem, "workitem.tags");

            Assert.That(result, Is.EquivalentTo(PriorityAndQ1Tags));
        }

        [Test]
        public void GetTagsForField_NonTagsKey_ReturnsEmpty()
        {
            var workItem = new WorkItem { Tags = ["Priority"] };

            var result = subject.GetTagsForField(workItem, "workitem.type");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void GetFixedFields_ReturnsSixWorkItemFieldsVerbatim()
        {
            var fields = subject.GetFixedFields();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fields, Has.Count.EqualTo(6));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.type", DisplayName: "Type", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.state", DisplayName: "State", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.name", DisplayName: "Name", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.referenceid", DisplayName: "Reference ID", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.parentreferenceid", DisplayName: "Parent Reference ID", IsMultiValue: false }));
                Assert.That(fields, Has.Some.Matches<WorkItemRuleFieldDefinition>(f => f is { FieldKey: "workitem.tags", DisplayName: "Tags", IsMultiValue: true }));
            }
        }
    }
}
