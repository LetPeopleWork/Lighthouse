using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Implementation.Seeding;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Seeding
{
    public class WorkTrackingSystemConnectionSeederTests() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task SeedAsync_CreatesDefaultCsvConnection_WhenDatabaseIsEmpty()
        {
            var subject = CreateSubject();

            await subject.Seed();

            var connections = await DatabaseContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .ToListAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connections, Has.Count.EqualTo(1));
                Assert.That(connections[0].Name, Is.EqualTo("CSV Azure DevOps"));
                Assert.That(connections[0].WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Csv));
            }
        }

        [Test]
        [TestCase(CsvWorkTrackingOptionNames.Delimiter, ",")]
        [TestCase(CsvWorkTrackingOptionNames.DateTimeFormat, "d.M.yyyy HH:mm:ss")]
        [TestCase(CsvWorkTrackingOptionNames.TagSeparator, ";")]
        [TestCase(CsvWorkTrackingOptionNames.IdHeader, "ID")]
        [TestCase(CsvWorkTrackingOptionNames.NameHeader, "Title")]
        [TestCase(CsvWorkTrackingOptionNames.StateHeader, "State")]
        [TestCase(CsvWorkTrackingOptionNames.TypeHeader, "Work Item Type")]
        [TestCase(CsvWorkTrackingOptionNames.StartedDateHeader, "Activated Date")]
        [TestCase(CsvWorkTrackingOptionNames.ClosedDateHeader, "Closed Date")]
        [TestCase(CsvWorkTrackingOptionNames.CreatedDateHeader, "Created Date")]
        [TestCase(CsvWorkTrackingOptionNames.ParentReferenceIdHeader, "Parent")]
        [TestCase(CsvWorkTrackingOptionNames.TagsHeader, "Tags")]
        [TestCase(CsvWorkTrackingOptionNames.UrlHeader, "Url")]
        public async Task SeedAsync_SetsCorrectAzureDevOpsOptions(string optionKey, string expectedValue)
        {
            var subject = CreateSubject();

            await subject.Seed();

            var connection = await DatabaseContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .SingleAsync();

            var option = connection.Options.Single(o => o.Key == optionKey);
            Assert.That(option.Value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(CsvWorkTrackingOptionNames.OwningTeamHeader)]
        [TestCase(CsvWorkTrackingOptionNames.EstimatedSizeHeader)]
        public async Task SeedAsync_SetsEmptyStringForOptionalHeaders(string optionKey)
        {
            var subject = CreateSubject();

            await subject.Seed();

            var connection = await DatabaseContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .SingleAsync();

            var option = connection.Options.Single(o => o.Key == optionKey);
            Assert.That(option.Value, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task SeedAsync_DoesNotCreateDuplicates_WhenConnectionsAlreadyExist()
        {
            // Arrange
            var factory = ServiceProvider.GetService<IWorkTrackingSystemFactory>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Factory");
            var existingConnection = factory.CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems.Csv);
            existingConnection.Name = "Existing Connection";
            
            DatabaseContext.WorkTrackingSystemConnections.Add(existingConnection);
            await DatabaseContext.SaveChangesAsync();

            var subject = CreateSubject();

            // Act
            await subject.Seed();

            // Assert
            var connections = await DatabaseContext.WorkTrackingSystemConnections.ToListAsync();
            Assert.That(connections, Has.Count.EqualTo(1)); // Should not duplicate
            Assert.That(connections[0].Name, Is.EqualTo("Existing Connection")); // Original preserved
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
            var connections = await DatabaseContext.WorkTrackingSystemConnections.ToListAsync();
            Assert.That(connections, Has.Count.EqualTo(1)); // Should still be 1, not duplicated
        }

        [Test]
        public async Task SeedAsync_IncludesAllRequiredOptions()
        {
            var subject = CreateSubject();

            await subject.Seed();

            var connection = await DatabaseContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .SingleAsync();

            var expectedOptions = new[]
            {
                CsvWorkTrackingOptionNames.Delimiter,
                CsvWorkTrackingOptionNames.DateTimeFormat,
                CsvWorkTrackingOptionNames.TagSeparator,
                CsvWorkTrackingOptionNames.IdHeader,
                CsvWorkTrackingOptionNames.NameHeader,
                CsvWorkTrackingOptionNames.StateHeader,
                CsvWorkTrackingOptionNames.TypeHeader,
                CsvWorkTrackingOptionNames.StartedDateHeader,
                CsvWorkTrackingOptionNames.ClosedDateHeader,
                CsvWorkTrackingOptionNames.CreatedDateHeader,
                CsvWorkTrackingOptionNames.ParentReferenceIdHeader,
                CsvWorkTrackingOptionNames.TagsHeader,
                CsvWorkTrackingOptionNames.UrlHeader,
                CsvWorkTrackingOptionNames.OwningTeamHeader,
                CsvWorkTrackingOptionNames.EstimatedSizeHeader
            };

            foreach (var expectedOption in expectedOptions)
            {
                Assert.That(connection.Options.Any(o => o.Key == expectedOption), 
                    Is.True, 
                    $"Option '{expectedOption}' should exist");
            }
        }

        [Test]
        public async Task SeedAsync_LoadsOptionsAndFieldDefinitions()
        {
            var subject = CreateSubject();

            await subject.Seed();

            var connection = await DatabaseContext.WorkTrackingSystemConnections
                .Include(c => c.Options)
                .Include(c => c.AdditionalFieldDefinitions)
                .SingleAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(connection.Options, Is.Not.Null.And.Not.Empty);
                // AdditionalFieldDefinitions may be empty for CSV, but should not be null
                Assert.That(connection.AdditionalFieldDefinitions, Is.Not.Null);
            }
        }

        private WorkTrackingSystemConnectionSeeder CreateSubject()
        {
            var factory = ServiceProvider.GetService<IWorkTrackingSystemFactory>() ?? throw new InvalidOperationException("Could not resolve Work Tracking System Factory");
            return new WorkTrackingSystemConnectionSeeder(
                DatabaseContext, 
                factory, 
                Mock.Of<ILogger<WorkTrackingSystemConnectionSeeder>>());
        }
    }
}