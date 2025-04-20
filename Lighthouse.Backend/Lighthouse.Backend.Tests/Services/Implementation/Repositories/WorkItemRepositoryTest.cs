using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class WorkItemRepositoryTest : IntegrationTestBase
    {
        public WorkItemRepositoryTest() : base(new TestWebApplicationFactory<Program>())
        {
        }

        [Test]
        public async Task GetAll_IncludesTeams()
        {
            var subject = CreateSubject();

            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "key", Value = "value" });
            var team = new Team { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var workItemBase = new WorkItemBase { ClosedDate = DateTime.UtcNow, StartedDate = DateTime.UtcNow, Name = "Item", Order = "12", ReferenceId = "1337", State = "Done", StateCategory = StateCategories.Done, Type = "Bug", Url = "https://letpeople.work/1886" };
            var workItem = new WorkItem(workItemBase, team);

            subject.Add(workItem);
            await subject.Save();

            var savedWorkItems = subject.GetAll();
            var savedWorkItem = savedWorkItems.FirstOrDefault();

            Assert.That(savedWorkItem.Team, Is.EqualTo(team));
        }

        private WorkItemRepository CreateSubject()
        {
            return new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
        }
    }
}
