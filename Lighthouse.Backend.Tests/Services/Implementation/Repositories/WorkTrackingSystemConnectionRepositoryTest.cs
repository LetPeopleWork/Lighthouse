using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Lighthouse.Backend.WorkTracking;
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
            CollectionAssert.AreEquivalent(foundConnection.Options, new List<WorkTrackingSystemConnectionOption> { workTrackingOption });
        }

        private WorkTrackingSystemConnectionRepository CreateSubject()
        {
            return new WorkTrackingSystemConnectionRepository(DatabaseContext, Mock.Of<ILogger<WorkTrackingSystemConnectionRepository>>());
        }
    }
}
