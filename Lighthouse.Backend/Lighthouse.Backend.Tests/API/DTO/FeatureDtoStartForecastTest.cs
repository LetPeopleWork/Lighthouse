using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Tests.TestDoubles;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>
    /// What the Feature read carries about when work begins, assembled the way a controller assembles
    /// it. This is the contract slice 01 exists to serve, and the acceptance scenarios drive it through
    /// a real host - which is the right place to prove it is wired, and too expensive a place to prove
    /// every branch of it.
    /// </summary>
    [TestFixture]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public class FeatureDtoStartForecastTest
    {
        private static readonly DateTimeOffset AMondayMorning = new(2026, 9, 21, 9, 0, 0, TimeSpan.Zero);
        private static readonly List<BlackoutPeriod> NoBlackoutDays = [];
        private static readonly int[] TheUsualPercentiles = [50, 70, 85, 95];

        [Test]
        public void AFeatureNobodyHasStarted_CarriesFourDatedStartPercentiles()
        {
            var team = ATeam(1);
            var feature = AForecastableFeature(team, startsOnDay: 4, finishesOnDay: 11);

            var dto = TheReadOf(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.StartForecast.Source, Is.EqualTo("Forecast"));
                Assert.That(dto.StartForecast.Percentiles.Select(p => p.Probability), Is.EqualTo(TheUsualPercentiles));
                Assert.That(dto.StartForecast.ObservedDate, Is.Null);
            }
        }

        [Test]
        public void AFeatureInFlight_CarriesTheDayItStartedAndNoPercentiles()
        {
            var startedOn = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

            var team = ATeam(1);
            var feature = AForecastableFeature(team, startsOnDay: 4, finishesOnDay: 11);
            feature.StateCategory = StateCategories.Doing;
            feature.StartedDate = startedOn;

            var dto = TheReadOf(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.StartForecast.Source, Is.EqualTo("Observed"));
                Assert.That(dto.StartForecast.ObservedDate, Is.EqualTo(startedOn));
                Assert.That(dto.StartForecast.Percentiles, Is.Empty);
            }
        }

        /// <summary>
        /// A Feature can be finished and still hold a start forecast, so being finished is not on its own
        /// enough to keep a predicted date off the screen. The simulation picks which Features to forecast
        /// by which teams contribute to them and never looks at their state, and it keeps a start row for
        /// any Feature that still has work left - which a finished Feature does whenever one of its
        /// children is still open. The day it actually started has to win over that surviving row.
        /// </summary>
        [Test]
        public void AFinishedFeatureWithWorkStillOpen_CarriesTheDayItStartedRatherThanTheSurvivingForecast()
        {
            var startedOn = new DateTime(2026, 9, 7, 0, 0, 0, DateTimeKind.Utc);

            var team = ATeam(1);
            var feature = AForecastableFeature(team, startsOnDay: 4, finishesOnDay: 11);
            feature.StateCategory = StateCategories.Done;
            feature.StartedDate = startedOn;

            var dto = TheReadOf(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(feature.StartForecasts, Is.Not.Empty, "the forecast the read has to pass over");
                Assert.That(dto.StartForecast.Source, Is.EqualTo("Observed"));
                Assert.That(dto.StartForecast.ObservedDate, Is.EqualTo(startedOn));
                Assert.That(dto.StartForecast.Percentiles, Is.Empty);
            }
        }

        /// <summary>
        /// The start date is absent and says why. The completion list is absent too, which it has always
        /// been for this case - the point here is that the start side did not invent a different answer.
        /// </summary>
        [Test]
        public void AFeatureAContributingTeamCannotBeForecastFor_SaysSoAtBothEnds()
        {
            var measured = ATeam(1);
            var silent = ATeam(2);

            var feature = new Feature([(measured, 5, 5), (silent, 3, 3)]) { Name = "Shared", ReferenceId = "SHARED" };
            feature.SetFeatureForecasts([ACompletionFor(measured, 11)]);

            var dto = TheReadOf(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.StartForecast.Source, Is.EqualTo("Unknown"));
                Assert.That(dto.StartForecast.Percentiles, Is.Empty);
                Assert.That(dto.Forecasts, Is.Empty);
                Assert.That(dto.TeamsWithoutForecast, Does.Contain(silent.Name));
            }
        }

        /// <summary>
        /// Both ends per contributing team. The completion half is the older gap: those forecasts have
        /// existed in the domain since long before this Epic and were dropped at this boundary, which is
        /// why "which team is the late one" was unanswerable from the API.
        /// </summary>
        [Test]
        public void AFeatureTwoTeamsShare_CarriesBothEndsForEachOfThem()
        {
            var first = ATeam(1);
            var second = ATeam(2);

            var feature = new Feature([(first, 5, 5), (second, 3, 3)]) { Name = "Shared", ReferenceId = "SHARED" };
            feature.SetFeatureForecasts([ACompletionFor(first, 11), ACompletionFor(second, 14)]);
            feature.SetStartForecasts(
            [
                new StartForecast(ADistributionOn(4), null),
                new StartForecast(ADistributionOn(4), first),
                new StartForecast(ADistributionOn(6), second),
            ]);

            var dto = TheReadOf(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.TeamForecasts, Has.Count.EqualTo(2));

                foreach (var team in new[] { first, second })
                {
                    var forTeam = dto.TeamForecasts.Single(entry => entry.TeamId == team.Id);

                    Assert.That(forTeam.StartPercentiles, Has.Count.EqualTo(4), $"start percentiles for team {team.Id}");
                    Assert.That(forTeam.CompletionPercentiles, Has.Count.EqualTo(4), $"completion percentiles for team {team.Id}");
                }
            }
        }

        [Test]
        public void AFeatureNobodyIsWorking_CarriesNoPerTeamRowsAtAll()
        {
            var feature = new Feature { Name = "Nobody's", ReferenceId = "NONE" };

            Assert.That(TheReadOf(feature).TeamForecasts, Is.Empty);
        }

        /// <summary>
        /// One row per contributing team, however the work was recorded. A team that appears twice in a
        /// Feature's work - which the sync treats as reachable and repairs - must not appear twice here,
        /// or a timeline would draw it two lanes.
        /// </summary>
        [Test]
        public void ATeamThatAppearsTwiceInTheWork_GetsOneRow()
        {
            var team = ATeam(1);

            var feature = new Feature([(team, 5, 5), (team, 3, 3)]) { Name = "Doubled", ReferenceId = "DOUBLED" };
            feature.SetFeatureForecasts([ACompletionFor(team, 11)]);

            Assert.That(TheReadOf(feature).TeamForecasts, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// The completion list keeps its shape and its content. A start row folded into the collection
        /// the aggregate reads would move these, which is the one thing this slice promised not to do.
        /// </summary>
        [Test]
        public void TheCompletionForecast_IsUnchangedByAnythingTheStartSideAdds()
        {
            var team = ATeam(1);
            var withoutStarts = AForecastableFeature(team, startsOnDay: 4, finishesOnDay: 11);
            withoutStarts.SetStartForecasts([]);

            var withStarts = AForecastableFeature(team, startsOnDay: 4, finishesOnDay: 11);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(TheReadOf(withStarts).Forecasts.Select(f => f.ExpectedDate),
                    Is.EqualTo(TheReadOf(withoutStarts).Forecasts.Select(f => f.ExpectedDate)));
                Assert.That(TheReadOf(withStarts).Forecasts.Select(f => f.Probability), Is.EqualTo(TheUsualPercentiles));
            }
        }

        private static FeatureDto TheReadOf(Feature feature)
            => new(feature, new FakeLighthouseClock(AMondayMorning), NoBlackoutDays, isBlocked: false, blockedSince: null);

        private static Team ATeam(int id) => new() { Id = id, Name = $"Team {id}" };

        private static Dictionary<int, int> ADistributionOn(int day) => new() { [day] = 100 };

        private static WhenForecast ACompletionFor(Team team, int day)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[day] = 100;

            return new WhenForecast(simulation) { TeamId = team.Id, Team = team };
        }

        private static Feature AForecastableFeature(Team team, int startsOnDay, int finishesOnDay)
        {
            var feature = new Feature(team, 5) { Name = "Alpha", ReferenceId = "ALPHA" };

            feature.SetFeatureForecasts([ACompletionFor(team, finishesOnDay)]);
            feature.SetStartForecasts(
            [
                new StartForecast(ADistributionOn(startsOnDay), null),
                new StartForecast(ADistributionOn(startsOnDay), team),
            ]);

            return feature;
        }
    }
}
