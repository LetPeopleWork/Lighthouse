using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Tests.Models
{
    public class FeatureTest
    {
        [Test]
        public void GetLikelihoodForFeature_FeatureHasNoRemainingWork_Returns100()
        {
            var subject = CreateSubject();

            var likelihood = subject.GetLikelhoodForDate(DateTime.Today.AddDays(17));

            Assert.That(likelihood, Is.EqualTo(100));
        }

        [Test]
        public void Update_SetsEstimatedSize()
        {
            var otherItem = new Feature
            {
                EstimatedSize = 42
            };

            var subject = CreateSubject();

            subject.Update(otherItem);

            Assert.That(subject.EstimatedSize, Is.EqualTo(otherItem.EstimatedSize));
        }

        [Test]
        public void Update_SetsOwningTeam()
        {
            var otherItem = new Feature
            {
                OwningTeam = "Team B"
            };

            var subject = CreateSubject();

            subject.Update(otherItem);

            Assert.That(subject.OwningTeam, Is.EqualTo(otherItem.OwningTeam));
        }

        [Test]
        [TestCase("In Progress", new[] { "On Hold", "Waiting for Customer" }, false)]
        [TestCase("Waiting", new[] { "On Hold", "Waiting for Customer" }, false)]
        [TestCase("On Hold", new[] { "On Hold", "Waiting for Customer" }, true)]
        [TestCase("Waiting for Customer", new[] { "On Hold", "Waiting for Customer" }, true)]
        public void IsBlocked_TeamHasBlockedStates_ReturnsTrueIfItemIsInBlockedState(string itemState, string[] blockedStates, bool expectedResult)
        {
            var item = CreateSubject();
            item.Projects.Add(
                new Project
                {
                    BlockedStates = [.. blockedStates]
                });

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
            item.Projects.Add(
                new Project
                {
                    BlockedTags = [.. blockedTags]
                });

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
            item.Projects.Add(
                new Project
                {
                    BlockedStates = [.. blockedStates],
                    BlockedTags = [.. blockedTags]
                });

            item.State = itemState;
            item.Tags = [.. itemTags];

            Assert.That(item.IsBlocked, Is.EqualTo(expectedResult));
        }

        private Feature CreateSubject()
        {
            return new Feature();
        }
    }
}
