using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkTrackingSystemConnectionTest
    {
        [Test]
        public void GetWorkTrackingSystemConnectionOptionByKey_NoOptionWithKey_Throws()
        {
            var subject = CreateSubject();

            Assert.Throws<ArgumentException>(() => subject.GetWorkTrackingSystemConnectionOptionByKey("MyKey"));
        }

        [Test]
        public void GetWorkTrackingSystemConnectionOptionByKey_OptionAvailable_ReturnsValue()
        {
            var subject = CreateSubject();
            var value = subject.GetWorkTrackingSystemConnectionOptionByKey("Key");

            Assert.That(value, Is.EqualTo("Value"));
        }

        [Test]
        public void AdditionalFieldDefinitions_EmptyByDefault()
        {
            var subject = new WorkTrackingSystemConnection();

            Assert.That(subject.AdditionalFieldDefinitions, Is.Empty);
        }

        [Test]
        public void AdditionalFieldDefinitions_CanAddFields()
        {
            var subject = new WorkTrackingSystemConnection();
            var field = new AdditionalFieldDefinition
            {
                DisplayName = "Iteration Path",
                Reference = "System.IterationPath"
            };

            subject.AdditionalFieldDefinitions.Add(field);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.AdditionalFieldDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.AdditionalFieldDefinitions[0].DisplayName, Is.EqualTo("Iteration Path"));
                Assert.That(subject.AdditionalFieldDefinitions[0].Reference, Is.EqualTo("System.IterationPath"));
            }
        }

        [Test]
        public void AdditionalFieldDefinitions_FieldIdIsStable_AfterDisplayNameEdit()
        {
            var subject = new WorkTrackingSystemConnection();
            var field = new AdditionalFieldDefinition
            {
                Id = 42,
                DisplayName = "Original Name",
                Reference = "some.ref"
            };
            subject.AdditionalFieldDefinitions.Add(field);
            var originalId = field.Id;

            subject.AdditionalFieldDefinitions[0].DisplayName = "New Name";

            Assert.That(subject.AdditionalFieldDefinitions[0].Id, Is.EqualTo(originalId));
        }

        [Test]
        public void AdditionalFieldDefinitions_FieldIdIsStable_AfterReferenceEdit()
        {
            var subject = new WorkTrackingSystemConnection();
            var field = new AdditionalFieldDefinition
            {
                Id = 42,
                DisplayName = "Field Name",
                Reference = "original.ref"
            };
            subject.AdditionalFieldDefinitions.Add(field);
            var originalId = field.Id;

            subject.AdditionalFieldDefinitions[0].Reference = "new.ref";

            Assert.That(subject.AdditionalFieldDefinitions[0].Id, Is.EqualTo(originalId));
        }

        private WorkTrackingSystemConnection CreateSubject()
        {
            var subject = new WorkTrackingSystemConnection();
            var option = new WorkTrackingSystemConnectionOption
            {
                Key = "Key",
                Value = "Value"
            };

            subject.Options.Add(option);

            return subject;
        }
    }
}
