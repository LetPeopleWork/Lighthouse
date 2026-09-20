using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    /// <summary>
    /// What a client is handed about when work on a Feature begins. The source is always present and is
    /// never left to be worked out from what is missing.
    /// </summary>
    [TestFixture]
    [Category("epic-6033-forecasted-start-dates")]
    [Category("slice-01")]
    public class FeatureStartDtoTest
    {
        private static readonly DateOnly Today = new(2026, 9, 21);
        private static readonly int[] TheUsualPercentiles = [50, 70, 85, 95];
        private static readonly List<BlackoutPeriod> NoBlackoutDays = [];

        [Test]
        public void AForecastStart_CarriesEveryPercentileAndNoObservedDate()
        {
            var start = FeatureStart.ExpectedFrom(AStartOn(3));

            var dto = new FeatureStartDto(start, Today, NoBlackoutDays, TheUsualPercentiles);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Source, Is.EqualTo("Forecast"));
                Assert.That(dto.ObservedDate, Is.Null);
                Assert.That(dto.Percentiles.Select(percentile => percentile.Probability), Is.EqualTo(TheUsualPercentiles));
            }
        }

        [Test]
        public void AnObservedStart_CarriesTheDateAndNoPercentiles()
        {
            var observedOn = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

            var dto = new FeatureStartDto(FeatureStart.On(observedOn), Today, NoBlackoutDays, TheUsualPercentiles);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Source, Is.EqualTo("Observed"));
                Assert.That(dto.ObservedDate, Is.EqualTo(observedOn));
                Assert.That(dto.Percentiles, Is.Empty);
            }
        }

        [Test]
        public void AStartNobodyCanSayAnythingAbout_SaysExactlyThat()
        {
            var dto = new FeatureStartDto(FeatureStart.NotKnown, Today, NoBlackoutDays, TheUsualPercentiles);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.Source, Is.EqualTo("Unknown"));
                Assert.That(dto.ObservedDate, Is.Null);
                Assert.That(dto.Percentiles, Is.Empty);
            }
        }

        /// <summary>
        /// A day off the histogram becomes a date the same way a completion day does: counted forward
        /// over working days from today. Day one is therefore tomorrow rather than today - today is the
        /// day nothing has been delivered on yet. Asserted from a Monday so a weekend cannot be what
        /// moves the date and hide a change of anchor.
        /// </summary>
        [Test]
        public void AStartDay_BecomesADateByTheSamePathACompletionDayDoes()
        {
            var dto = new FeatureStartDto(FeatureStart.ExpectedFrom(AStartOn(1)), Today, NoBlackoutDays, [85]);

            Assert.That(dto.Percentiles.Single().ExpectedDate, Is.EqualTo(new DateTime(2026, 9, 22, 0, 0, 0, DateTimeKind.Utc)));
        }

        [Test]
        public void ATeamsShareOfAFeature_CarriesBothEnds()
        {
            var team = new Team { Id = 7, Name = "Team" };

            var dto = new FeatureTeamForecastDto(team.Id, AStartOn(2), ACompletionOn(9), Today, NoBlackoutDays, TheUsualPercentiles);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.TeamId, Is.EqualTo(team.Id));
                Assert.That(dto.StartPercentiles, Has.Count.EqualTo(4));
                Assert.That(dto.CompletionPercentiles, Has.Count.EqualTo(4));
            }
        }

        /// <summary>
        /// A team with one end and not the other still gets a row. Dropping it would make a team that
        /// cannot be forecast indistinguishable from a team that is not on the Feature at all.
        /// </summary>
        [Test]
        public void ATeamWithNoForecastAtEitherEnd_StillGetsARow()
        {
            var dto = new FeatureTeamForecastDto(7, null, null, Today, NoBlackoutDays, TheUsualPercentiles);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.TeamId, Is.EqualTo(7));
                Assert.That(dto.StartPercentiles, Is.Empty);
                Assert.That(dto.CompletionPercentiles, Is.Empty);
            }
        }

        /// <summary>
        /// The percentile DTO was widened to take any distribution so a start could use it. A completion
        /// still has to carry the throughput-filter notice it always did - that notice is what tells a
        /// reader the date was produced from a filtered history.
        /// </summary>
        [Test]
        public void ACompletionPercentile_StillCarriesItsThroughputFilterNotice()
        {
            var completion = ACompletionOn(9);
            completion.FilterApplied = true;
            completion.ExcludedSummary = "two outliers left out";

            var dto = new WhenForecastDto(completion, 85, Today, NoBlackoutDays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.FilterApplied, Is.True);
                Assert.That(dto.ExcludedSummary, Is.EqualTo("two outliers left out"));
            }
        }

        [Test]
        public void AStartPercentile_CarriesNoSuchNotice()
        {
            var dto = new WhenForecastDto(AStartOn(2), 85, Today, NoBlackoutDays);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(dto.FilterApplied, Is.False);
                Assert.That(dto.ExcludedSummary, Is.Null);
            }
        }

        private static StartForecast AStartOn(int day) => new(new Dictionary<int, int> { [day] = 100 });

        private static WhenForecast ACompletionOn(int day)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[day] = 100;

            return new WhenForecast(simulation);
        }
    }
}
