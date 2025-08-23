using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class WorkTrackingSystemConnectionRepositoryTest : IntegrationTestBase
    {
        public WorkTrackingSystemConnectionRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task GetWorkTrackingSystemConnectionById_ExistingId_RetunrsCorrectTeam()
        {
            var subject = CreateSubject();
            var workTrackingOption = new WorkTrackingSystemConnectionOption { Key = "key", Value = "value", IsSecret = false };
            var connection = new WorkTrackingSystemConnection { Name = "Test", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connection.Options.Add(workTrackingOption);

            subject.Add(connection);
            await subject.Save();

            var foundConnection = subject.GetById(connection.Id);

            Assert.That(foundConnection, Is.EqualTo(connection));
            Assert.That(foundConnection.Options, Is.EquivalentTo(new List<WorkTrackingSystemConnectionOption> { workTrackingOption }));
        }

        [Test]
        public async Task DoesNotEncryptNonSecretOptionsOnSave()
        {
            var optionValue = "This you can know";

            var subject = CreateSubject();
            var nonSecretOption = new WorkTrackingSystemConnectionOption { Key = "NotSecret", Value = optionValue, IsSecret = false };
            var connection = new WorkTrackingSystemConnection { Name = "Test", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connection.Options.Add(nonSecretOption);

            subject.Add(connection);
            await subject.Save();

            var storedOption = DatabaseContext.WorkTrackingSystemConnections.Include(w => w.Options).SelectMany(w => w.Options).Single();

            Assert.That(storedOption.Value, Is.EqualTo(optionValue));
        }

        [Test]
        public async Task ShouldEncryptSecretOptionsOnSave()
        {
            var optionValue = "you never will find out";

            var subject = CreateSubject();
            var option = new WorkTrackingSystemConnectionOption { Key = "secret", Value = optionValue, IsSecret = true };
            var connection = new WorkTrackingSystemConnection { Name = "Test", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            connection.Options.Add(option);

            subject.Add(connection);
            await subject.Save();

            var storedOption = DatabaseContext.WorkTrackingSystemConnections.Include(w => w.Options).SelectMany(w => w.Options).Single();

            Assert.That(storedOption.Value, Is.Not.EqualTo(optionValue));
        }

        [Test]
        public void Constructor_SeedsCsvConnection_WhenNotExists()
        {
            // Ensure no CSV connection exists initially
            var existingCsvConnections = DatabaseContext.WorkTrackingSystemConnections
                .Where(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv)
                .ToList();
            
            foreach (var connection in existingCsvConnections)
            {
                DatabaseContext.WorkTrackingSystemConnections.Remove(connection);
            }
            DatabaseContext.SaveChanges();

            // Creating the repository should trigger seeding
            _ = CreateSubject();

            var csvConnection = DatabaseContext.WorkTrackingSystemConnections
                .SingleOrDefault(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv);

            Assert.That(csvConnection, Is.Not.Null);
            Assert.That(csvConnection.Name, Is.EqualTo("CSV"));
            Assert.That(csvConnection.WorkTrackingSystem, Is.EqualTo(WorkTrackingSystems.Csv));
        }

        [Test]
        public void Constructor_DoesNotDuplicateCsvConnection_WhenAlreadyExists()
        {
            // First, create a repository to seed the CSV connection
            _ = CreateSubject();

            // Count CSV connections after first seeding
            var csvConnectionsAfterFirst = DatabaseContext.WorkTrackingSystemConnections
                .Where(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv)
                .ToList();

            // Create another repository instance
            _ = CreateSubject();

            // Count CSV connections after second repository creation
            var csvConnectionsAfterSecond = DatabaseContext.WorkTrackingSystemConnections
                .Where(c => c.WorkTrackingSystem == WorkTrackingSystems.Csv)
                .ToList();

            Assert.That(csvConnectionsAfterFirst.Count, Is.EqualTo(1));
            Assert.That(csvConnectionsAfterSecond.Count, Is.EqualTo(1));
            Assert.That(csvConnectionsAfterFirst.Single().Id, Is.EqualTo(csvConnectionsAfterSecond.Single().Id));
        }

        private WorkTrackingSystemConnectionRepository CreateSubject()
        {
            return new WorkTrackingSystemConnectionRepository(DatabaseContext, Mock.Of<ILogger<WorkTrackingSystemConnectionRepository>>());
        }
    }
}
