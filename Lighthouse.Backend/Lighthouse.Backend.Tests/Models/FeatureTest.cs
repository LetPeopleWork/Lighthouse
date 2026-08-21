using Lighthouse.Backend.Models.Dependencies;
﻿using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.Models
{
    public class FeatureTest
    {
        private static readonly BlackoutPeriod[] NoBlackoutPeriods = [];

        /// <summary>
        /// Bug #5567: a fixed instant on a UTC instance. The expectations below are unchanged from
        /// before the anchor moved - the point is that they no longer RE-DERIVE the production
        /// expression, which is root cause D.
        /// </summary>
        private static readonly FakeLighthouseClock Clock =
            new(new DateTimeOffset(2026, 7, 27, 10, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc);

        /// <summary>
        /// A connector builds a Feature out of what a tracker just handed over, which never includes who
        /// is working on it - the work is matched to Teams later, by the service that has both in hand.
        /// So a freshly mapped Feature carries its links and no work at all, and that is the ordinary
        /// state rather than a half-built object. Pinned because it reads like a defect on sight, and the
        /// constructor that takes only the links is the one that invites the second look.
        /// </summary>
        [Test]
        public void AFeatureBuiltFromNothingButItsLinks_CarriesThemAndNoWorkYet()
        {
            var links = new[] { new FeatureDependencyReference(0, "PROJ-1", DependencySource.TrackerLink) };

            var feature = new Feature(links);

            var expected = new[] { "PROJ-1" };

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.DependsOnReferences.Select(reference => reference.ReferenceId), Is.EqualTo(expected));
                Assert.That(feature.FeatureWork, Is.Empty);
            }
        }

        /// <summary>
        /// The same emptiness the constructor above leaves, from the constructor that takes nothing. The
        /// two have to agree, or which one a connector happens to call would decide what a Feature knows.
        /// </summary>
        [Test]
        public void AFeatureBuiltFromNothingAtAll_CarriesNoWorkAndNoLinks()
        {
            var feature = new Feature();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.FeatureWork, Is.Empty);
                Assert.That(feature.DependsOnReferences, Is.Empty);
            }
        }

        [Test]
        public void GetLikelihoodForFeature_FeatureHasNoRemainingWork_Returns100()
        {
            var subject = CreateSubject();

            var likelihood = subject.GetLikelhoodForDate(Clock.TodayAsUtcMidnight.AddDays(17), Clock.Today, NoBlackoutPeriods);

            Assert.That(likelihood, Is.EqualTo(100));
        }

        /// <summary>
        /// The short circuit stands on its own: a feature with nothing left to do is certain to make
        /// ANY date, whatever its stale forecast says.
        /// </summary>
        [Test]
        public void GetLikelhoodForDate_NoRemainingWork_Returns100EvenWhenTheForecastIsPessimistic()
        {
            var subject = CreateSubject();
            subject.FeatureWork.Add(new FeatureWork { RemainingWorkItems = 0, TotalWorkItems = 5 });
            subject.Forecasts.Add(MostlySlowForecast());

            var likelihood = subject.GetLikelhoodForDate(Clock.TodayAsUtcMidnight.AddDays(1), Clock.Today, NoBlackoutPeriods);

            Assert.That(likelihood, Is.EqualTo(100));
        }

        /// <summary>
        /// No target date means no date to be late for. Both halves of the guard have to hold: work
        /// remaining is not on its own a reason to run the forecast, or a feature that nobody has
        /// committed to a date would report a risk against 0001-01-01.
        /// </summary>
        [Test]
        public void GetLikelhoodForDate_NoTargetDate_Returns100EvenWithWorkRemaining()
        {
            var subject = CreateSubject();
            subject.FeatureWork.Add(new FeatureWork { RemainingWorkItems = 5, TotalWorkItems = 5 });
            subject.Forecasts.Add(MostlySlowForecast());

            var likelihood = subject.GetLikelhoodForDate(default, Clock.Today, NoBlackoutPeriods);

            Assert.That(likelihood, Is.EqualTo(100));
        }

        /// <summary>
        /// 5 trials in 100 land within two days, the rest take sixty - so a target one day out
        /// scores 5%, well clear of the 100 the short circuits return.
        /// </summary>
        private static WhenForecast MostlySlowForecast()
        {
            var simulationResult = new SimulationResult();
            simulationResult.SimulationResults[2] = 5;
            simulationResult.SimulationResults[60] = 95;

            return new WhenForecast(simulationResult);
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

        // Blocked evaluation moved off the model into IBlockedItemService (ADR-067, single rule-based read
        // path). The former Feature.IsBlocked portfolio state/tag cases now live in BlockedItemServiceTest.

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
