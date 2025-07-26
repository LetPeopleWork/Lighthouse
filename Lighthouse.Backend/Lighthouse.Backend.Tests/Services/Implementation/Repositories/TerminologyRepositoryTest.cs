using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    [TestFixture]
    public class TerminologyRepositoryTest : IntegrationTestBase
    {
        public TerminologyRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        private TerminologyRepository CreateSubject()
        {
            return new TerminologyRepository(DatabaseContext, Mock.Of<ILogger<TerminologyRepository>>());
        }

        [Test]
        public async Task SeedTerminology_SeededOnConstruction_AddsDefaultTerminologyEntries()
        {
            var subject = CreateSubject();

            await subject.Save();

            var entries = subject.GetAll();
            
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries.Count(), Is.GreaterThanOrEqualTo(2));
                
                var workItemEntry = entries.FirstOrDefault(e => e.Key == "workItem");
                Assert.That(workItemEntry, Is.Not.Null);
                Assert.That(workItemEntry.DefaultValue, Is.EqualTo("Work Item"));
                Assert.That(workItemEntry.Value, Is.Empty);
                Assert.That(workItemEntry.Description, Is.EqualTo("High level name of item that a team works on"));
                
                var workItemsEntry = entries.FirstOrDefault(e => e.Key == "workItems");
                Assert.That(workItemsEntry, Is.Not.Null);
                Assert.That(workItemsEntry.DefaultValue, Is.EqualTo("Work Items"));
                Assert.That(workItemsEntry.Value, Is.Empty);
                Assert.That(workItemsEntry.Description, Is.EqualTo("Plural form of Work Item"));
            }
        }

        [Test]
        public async Task SeedTerminology_DoesNotOverwriteExistingEntries()
        {
            // Arrange
            var subject = CreateSubject();
            await subject.Save(); // Initial seeding
            
            // Modify an existing entry
            var workItemEntry = subject.GetByPredicate(e => e.Key == "workItem");
            workItemEntry.DefaultValue = "Custom Work Item";
            subject.Update(workItemEntry);
            await subject.Save();
            
            // Act - Create new repository instance (triggers seeding again)
            var newSubject = CreateSubject();
            await newSubject.Save();
            
            // Assert
            var updatedEntry = newSubject.GetByPredicate(e => e.Key == "workItem");
            Assert.That(updatedEntry.DefaultValue, Is.EqualTo("Custom Work Item"), 
                "Seeding should not overwrite existing entries");
        }

        [Test]
        public async Task SeedTerminology_AddsOnlyMissingEntries()
        {
            // Arrange
            var subject = CreateSubject();
            
            // Add one entry manually before seeding
            subject.Add(new TerminologyEntry
            {
                Key = "feature",
                DefaultValue = "Feature",
                Value = "Epic",
                Description = "Manually added entry"
            });
            await subject.Save();
            
            // Act - Create new repository instance (triggers seeding)
            var newSubject = CreateSubject();
            await newSubject.Save();
            
            // Assert
            var entries = newSubject.GetAll();
            var workItemEntry = entries.First(e => e.Key == "workItem");
            var newEntry = entries.First(e => e.Key == "feature");
            
            using (Assert.EnterMultipleScope())
            {
                // Manual entry should be preserved
                Assert.That(workItemEntry.DefaultValue, Is.EqualTo("Work Item"));
                
                // Missing entry should be added with default value
                Assert.That(newEntry.DefaultValue, Is.EqualTo("Feature"));
            }
        }
    }
}
