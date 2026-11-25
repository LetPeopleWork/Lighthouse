using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class FeatureDtoTest
    {
        private const int WorkingDaysToCompletion = 10;

        private static readonly string[] AlphaThenZulu = ["Alpha", "Zulu"];
        private static readonly int[] TheUsualPercentiles = [50, 70, 85, 95];
        private static readonly (int, string)[] OnlyTheReadable = [(2, "Readable")];
        private static readonly int[] BothPortfolioIds = [1, 2];

        private static DateTime Today => TestToday.AmbientAsUtcMidnight;

        [Test]
        public void CreateFeatureDto_GivenForecastCreationTime_ReturnsDateAsUTC()
        {
            var forecastCreationTime = TestToday.FrozenNow.DateTime;

            var forecast = new WhenForecast()
            {
                CreationTime = forecastCreationTime
            };

            var feature = new Feature();
            feature.SetFeatureForecasts(new List<WhenForecast> { forecast });

            var subject = CreateSubject(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(forecastCreationTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            };
        }

        [Test]
        public void CreateFeatureDto_FeatureWithFutureBlackoutDays_ForecastPercentileDateStepsOverTheSpan()
        {
            var feature = ForecastedFeature();
            var blackoutPeriods = new[]
            {
                new BlackoutPeriod
                {
                    Start = DateOnly.FromDateTime(Today.AddDays(3)),
                    End = DateOnly.FromDateTime(Today.AddDays(4)),
                },
            };

            var subject = new FeatureDto(feature, TestToday.Clock, blackoutPeriods, false, null);

            var expectedDate = subject.Forecasts.Single(f => f.Probability == 85).ExpectedDate;
            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(12)));
        }

        [Test]
        public void CreateFeatureDto_FeatureWithNoBlackoutPeriods_ForecastPercentileDateUnchanged()
        {
            var feature = ForecastedFeature();

            var subject = new FeatureDto(feature, TestToday.Clock, Array.Empty<BlackoutPeriod>(), false, null);

            var expectedDate = subject.Forecasts.Single(f => f.Probability == 85).ExpectedDate;
            Assert.That(expectedDate, Is.EqualTo(Today.AddDays(WorkingDaysToCompletion)));
        }

        private static Feature ForecastedFeature()
        {
            var feature = new Feature();
            feature.SetFeatureForecasts([DeterministicForecast(WorkingDaysToCompletion)]);
            return feature;
        }

        private static WhenForecast DeterministicForecast(int workingDays)
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[workingDays] = 100;
            return new WhenForecast(simulation, TestToday.FrozenNow.UtcDateTime) { HasSufficientData = true };
        }

        [Test]
        public void TheFeaturePayloadAndTheListRow_ReportEachTeamsRemainingAndTotalWork()
        {
            var feature = new Feature();
            feature.AddOrUpdateWorkForTeam(new Team { Id = 1, Name = "Alpha" }, 3, 5);
            feature.AddOrUpdateWorkForTeam(new Team { Id = 2, Name = "Zulu" }, 2, 2);

            var (full, row) = BothDtosFor(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(full.RemainingWork, Is.EqualTo(new Dictionary<int, int> { [1] = 3, [2] = 2 }));
                Assert.That(full.TotalWork, Is.EqualTo(new Dictionary<int, int> { [1] = 5, [2] = 2 }));
                Assert.That(row.RemainingWork, Is.EqualTo(new Dictionary<int, int> { [1] = 3, [2] = 2 }));
                Assert.That(row.TotalWork, Is.EqualTo(new Dictionary<int, int> { [1] = 5, [2] = 2 }));
            }
        }

        [Test]
        public void TheFeaturePayloadAndTheListRow_NameTheTeamsThatCannotBeForecastInAlphabeticalOrderAndShowNoForecast()
        {
            var feature = new Feature();
            feature.AddOrUpdateWorkForTeam(new Team { Id = 2, Name = "Zulu" }, 3, 3);
            feature.AddOrUpdateWorkForTeam(new Team { Id = 1, Name = "Alpha" }, 3, 3);

            var (full, row) = BothDtosFor(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(full.TeamsWithoutForecast, Is.EqualTo(AlphaThenZulu));
                Assert.That(row.TeamsWithoutForecast, Is.EqualTo(AlphaThenZulu));
                Assert.That(full.Forecasts, Is.Empty);
                Assert.That(row.Forecasts, Is.Empty);
            }
        }

        [Test]
        public void TheListRow_OfAForecastableFeature_ReportsTheUsualFourPercentiles()
        {
            var (full, row) = BothDtosFor(ForecastedFeature());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(full.Forecasts.Select(forecast => forecast.Probability), Is.EqualTo(TheUsualPercentiles));
                Assert.That(row.Forecasts.Select(forecast => forecast.Probability), Is.EqualTo(TheUsualPercentiles));
            }
        }

        [Test]
        public void TheListRow_GivenForecastCreationTime_ReturnsDateAsUTC()
        {
            var forecastCreationTime = TestToday.FrozenNow.DateTime;
            var feature = new Feature();
            feature.SetFeatureForecasts(new List<WhenForecast> { new() { CreationTime = forecastCreationTime } });

            var (_, row) = BothDtosFor(feature);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(row.LastUpdated, Is.EqualTo(forecastCreationTime));
                Assert.That(row.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
        }

        [Test]
        public void TheFeaturePayloadAndTheListRow_ListOnlyThePortfoliosTheCallerMayRead()
        {
            var feature = new Feature();
            feature.Portfolios.Add(new Portfolio { Id = 1, Name = "Hidden" });
            feature.Portfolios.Add(new Portfolio { Id = 2, Name = "Readable" });

            var (full, row) = BothDtosFor(feature, new HashSet<int> { 2 });
            var unrestricted = new FeatureDto(feature, TestToday.Clock, [], false, null, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(full.Projects.Select(project => (project.Id, project.Name)), Is.EqualTo(OnlyTheReadable));
                Assert.That(row.Projects.Select(project => (project.Id, project.Name)), Is.EqualTo(OnlyTheReadable));
                Assert.That(unrestricted.Projects.Select(project => project.Id), Is.EqualTo(BothPortfolioIds));
            }
        }

        private static (FeatureDto Full, FeatureListRowDto Row) BothDtosFor(Feature feature, ISet<int>? readablePortfolioIds = null)
        {
            var readable = readablePortfolioIds ?? feature.Portfolios.Select(portfolio => portfolio.Id).ToHashSet();
            return (
                new FeatureDto(feature, TestToday.Clock, [], false, null, readable),
                new FeatureListRowDto(feature, TestToday.Clock, [], false, readable));
        }

        private FeatureDto CreateSubject(Feature feature)
        {
            return new FeatureDto(feature, TestToday.Clock, Array.Empty<BlackoutPeriod>(), false, null);
        }
    }
}
