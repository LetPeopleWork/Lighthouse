using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models
{
    /// <summary>
    /// When a Feature says work on it begins, and where it says that answer came from.
    ///
    /// The provenance is the point. A screen or a write-back resolver that worked it out from the
    /// absence of percentiles would be deciding the same question separately, and "already started" and
    /// "cannot be forecast" both look like no percentiles from the outside.
    /// </summary>
    [TestFixture]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public class FeatureWhenWorkBeginsTest
    {
        private static readonly DateTime TheDayWorkBegan = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void AFeatureInFlight_ReportsTheDayItActuallyStarted()
        {
            var feature = AForecastableFeature();
            feature.StateCategory = StateCategories.Doing;
            feature.StartedDate = TheDayWorkBegan;

            var start = feature.WhenWorkBegins;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start.Source, Is.EqualTo(StartDateSource.Observed));
                Assert.That(start.ObservedDate, Is.EqualTo(TheDayWorkBegan));
                Assert.That(start.Forecast, Is.Null, "a forecast beside a fact is a second opinion about a settled question");
            }
        }

        /// <summary>
        /// The state on its own is not enough. A Feature a connector reports as in progress without ever
        /// saying when would otherwise report an observed start of nothing at all, which reads worse than
        /// the forecast it replaced.
        /// </summary>
        [Test]
        public void AFeatureInFlightWithNoStartedDate_FallsBackToTheForecast()
        {
            var feature = AForecastableFeature();
            feature.StateCategory = StateCategories.Doing;
            feature.StartedDate = null;

            Assert.That(feature.WhenWorkBegins.Source, Is.EqualTo(StartDateSource.Forecast));
        }

        /// <summary>
        /// A started date on a Feature nobody has started yet is history, not a start: work item sync
        /// carries one on Features that were begun and put back down.
        /// </summary>
        [Test]
        public void AFeatureNotInFlight_IsForecastEvenIfItCarriesAStartedDate()
        {
            var feature = AForecastableFeature();
            feature.StateCategory = StateCategories.ToDo;
            feature.StartedDate = TheDayWorkBegan;

            Assert.That(feature.WhenWorkBegins.Source, Is.EqualTo(StartDateSource.Forecast));
        }

        [Test]
        public void AFeatureNobodyHasStarted_ReportsWhatTheSimulationExpects()
        {
            var feature = AForecastableFeature();

            var start = feature.WhenWorkBegins;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(start.Source, Is.EqualTo(StartDateSource.Forecast));
                Assert.That(start.ObservedDate, Is.Null);
                Assert.That(start.Forecast!.GetProbability(85), Is.EqualTo(4));
            }
        }

        [Test]
        public void AFeatureThatCannotBeForecast_SaysSoRatherThanSayingNothing()
        {
            var team = new Team { Id = 1, Name = "Team" };
            var feature = new Feature(team, 5);

            var start = feature.WhenWorkBegins;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.CanBeForecast, Is.False);
                Assert.That(start.Source, Is.EqualTo(StartDateSource.Unknown));
                Assert.That(start.Forecast, Is.Null);
            }
        }

        /// <summary>
        /// The run still records a start for a Feature it cannot honestly forecast, because the teams it
        /// *can* forecast take part as usual - so the distribution is there, and it is a real one. It
        /// must still not be reported.
        ///
        /// A Feature reports no dates at all when any contributing team cannot be forecast, rather than
        /// dates built from the teams that could, because a partial answer in the shape of a whole one is
        /// read as a whole one. The start side inherits that rule; this is the case where inheriting it
        /// costs something, and therefore the case where forgetting to would not show.
        /// </summary>
        [Test]
        public void AFeatureThatCannotBeForecast_SaysSoEvenWhenTheRunDidRecordAStartForIt()
        {
            var measured = new Team { Id = 1, Name = "Measured" };
            var silent = new Team { Id = 2, Name = "Never delivered" };

            var feature = new Feature([(measured, 5, 5), (silent, 3, 3)]);
            feature.SetFeatureForecasts([AsJustForecast(measured, 10)]);
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [4] = 10 }, null)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.CanBeForecast, Is.False, "one of its teams has no measured delivery");
                Assert.That(feature.StartForecasts.Single().TotalTrials, Is.EqualTo(10), "yet a start was recorded");
                Assert.That(feature.WhenWorkBegins.Source, Is.EqualTo(StartDateSource.Unknown));
            }
        }

        /// <summary>
        /// A distribution built from no runs at all is not an answer. It would report day zero at every
        /// percentile, which reads as "today" on every screen that draws it.
        /// </summary>
        [Test]
        public void AStartDistributionWithNoRunsBehindIt_IsNotAnAnswer()
        {
            var feature = AForecastableFeature();
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int>(), null)]);

            Assert.That(feature.WhenWorkBegins.Source, Is.EqualTo(StartDateSource.Unknown));
        }

        [Test]
        public void AFeatureWithOnlyPerTeamRows_HasNoAnswerOfItsOwn()
        {
            var feature = AForecastableFeature();
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [4] = 10 }) { TeamId = 1 }]);

            Assert.That(feature.WhenWorkBegins.Source, Is.EqualTo(StartDateSource.Unknown));
        }

        [Test]
        public void SetStartForecasts_ReplacesWhatWasThereRatherThanAddingToIt()
        {
            var feature = AForecastableFeature();

            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [9] = 10 }, null)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartForecasts, Has.Count.EqualTo(1));
                Assert.That(feature.WhenWorkBegins.Forecast!.GetProbability(85), Is.EqualTo(9));
            }
        }

        [Test]
        public void SetStartForecasts_PointsEveryRowBackAtItsFeature()
        {
            var feature = AForecastableFeature();
            feature.Id = 42;

            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [1] = 1 }, null)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartForecasts[0].Feature, Is.SameAs(feature));
                Assert.That(feature.StartForecasts[0].FeatureId, Is.EqualTo(42));
            }
        }

        /// <summary>
        /// A team with nothing measured takes no part in the run, so it has a completion row with no runs
        /// behind it - and every percentile off an empty histogram is day zero, which every date
        /// projection turns into today. Reported as-is, that team would say it finishes today at every
        /// confidence level: a confident, fabricated answer in exactly the shape a real one arrives in,
        /// for the one case the rest of the product is careful to say nothing about.
        /// </summary>
        [Test]
        public void ATeamWithNothingMeasured_HasNoForecastAtEitherEndRatherThanOneDatedToday()
        {
            var team = new Team { Id = 7, Name = "Never delivered" };
            var feature = new Feature(team, 5);

            feature.SetFeatureForecasts([new WhenForecast(new SimulationResult(team, feature, 5))]);
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int>(), team)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.Forecasts.Single().TotalTrials, Is.Zero, "the row is there, it just has nothing behind it");
                Assert.That(feature.CompletionForecastFor(team), Is.Null);
                Assert.That(feature.StartForecastFor(team), Is.Null);
            }
        }

        [Test]
        public void TheForecastsForOneTeam_AreFoundByThatTeam()
        {
            var team = new Team { Id = 7, Name = "Team" };
            var other = new Team { Id = 8, Name = "Other" };

            var feature = new Feature(team, 5);
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [3] = 10 }, team)]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartForecastFor(team)!.GetProbability(85), Is.EqualTo(3));
                Assert.That(feature.StartForecastFor(other), Is.Null);
            }
        }

        /// <summary>
        /// A per-team completion row names its team through whichever of the two the store left set: the
        /// navigation is there after a forecast has just run, and only the id survives a round trip.
        /// </summary>
        [Test]
        public void TheCompletionForecastForOneTeam_IsFoundByEitherTheTeamOrItsId()
        {
            var team = new Team { Id = 7, Name = "Team" };
            var feature = new Feature(team, 5);

            feature.SetFeatureForecasts([AsJustForecast(team, 4)]);
            Assert.That(feature.CompletionForecastFor(team), Is.Not.Null, "found through the team itself");

            feature.SetFeatureForecasts([AsReadBackFromTheStore(team.Id, 6)]);
            Assert.That(feature.CompletionForecastFor(team), Is.Not.Null, "and through its id alone");
        }

        /// <summary>A row as it comes back from the store, where only the team's id survives.</summary>
        private static WhenForecast AsReadBackFromTheStore(int teamId, int day)
            => new(ASimulationFinishingOn(day)) { TeamId = teamId, Team = null };

        /// <summary>A row as it is the moment a forecast has just run, carrying the team itself.</summary>
        private static WhenForecast AsJustForecast(Team team, int day)
            => new(ASimulationFinishingOn(day)) { TeamId = team.Id, Team = team };

        private static SimulationResult ASimulationFinishingOn(int day)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[day] = 10;

            return simulation;
        }

        /// <summary>
        /// A Feature with a completion row for its one team, which is what makes it forecastable, and a
        /// start distribution that lands on day four in every run.
        /// </summary>
        private static Feature AForecastableFeature()
        {
            var team = new Team { Id = 1, Name = "Team" };
            var feature = new Feature(team, 5);

            feature.SetFeatureForecasts([AsJustForecast(team, 10)]);
            feature.SetStartForecasts([new StartForecast(new Dictionary<int, int> { [4] = 10 }, null)]);

            return feature;
        }
    }
}
