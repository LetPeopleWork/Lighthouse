using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class WorkItemTest
    {
        [Test]
        public void Update_SetsParentsReference()
        {
            var otherItem = new WorkItem
            {
                ParentReferenceId = "123"
            };

            var item = CreateSubject();

            item.Update(otherItem);

            Assert.That(item.ParentReferenceId, Is.EqualTo(otherItem.ParentReferenceId));
        }

        [Test]
        public void Update_SetsTeamId()
        {
            var otherItem = new WorkItem
            {
                TeamId = 2
            };

            var item = CreateSubject();

            item.Update(otherItem);

            Assert.That(item.TeamId, Is.EqualTo(otherItem.TeamId));
        }

        [Test]
        public void Update_SetsTeam()
        {
            var otherItem = new WorkItem
            {
                Team = new Team { Id = 2, Name = "Team B" }
            };

            var item = CreateSubject();

            item.Update(otherItem);

            Assert.That(item.Team, Is.EqualTo(otherItem.Team));
        }

        [Test]
        public void Construct_FromWorkItemBaseCarryingSyncedTransitions_PreservesTransitions()
        {
            var transition = new WorkItemStateTransition
            {
                FromState = "To Do",
                ToState = "In Progress",
                TransitionedAt = new DateTime(2026, 5, 25, 8, 0, 0, DateTimeKind.Utc),
            };

            var workItemBase = new WorkItemBase
            {
                ReferenceId = "5025",
                Name = "Item",
                State = "In Progress",
                SyncedTransitions = [transition],
            };

            var item = new WorkItem(workItemBase, new Team { Id = 7, Name = "Team" });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.SyncedTransitions, Has.Count.EqualTo(1));
                Assert.That(item.SyncedTransitions[0].FromState, Is.EqualTo("To Do"));
                Assert.That(item.SyncedTransitions[0].ToState, Is.EqualTo("In Progress"));
                Assert.That(item.SyncedTransitions[0].TransitionedAt, Is.EqualTo(transition.TransitionedAt));
            }
        }

        // Blocked evaluation moved off the model into IBlockedItemService (ADR-067, single rule-based read
        // path). The former WorkItem.IsBlocked state/tag/combination cases now live in BlockedItemServiceTest
        // and the Slice01 rule-based acceptance scenarios.

        private static WorkItem CreateSubject()
        {
            return new WorkItem();
        }
    }
}
