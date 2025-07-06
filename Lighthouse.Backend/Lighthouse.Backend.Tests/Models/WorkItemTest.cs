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
        public void IsBlocked_TeamHasNoBlockedSettings_ReturnsFalse()
        {
            var item = CreateSubject();

            item.Team = new Team();

            Assert.That(item.IsBlocked, Is.False);
        }

        [Test]
        [TestCase("In Progress", new[] { "On Hold", "Waiting for Customer" }, false)]
        [TestCase("Waiting", new[] { "On Hold", "Waiting for Customer" }, false)]
        [TestCase("On Hold", new[] { "On Hold", "Waiting for Customer" }, true)]
        [TestCase("Waiting for Customer", new[] { "On Hold", "Waiting for Customer" }, true)]
        public void IsBlocked_TeamHasBlockedStates_ReturnsTrueIfItemIsInBlockedState(string itemState, string[] blockedStates, bool expectedResult)
        {
            var item = CreateSubject();
            item.Team = new Team
            {
                BlockedStates = [.. blockedStates]
            };

            item.State = itemState;

            Assert.That(item.IsBlocked, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase(new[] { "In Progress" }, new[] { "On Hold", "Waiting for Customer" }, false)]
        [TestCase(new[] { "In Progress", "On Hold" }, new[] { "On Hold", "Waiting for Customer" }, true)]
        [TestCase(new[] { "On Hold" }, new[] { "On Hold", "Waiting for Customer" }, true)]
        [TestCase(new[] { "" }, new[] { "On Hold", "Waiting for Customer" }, false)]
        public void IsBlocked_TeamHasBlockedTags_ReturnsTrueIfItemHasBlockedTag(string[] itemTags, string[] blockedTags, bool expectedResult)
        {
            var item = CreateSubject();
            item.Team = new Team
            {
                BlockedTags = [.. blockedTags]
            };

            item.Tags = [.. itemTags];

            Assert.That(item.IsBlocked, Is.EqualTo(expectedResult));
        }

        [Test]
        [TestCase("Active", new[] { "My Tag" }, new[] { "Waiting for Customer" }, new[] { "Blocked" }, false)]
        [TestCase("Active", new[] { "Blocked" }, new[] { "Waiting for Customer" }, new[] { "Blocked" }, true)]
        [TestCase("Waiting for Customer", new[] { "My Tag" }, new[] { "Waiting for Customer" }, new[] { "Blocked" }, true)]
        [TestCase("Waiting for Customer", new[] { "Blocked" }, new[] { "Waiting for Customer" }, new[] { "Blocked" }, true)]
        [TestCase("Waiting for Customer", new[] { "Blocked" }, new[] { "" }, new[] { "" }, false)]
        public void IsBlocked_TeamHasBlockedStatesAndTags_ReturnsTrueIfAnyIsMatching(string itemState, string[] itemTags, string[] blockedStates, string[] blockedTags, bool expectedResult)
        {

            var item = CreateSubject();
            item.Team = new Team
            {
                BlockedStates = [..blockedStates],
                BlockedTags = [.. blockedTags]
            };

            item.State = itemState;
            item.Tags = [.. itemTags];

            Assert.That(item.IsBlocked, Is.EqualTo(expectedResult));
        }

        private static WorkItem CreateSubject()
        {
            return new WorkItem();
        }
    }
}
