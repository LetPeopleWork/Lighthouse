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

        private static WorkItem CreateSubject()
        {
            return new WorkItem();
        }
    }
}
