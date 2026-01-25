using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class TerminologySeederTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        [TestCase("workItem", "Work Item")]
        [TestCase("workItems", "Work Items")]
        [TestCase("feature", "Feature")]
        [TestCase("features", "Features")]
        [TestCase("cycleTime", "Cycle Time")]
        [TestCase("throughput", "Throughput")]
        [TestCase("workInProgress", "Work In Progress")]
        [TestCase("wip", "WIP")]
        [TestCase("workItemAge", "Work Item Age")]
        [TestCase("tag", "Tag")]
        [TestCase("workTrackingSystem", "Work Tracking System")]
        [TestCase("workTrackingSystems", "Work Tracking Systems")]
        [TestCase("blocked", "Blocked")]
        [TestCase("serviceLevelExpectation", "Service Level Expectation")]
        [TestCase("sle", "SLE")]
        [TestCase("team", "Team")]
        [TestCase("teams", "Teams")]
        [TestCase("portfolio", "Portfolio")]
        [TestCase("portfolios", "Portfolios")]
        [TestCase("delivery", "Delivery")]
        [TestCase("deliveries", "Deliveries")]
        public async Task SeedAsync_AddsTerminology_WhenDatabaseIsEmpty(string key, string expectedDefaultValue)
        {
            var subject = CreateSubject();

            await subject.Seed();

            var entry = DatabaseContext.TerminologyEntries.Single(t => t.Key == key);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.DefaultValue, Is.EqualTo(expectedDefaultValue));
                Assert.That(entry.Description, Is.Not.Null.And.Not.Empty);
            }
        }

        [Test]
        public async Task SeedAsync_UpdatesDefaultValueAndDescription_WhenEntryExists()
        {
            // Arrange
            DatabaseContext.TerminologyEntries.Add(new TerminologyEntry
            {
                Key = "workItem",
                DefaultValue = "Old Value",
                Description = "Old Description"
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var entry = DatabaseContext.TerminologyEntries.Single(t => t.Key == "workItem");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(entry.DefaultValue, Is.EqualTo("Work Item"));
                Assert.That(entry.Description, Does.Contain("Units of Value"));
            }
        }

        [Test]
        public async Task SeedAsync_DoesNotDuplicate_WhenTerminologyAlreadyExists()
        {
            // Arrange
            DatabaseContext.TerminologyEntries.Add(new TerminologyEntry
            {
                Key = "team",
                DefaultValue = "Team",
                Description = "Existing description"
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var entries = DatabaseContext.TerminologyEntries
                .Where(t => t.Key == "team")
                .ToList();

            Assert.That(entries, Has.Count.EqualTo(1));
        }

        [Test]
        [TestCase("workItemQuery")]
        [TestCase("query")]
        public async Task SeedAsync_RemovesDeprecatedTerminology(string deprecatedKey)
        {
            // Arrange
            DatabaseContext.TerminologyEntries.Add(new TerminologyEntry
            {
                Key = deprecatedKey,
                DefaultValue = "Deprecated",
                Description = "Old terminology"
            });
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var deprecatedEntry = DatabaseContext.TerminologyEntries
                .FirstOrDefault(t => t.Key == deprecatedKey);

            Assert.That(deprecatedEntry, Is.Null);
        }

        [Test]
        public async Task SeedAsync_AddsAllExpectedTerminology()
        {
            var subject = CreateSubject();

            await subject.Seed();

            var entries = DatabaseContext.TerminologyEntries.ToList();
            
            // Should have exactly 21 terminology entries
            Assert.That(entries, Has.Count.EqualTo(21));
        }

        [Test]
        public async Task SeedAsync_CanBeCalledMultipleTimes_WithoutErrors()
        {
            var subject = CreateSubject();

            // Act
            await subject.Seed();
            await subject.Seed();
            await subject.Seed();

            // Assert
            var entries = DatabaseContext.TerminologyEntries.ToList();
            Assert.That(entries, Has.Count.EqualTo(21)); // Should still be 21, not duplicated
        }

        [Test]
        public async Task SeedAsync_RemovesMultipleDeprecatedEntries_InSingleOperation()
        {
            // Arrange
            var deprecatedKeys = new[] { "workItemQuery", "query" };
            
            foreach (var key in deprecatedKeys)
            {
                DatabaseContext.TerminologyEntries.Add(new TerminologyEntry
                {
                    Key = key,
                    DefaultValue = "Deprecated",
                    Description = "Old"
                });
            }
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var remainingDeprecated = DatabaseContext.TerminologyEntries
                .Where(t => deprecatedKeys.Contains(t.Key))
                .ToList();

            Assert.That(remainingDeprecated, Is.Empty);
        }

        [Test]
        public async Task SeedAsync_PreservesId_WhenUpdatingExistingEntry()
        {
            // Arrange
            DatabaseContext.TerminologyEntries.Add(new TerminologyEntry
            {
                Key = "portfolio",
                DefaultValue = "Old",
                Description = "Old"
            });
            await DatabaseContext.SaveChangesAsync();

            var originalId = DatabaseContext.TerminologyEntries.Single(t => t.Key == "portfolio").Id;
            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var entry = DatabaseContext.TerminologyEntries.Single(t => t.Key == "portfolio");
            Assert.That(entry.Id, Is.EqualTo(originalId)); // ID should not change
        }

        private TerminologySeeder CreateSubject()
        {
            return new TerminologySeeder(DatabaseContext, Mock.Of<ILogger<TerminologySeeder>>());
        }
    }
}