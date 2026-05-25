using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.Repositories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Repositories
{
    public class WorkItemStateTransitionRepositoryTest : IntegrationTestBase
    {
        public WorkItemStateTransitionRepositoryTest() : base()
        {
        }

        [Test]
        public async Task AddAndSave_PersistsTransitionRow_AndGetAllReturnsIt()
        {
            var workItem = await GivenPersistedWorkItem();
            var subject = CreateSubject();

            var transition = new WorkItemStateTransition
            {
                WorkItemId = workItem.Id,
                FromState = "Doing",
                ToState = "Done",
                TransitionedAt = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            };

            subject.Add(transition);
            await subject.Save();

            var savedTransition = subject.GetAll().Single();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(savedTransition.WorkItemId, Is.EqualTo(workItem.Id));
                Assert.That(savedTransition.FromState, Is.EqualTo("Doing"));
                Assert.That(savedTransition.ToState, Is.EqualTo("Done"));
                Assert.That(savedTransition.TransitionedAt, Is.EqualTo(transition.TransitionedAt));
            }
        }

        [Test]
        public async Task RemoveParentWorkItem_CascadesAndRemovesTransitionRow()
        {
            var workItem = await GivenPersistedWorkItem();
            var subject = CreateSubject();
            subject.Add(new WorkItemStateTransition
            {
                WorkItemId = workItem.Id,
                FromState = "Doing",
                ToState = "Done",
                TransitionedAt = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            });
            await subject.Save();

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            workItemRepository.Remove(workItem.Id);
            await workItemRepository.Save();

            Assert.That(subject.GetAll(), Is.Empty);
        }

        private async Task<WorkItem> GivenPersistedWorkItem()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.AzureDevOps };
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption { Key = "key", Value = "value" });
            var team = new Team { Name = "Name", WorkTrackingSystemConnection = workTrackingSystemConnection };

            var workItemBase = new WorkItemBase { ClosedDate = DateTime.UtcNow, StartedDate = DateTime.UtcNow, Name = "Item", Order = "12", ReferenceId = "1337", State = "Done", StateCategory = StateCategories.Done, Type = "Bug", Url = "https://letpeople.work/1886" };
            var workItem = new WorkItem(workItemBase, team);

            var workItemRepository = new WorkItemRepository(DatabaseContext, Mock.Of<ILogger<WorkItemRepository>>());
            workItemRepository.Add(workItem);
            await workItemRepository.Save();

            return workItem;
        }

        private WorkItemStateTransitionRepository CreateSubject()
        {
            return new WorkItemStateTransitionRepository(DatabaseContext, Mock.Of<ILogger<WorkItemStateTransitionRepository>>());
        }
    }
}
