using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

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
            item.Portfolios.Add(
                new Portfolio
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
            item.Portfolios.Add(
                new Portfolio
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
            item.Portfolios.Add(
                new Portfolio
                {
                    BlockedStates = [.. blockedStates],
                    BlockedTags = [.. blockedTags]
                });

            item.State = itemState;
            item.Tags = [.. itemTags];

            Assert.That(item.IsBlocked, Is.EqualTo(expectedResult));
        }

        [Test]
        public void GetFeatureSize_WhenFeautureHasNoWork_ReturnsZero()
        {
            var subject = CreateSubject();

            var size = subject.Size;
            
            Assert.That(size, Is.Zero);
        }

        [Test]
        public void GetFeatureSize_WhenFeautureUsesDefaultSize_ReturnsZero()
        {
            var team = new Team { Name = "Team A", Id = 12, };

            var subject = CreateSubject();
            subject.AddOrUpdateWorkForTeam(team, 5, 10);

            subject.IsUsingDefaultFeatureSize = true;

            var size = subject.Size;
            
            Assert.That(size, Is.Zero);
        }

        [Test]
        public void GetFeatureSize_FeatureHasWorkOfOneTeam_ReturnsWorkOfThatTeam()
        {
            var team = new Team { Name = "Team A", Id = 12, };
            var subject = CreateSubject();
            subject.AddOrUpdateWorkForTeam(team, 5, 10);

            var size = subject.Size;
            
            Assert.That(size, Is.EqualTo(10));
        }

        [Test]
        public void GetFeatureSize_FeatureHasWorkOfMultipleTeams_ReturnsSumOfWork()
        {
            var teamA = new Team { Name = "Team A", Id = 12, };
            var teamB = new Team { Name = "Team B", Id = 13, };
            
            var subject = CreateSubject();
            
            subject.AddOrUpdateWorkForTeam(teamA, 5, 10);
            subject.AddOrUpdateWorkForTeam(teamB, 3, 6);

            var size = subject.Size;
            
            Assert.That(size, Is.EqualTo(16));
        }

        [Test]
        public void SetFeatureForecasts_SetsFeatureIdOnEachForecast()
        {
            var subject = CreateSubject();
            subject.Id = 42;

            var forecast1 = new WhenForecast { NumberOfItems = 5 };
            var forecast2 = new WhenForecast { NumberOfItems = 10 };

            subject.SetFeatureForecasts([forecast1, forecast2]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecasts, Has.Count.EqualTo(2));
                Assert.That(subject.Forecasts[0].FeatureId, Is.EqualTo(42));
                Assert.That(subject.Forecasts[1].FeatureId, Is.EqualTo(42));
                Assert.That(subject.Forecasts[0].Feature, Is.SameAs(subject));
                Assert.That(subject.Forecasts[1].Feature, Is.SameAs(subject));
            }
        }

        [Test]
        public void AddOrUpdateWorkForTeam_DuplicateTeamIdExists_UpdatesInsteadOfThrowing()
        {
            var team = new Team { Name = "Team A", Id = 12 };

            var subject = CreateSubject();

            // Simulate corrupt state: two FeatureWork rows for same team
            subject.FeatureWork.Add(new FeatureWork(team, 3, 5, subject));
            subject.FeatureWork.Add(new FeatureWork(team, 3, 5, subject));

            // Should not throw, should handle gracefully
            Assert.DoesNotThrow(() => subject.AddOrUpdateWorkForTeam(team, 2, 8));

            var totalForTeam = subject.FeatureWork.Where(fw => fw.TeamId == team.Id).Sum(fw => fw.TotalWorkItems);
            Assert.That(totalForTeam, Is.EqualTo(8));
        }

        [Test]
        public void RemoveTeamFromFeature_DuplicateTeamIdExists_RemovesAllDuplicates()
        {
            var team = new Team { Name = "Team A", Id = 12 };

            var subject = CreateSubject();

            // Simulate corrupt state: two FeatureWork rows for same team
            subject.FeatureWork.Add(new FeatureWork(team, 3, 5, subject));
            subject.FeatureWork.Add(new FeatureWork(team, 3, 5, subject));

            subject.RemoveTeamFromFeature(team);

            Assert.That(subject.FeatureWork.Where(fw => fw.TeamId == team.Id), Is.Empty);
        }

        [Test]
        public void GetRemainingWorkForTeam_DuplicateTeamIdExists_DoesNotThrow()
        {
            var team = new Team { Name = "Team A", Id = 12 };

            var subject = CreateSubject();

            // Simulate corrupt state: two FeatureWork rows for same team
            subject.FeatureWork.Add(new FeatureWork(team, 3, 5, subject));
            subject.FeatureWork.Add(new FeatureWork(team, 4, 6, subject));

            var result = 0;
            Assert.DoesNotThrow(() => result = subject.GetRemainingWorkForTeam(team));
            Assert.That(result, Is.GreaterThanOrEqualTo(0));
        }

        private static Feature CreateSubject()
        {
            return new Feature();
        }
    }
}
