using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models
{
    // Story #5570 (ADR-112). A feature whose contributing team has no usable throughput has no honest
    // completion distribution. The hazard is not the missing dates - it is ForecastBase.GetLikelihood
    // returning 100 on an empty histogram, i.e. maximum confidence on the one feature nobody can forecast.
    public class FeatureUnknownForecastTest
    {
        // CA1861: inline arrays in NUnit assertions are new-code Sonar violations.
        private static readonly string[] TeamWithoutThroughput = ["No Throughput"];
        private static readonly string[] TeamTheForecastRanFor = ["Ran Without Throughput"];

        private static readonly string[] ContributingTeam = ["Contributing"];

        private static readonly string[] BothTeamsForThePrecedenceFixture = ["Ran Without Throughput", "Contributing"];

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

            Assert.That(subject.TeamsWithoutForecast.Select(t => t.Name), Is.EqualTo(TeamWithoutThroughput));
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

        [Test]
        public void CanBeForecast_NoRemainingWorkButATeamForecastCarriesNoTrials_IsStillTrue()
        {
            // The exemption is about remaining work, not about who owns the empty forecast: a finished
            // feature can still carry a stale per-team forecast with no trials.
            var done = new Team { Id = 1, Name = "Done" };
            var subject = new Feature([(done, 0, 3)]);
            subject.SetFeatureForecasts([ForecastFor(done, [])]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.CanBeForecast, Is.True);
                Assert.That(subject.TeamsWithoutForecast, Is.Empty);
            }
        }

        [Test]
        public void TeamsWithoutForecast_ForecastCarriesOnlyATeamId_ResolvesTheTeamFromTheFeature()
        {
            // Forecasts loaded from EF need not have the Team navigation populated.
            var withoutThroughput = new Team { Id = 7, Name = "No Throughput" };
            var subject = new Feature([(withoutThroughput, 2, 2)]);

            var forecast = new WhenForecast([]) { TeamId = withoutThroughput.Id };
            subject.SetFeatureForecasts([forecast]);

            Assert.That(subject.TeamsWithoutForecast.Select(t => t.Name), Is.EqualTo(TeamWithoutThroughput));
        }

        [Test]
        public void TeamsWithoutForecast_ForecastMatchesNoTeamOnTheFeature_LeavesTheContributingTeamUnforecast()
        {
            // Inverted for ADR-113 DDD-8. The row names nobody on this feature, so it is still ignored
            // as a zero-trial signal - but the contributing team then has no row at all, which is worse
            // than a zero-trial one. Reporting the feature as forecastable here was the silent
            // certainty the delivery-grain rollup exists to remove.
            var contributing = new Team { Id = 1, Name = "Contributing" };
            var subject = new Feature([(contributing, 2, 2)]);

            subject.SetFeatureForecasts([new WhenForecast([]) { TeamId = 999 }]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.TeamsWithoutForecast.Select(team => team.Name), Is.EqualTo(ContributingTeam));
                Assert.That(subject.CanBeForecast, Is.False);
            }
        }

        [Test]
        public void TeamsWithoutForecast_ForecastNamesADifferentTeamThanItsTeamId_PrefersTheForecastsOwnTeam()
        {
            // Precedence matters: the forecast knows which team it was run for, the feature only knows
            // who contributes. Resolving by id is the fallback, not the first choice.
            //
            // Both teams are named after ADR-113 DDD-8, and for two different reasons: the row ran for
            // team 42 and produced no trials, and team 1 - which actually contributes - therefore owns
            // no row at all. Production never builds this shape; ForecastService writes Team and TeamId
            // together from the same SimulationResult.
            var runFor = new Team { Id = 42, Name = "Ran Without Throughput" };
            var contributing = new Team { Id = 1, Name = "Contributing" };

            var subject = new Feature([(contributing, 2, 2)]);
            subject.SetFeatureForecasts([new WhenForecast([]) { Team = runFor, TeamId = contributing.Id }]);

            Assert.That(subject.TeamsWithoutForecast.Select(team => team.Name), Is.EqualTo(BothTeamsForThePrecedenceFixture));
        }

        [Test]
        public void SetFeatureForecasts_CalledAgain_ReplacesTheForecastsRatherThanAddingToThem()
        {
            // A stale forecast left behind would keep a team in TeamsWithoutForecast after its
            // throughput arrived, so the feature would stay un-forecastable forever.
            var team = new Team { Id = 1, Name = "Team" };
            var subject = new Feature([(team, 3, 3)]);

            subject.SetFeatureForecasts([ForecastFor(team, [])]);
            subject.SetFeatureForecasts([ForecastFor(team, new Dictionary<int, int> { { 5, 100 } })]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecasts, Has.Count.EqualTo(1));
                Assert.That(subject.CanBeForecast, Is.True);
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
