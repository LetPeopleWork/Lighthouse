using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models
{
    // Story #5570 (ADR-112). A feature whose contributing team has no usable throughput has no honest
    // completion distribution. The hazard is not the missing dates - it is ForecastBase.GetLikelihood
    // returning 100 on an empty histogram, i.e. maximum confidence on the one feature nobody can forecast.
    public class FeatureUnknownForecastTest
    {
        private static readonly DateOnly Today = new(2026, 7, 28);
        private static readonly DateTime TargetDate = new(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);

        [Test]
        public void CanBeForecast_ContributingTeamHasNoThroughput_IsFalse()
        {
            var subject = FeatureWithUnforecastableTeam();

            Assert.That(subject.CanBeForecast, Is.False);
        }

        [Test]
        public void GetLikelhoodForDate_ContributingTeamHasNoThroughput_IsNotHundred()
        {
            // AC-02.2, the sharp edge: falling through to ForecastBase.GetLikelihood's `return 100`
            // reports total confidence in a feature that cannot be forecast at all.
            var subject = FeatureWithUnforecastableTeam();

            var likelihood = subject.GetLikelhoodForDate(TargetDate, Today, []);

            Assert.That(likelihood, Is.Not.EqualTo(100));
        }

        [Test]
        public void TeamsWithoutForecast_NamesTheTeamThatCouldNotBeForecast()
        {
            var subject = FeatureWithUnforecastableTeam();

            Assert.That(subject.TeamsWithoutForecast.Select(t => t.Name), Is.EqualTo(new[] { "No Throughput" }));
        }

        [Test]
        public void CanBeForecast_EveryContributingTeamHasThroughput_IsTrue()
        {
            var forecasting = new Team { Id = 1, Name = "Forecasting" };
            var subject = new Feature([(forecasting, 3, 3)]);
            subject.SetFeatureForecasts([ForecastFor(forecasting, new Dictionary<int, int> { { 5, 100 } })]);

            Assert.That(subject.CanBeForecast, Is.True);
        }

        [Test]
        public void CanBeForecast_NoRemainingWork_IsTrueEvenThoughNothingWasSimulated()
        {
            // AC-02.3: a finished feature is a fact, not a forecast. It carries ForecastService's day-0
            // sentinel, which also has no trials, and must not be mistaken for "cannot be forecast".
            var done = new Team { Id = 1, Name = "Done" };
            var subject = new Feature([(done, 0, 3)]);
            subject.SetFeatureForecasts([ForecastFor(null, new Dictionary<int, int> { { 0, 0 } })]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.CanBeForecast, Is.True);
                Assert.That(subject.GetLikelhoodForDate(TargetDate, Today, []), Is.EqualTo(100));
            }
        }

        [Test]
        public void HasSufficientData_ContributingTeamHasNoThroughput_StaysFalse()
        {
            // AC-02.4: the two signals compose. Unknown says no distribution exists; insufficient data
            // says the distribution rests on thin history.
            var subject = FeatureWithUnforecastableTeam();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecast.HasSufficientData, Is.False);
                Assert.That(subject.CanBeForecast, Is.False);
            }
        }

        private static Feature FeatureWithUnforecastableTeam()
        {
            var forecasting = new Team { Id = 1, Name = "Forecasting" };
            var withoutThroughput = new Team { Id = 2, Name = "No Throughput" };

            var feature = new Feature([(forecasting, 3, 3), (withoutThroughput, 2, 2)]);
            feature.SetFeatureForecasts([
                ForecastFor(forecasting, new Dictionary<int, int> { { 5, 100 } }),
                ForecastFor(withoutThroughput, [], hasSufficientData: false),
            ]);

            return feature;
        }

        private static WhenForecast ForecastFor(Team? team, Dictionary<int, int> histogram, bool hasSufficientData = true)
        {
            return new WhenForecast(histogram)
            {
                Team = team,
                TeamId = team?.Id,
                HasSufficientData = hasSufficientData,
            };
        }
    }
}
