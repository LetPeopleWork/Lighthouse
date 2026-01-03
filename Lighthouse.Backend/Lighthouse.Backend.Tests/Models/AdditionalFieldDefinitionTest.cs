using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class AdditionalFieldDefinitionTest
    {
        [Test]
        public void Constructor_CreatesWithDefaultId_IdIsZero()
        {
            var subject = new AdditionalFieldDefinition();

            Assert.That(subject.Id, Is.Zero);
        }

        [Test]
        public void Constructor_WithExplicitId_PreservesId()
        {
            var expectedId = 42;
            var subject = new AdditionalFieldDefinition { Id = expectedId };

            Assert.That(subject.Id, Is.EqualTo(expectedId));
        }

        [Test]
        public void DisplayName_CanBeEdited_WithoutChangingId()
        {
            var subject = new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Original Name",
                Reference = "original.ref"
            };
            var originalId = subject.Id;

            subject.DisplayName = "New Name";

            Assert.That(subject.Id, Is.EqualTo(originalId));
            Assert.That(subject.DisplayName, Is.EqualTo("New Name"));
        }

        [Test]
        public void Reference_CanBeEdited_WithoutChangingId()
        {
            var subject = new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Field Name",
                Reference = "original.ref"
            };
            var originalId = subject.Id;

            subject.Reference = "new.ref";

            Assert.That(subject.Id, Is.EqualTo(originalId));
            Assert.That(subject.Reference, Is.EqualTo("new.ref"));
        }
    }
}
