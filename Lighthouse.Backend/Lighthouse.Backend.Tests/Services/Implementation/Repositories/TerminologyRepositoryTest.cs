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
        [TestCase("workItem", "Work Item", "Units of Value that move through your system and that your teams work on. Alternatives may be 'Story' or 'Issue'")]
        [TestCase("workItems", "Work Items", "Plural form of 'Work Item'")]
        [TestCase("feature", "Feature", "Larger unit of work that contains multiple Work Items. Alternatives may be 'Epic' or 'Theme'")]
        [TestCase("features", "Features", "Plural form of 'Feature'")]
        [TestCase("cycleTime", "Cycle Time", "The elapsed time between when a work item started and when a work item finished. Alternatives may be 'Lead Time' or 'Flow Time'")]
        [TestCase("throughput", "Throughput", "The number of work items finished per unit of time. Alternatives may be 'Delivery Rate' or 'Flow Velocity'")]
        [TestCase("workInProgress", "Work In Progress", "The number of work items started but not finished. Alternatives may be 'Flow Load' or 'Ongoing Stuff'")]
        [TestCase("wip", "WIP", "Abbreviation of 'Work In Progress'.")]
        [TestCase("workItemAge", "Work Item Age", "The elapsed time between when a work item started and the current date. Alternatives may be 'Age' or 'In Progress Time'")]
        [TestCase("tag", "Tag", "A user defined indication on your 'Work Items'. Alternatives may be 'Label' or 'Category'")]
        [TestCase("workTrackingSystem", "Work Tracking System", "Generic name of the source of your data. Alternatives may be 'Jira Instance' or 'Azure DevOps Organization'")]
        [TestCase("workTrackingSystems", "Work Tracking Systems", "Plural form of 'Work Tracking System'")]
        [TestCase("query", "Query", "Query that is applied on your 'Work Tracking System' for filtering. Alternatives may be 'JQL' or 'WIQL'")]
        [TestCase("blocked", "Blocked", "Indication for 'Work Items' that don't progress anymore. Alternatives may be 'On Hold' or 'Stopped'")]
        [TestCase("serviceLevelExpectation", "Service Level Expectation", "A forecast of how long it should take a work item to flow from started to finished. Alternatives may be 'Target' or 'Goal'")]
        [TestCase("sle", "SLE", "Abbreviation of 'Service Level Expectation'")]
        [TestCase("team", "Team", "The smallest groups in the organization that deliver 'Work Items'. Alternatives may be 'Squad' or 'Crew'")]
        [TestCase("teams", "Teams", "Plural form of 'Team'")]
        [TestCase("portfolio", "Portfolio", "Collection of work items that belong together and are managed as a unit. Alternatives may be 'Project' or 'Initiative'")]
        [TestCase("portfolios", "Portfolios", "Plural form of 'Portfolio'")]
        [TestCase("delivery", "Delivery", "A delivery marks a specific point in time where a defined list of Features should be done. Alternative names may be Milestone, Checkpoint, etc.")]
        [TestCase("deliveries", "Deliveries", "Plural form of 'Delivery'")]
        public async Task SeedTerminology_ContainsAllExpectedKeysWithDefaults(string key, string defaultValue, string description)
        {
            var subject = CreateSubject();

            await subject.Save();

            var entries = subject.GetAll();
            using (Assert.EnterMultipleScope())
            {
                var entry = entries.Single(e => e.Key == key);

                Assert.That(entry.Key, Is.EqualTo(key));
                Assert.That(entry.DefaultValue, Is.EqualTo(defaultValue));
                Assert.That(entry.Value, Is.Empty);
                Assert.That(entry.Description, Is.EqualTo(description));
            }
        }

        [Test]
        public async Task SeedTerminology_OverwriteExistingEntries()
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
            Assert.That(updatedEntry.DefaultValue, Is.EqualTo("Work Item"), 
                "Seeding should overwrite existing entries");
        }

        [Test]
        public async Task SeedTerminology_AddsOnlyMissingEntries()
        {
            // Arrange
            var subject = CreateSubject();
            
            // Add one entry manually before seeding
            subject.Add(new TerminologyEntry
            {
                Key = "feature2",
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
            var newEntry = entries.First(e => e.Key == "feature2");
            
            using (Assert.EnterMultipleScope())
            {
                // Manual entry should be preserved
                Assert.That(workItemEntry.DefaultValue, Is.EqualTo("Work Item"));
                
                // Missing entry should be added with default value
                Assert.That(newEntry.DefaultValue, Is.EqualTo("Feature"));
            }
        }

        [Test]
        public async Task SeedTerminology_UpdatesDefaultValueAndDescriptionForExistingEntries()
        {
            // Arrange - Create initial repository with seeded data
            var subject = CreateSubject();
            await subject.Save();
            
            // Manually modify the seeded entry to have different default values and description
            var workItemEntry = subject.GetByPredicate(e => e.Key == "workItem");
            var originalDefaultValue = workItemEntry.DefaultValue;
            var originalDescription = workItemEntry.Description;
            var userCustomValue = workItemEntry.Value;
            
            // Simulate old/outdated seeded values
            workItemEntry.DefaultValue = "Old Work Item";
            workItemEntry.Description = "Old description that should be updated";
            subject.Update(workItemEntry);
            await subject.Save();
            
            // Act - Create new repository instance which triggers seeding and should update the entry
            var newSubject = CreateSubject();
            await newSubject.Save();
            
            // Assert
            var updatedEntry = newSubject.GetByPredicate(e => e.Key == "workItem");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(updatedEntry.Key, Is.EqualTo("workItem"));
                Assert.That(updatedEntry.DefaultValue, Is.EqualTo(originalDefaultValue), 
                    "DefaultValue should be updated to the current seeded value");
                Assert.That(updatedEntry.Description, Is.EqualTo(originalDescription), 
                    "Description should be updated to the current seeded description");
                Assert.That(updatedEntry.Value, Is.EqualTo(userCustomValue), 
                    "User's custom Value should be preserved during update");
            }
        }

        [Test]
        public async Task SeedTerminology_EntriesHaveAutoIncrementingIds()
        {
            // Arrange & Act
            var subject = CreateSubject();
            await subject.Save();
            
            // Assert
            var entries = subject.GetAll().ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entries, Is.Not.Empty);
                
                // Verify all entries have positive auto-incremented IDs
                foreach (var entry in entries)
                {
                    Assert.That(entry.Id, Is.GreaterThan(0), $"Entry {entry.Key} should have auto-incremented ID");
                }
                
                // Verify IDs are unique
                var ids = entries.Select(e => e.Id).ToList();
                var uniqueIds = ids.Distinct().ToList();
                Assert.That(uniqueIds, Has.Count.EqualTo(ids.Count), "All IDs should be unique");
                
                // Verify entries are ordered by creation (first entry should have smallest ID)
                var sortedIds = ids.OrderBy(id => id).ToList();
                Assert.That(ids, Is.EqualTo(sortedIds), "Entries should be ordered by ID (creation order)");
            }
        }
    }
}
