using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.Models
{
    [TestFixture]
    public class TerminologyEntryTest
    {
        [Test]
        public void TerminologyEntry_ValidData_CreatedSuccessfully()
        {
            // Arrange & Act
            var entry = new TerminologyEntry
            {
                Key = "WorkItem",
                Description = "A single unit of work to be completed",
                DefaultValue = "Work Item"
            };

            // Assert
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.Key, Is.EqualTo("WorkItem"));
                Assert.That(entry.Description, Is.EqualTo("A single unit of work to be completed"));
                Assert.That(entry.DefaultValue, Is.EqualTo("Work Item"));
            }
        }

        [Test]
        public void TerminologyEntry_RequiredProperties_MustBeSet()
        {
            // Arrange & Act
            var entry = new TerminologyEntry();

            // Assert - These should be required properties
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.Key, Is.Not.Null);
                Assert.That(entry.Description, Is.Not.Null);  
                Assert.That(entry.DefaultValue, Is.Not.Null);
            }
        }

        [Test]
        public void TerminologyEntry_ImplementsIEntity_HasIdProperty()
        {
            // Arrange & Act
            var entry = new TerminologyEntry();

            // Assert
            Assert.That(entry, Is.InstanceOf<IEntity>());
            Assert.That(entry.Id, Is.GreaterThanOrEqualTo(0));
        }
    }
}
