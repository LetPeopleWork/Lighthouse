using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.Models.Forecast
{
    public class AggregatedWhenForecastTest
    {
        private static readonly int[] Percentiles = [50, 70, 85, 95];

        [Test]
        public void HasSufficientData_OneContributingForecastInsufficient_AggregatesToFalse()
        {
            var sufficient = CreateForecast(hasSufficientData: true);
            var insufficient = CreateForecast(hasSufficientData: false);

            var aggregate = new AggregatedWhenForecast([sufficient, insufficient]);

            Assert.That(aggregate.HasSufficientData, Is.False);
        }

        [Test]
        public void HasSufficientData_AllContributingForecastsSufficient_AggregatesToTrue()
        {
            var first = CreateForecast(hasSufficientData: true);
            var second = CreateForecast(hasSufficientData: true);

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.HasSufficientData, Is.True);
        }

        [Test]
        public void FilterApplied_OneContributingForecastFiltered_AggregatesToTrue()
        {
            var filtered = CreateForecast(filterApplied: true);
            var unfiltered = CreateForecast(filterApplied: false);

            var aggregate = new AggregatedWhenForecast([filtered, unfiltered]);

            Assert.That(aggregate.FilterApplied, Is.True);
        }

        [Test]
        public void ExcludedSummary_DistinctSummariesAcrossForecasts_AreJoinedAndDeduplicated()
        {
            var first = CreateForecast(excludedSummary: "excluded 2 items");
            var second = CreateForecast(excludedSummary: "excluded 5 items");
            var duplicate = CreateForecast(excludedSummary: "excluded 2 items");
            var none = CreateForecast(excludedSummary: null);

            var aggregate = new AggregatedWhenForecast([first, second, duplicate, none]);

            Assert.That(aggregate.ExcludedSummary, Is.EqualTo("excluded 2 items; excluded 5 items"));
        }

        [Test]
        public void ExcludedSummary_NoForecastReportsExclusions_IsNull()
        {
            var first = CreateForecast(excludedSummary: null);
            var second = CreateForecast(excludedSummary: "   ");

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.ExcludedSummary, Is.Null);
        }

        [Test]
        public void GetProbability_TwoTeamsWithMassAtTheSelectedTeamsDate_IsStrictlyLaterThanThatTeam()
        {
            var teamHistogram = new Dictionary<int, int> { { 1, 5000 }, { 2, 2500 }, { 3, 2500 } };
            var first = CreateForecast(histogram: teamHistogram);
            var second = CreateForecast(histogram: new Dictionary<int, int>(teamHistogram));

            var aggregate = new AggregatedWhenForecast([first, second]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.GetProbability(50), Is.EqualTo(1));
                Assert.That(aggregate.GetProbability(50), Is.EqualTo(2));
            }
        }

        [Test]
        public void GetProbability_CrossingContributors_IsAtLeastEveryContributorsSamePercentile()
        {
            var tightLate = CreateForecast(histogram: new Dictionary<int, int> { { 8, 500 }, { 9, 9000 }, { 10, 500 } });
            var wideEarly = CreateForecast(histogram: new Dictionary<int, int> { { 2, 4000 }, { 9, 3000 }, { 20, 3000 } });

            var aggregate = new AggregatedWhenForecast([tightLate, wideEarly]);

            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in Percentiles)
                {
                    Assert.That(aggregate.GetProbability(percentile), Is.GreaterThanOrEqualTo(tightLate.GetProbability(percentile)), $"p{percentile} vs tight-late team");
                    Assert.That(aggregate.GetProbability(percentile), Is.GreaterThanOrEqualTo(wideEarly.GetProbability(percentile)), $"p{percentile} vs wide-early team");
                }
            }
        }

        [Test]
        public void SingleContributor_HistogramAndPercentilesAreIdenticalToThatContributor()
        {
            var contributor = CreateForecast(histogram: new Dictionary<int, int> { { 3, 1 }, { 4, 2 }, { 5, 3 }, { 6, 2 }, { 7, 1 }, { 9, 1 } });

            var aggregate = new AggregatedWhenForecast([contributor]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aggregate.SimulationResult, Is.EqualTo(contributor.SimulationResult));
                Assert.That(aggregate.TotalTrials, Is.EqualTo(contributor.TotalTrials));

                foreach (var percentile in Percentiles)
                {
                    Assert.That(aggregate.GetProbability(percentile), Is.EqualTo(contributor.GetProbability(percentile)), $"p{percentile}");
                }
            }
        }

        [Test]
        public void InputOrder_CrossingContributors_DoesNotChangeTheResult()
        {
            var tightLate = CreateForecast(histogram: new Dictionary<int, int> { { 8, 500 }, { 9, 9000 }, { 10, 500 } });
            var wideEarly = CreateForecast(histogram: new Dictionary<int, int> { { 2, 4000 }, { 9, 3000 }, { 20, 3000 } });

            var forwards = new AggregatedWhenForecast([tightLate, wideEarly]);
            var backwards = new AggregatedWhenForecast([wideEarly, tightLate]);

            Assert.That(backwards.SimulationResult, Is.EqualTo(forwards.SimulationResult));
        }

        [Test]
        public void Provenance_AggregateOfMultipleTeams_CarriesNoTeamIdentity()
        {
            var first = CreateForecast(team: new Team { Id = 1, Name = "Team A" });
            var second = CreateForecast(team: new Team { Id = 2, Name = "Team B" });

            var aggregate = new AggregatedWhenForecast([first, second]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aggregate.Team, Is.Null);
                Assert.That(aggregate.TeamId, Is.Null);
            }
        }

        [Test]
        public void Provenance_NumberOfItems_IsTheSumOfAllContributors()
        {
            var first = CreateForecast(numberOfItems: 3);
            var second = CreateForecast(numberOfItems: 2);

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.NumberOfItems, Is.EqualTo(5));
        }

        [Test]
        public void Provenance_CreationTime_IsTheOldestContributor()
        {
            var oldest = new DateTime(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);
            var first = CreateForecast(creationTime: new DateTime(2026, 7, 27, 8, 0, 0, DateTimeKind.Utc));
            var second = CreateForecast(creationTime: oldest);

            var aggregate = new AggregatedWhenForecast([first, second]);

            Assert.That(aggregate.CreationTime, Is.EqualTo(oldest));
        }

        [Test]
        public void NoContributors_ProducesAnEmptyForecast()
        {
            var aggregate = new AggregatedWhenForecast([]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aggregate.TotalTrials, Is.Zero);
                Assert.That(aggregate.SimulationResult, Is.Empty);
            }
        }

        [Test]
        public void EveryContributorWithoutTrials_KeepsTheContributorHistogram()
        {
            // A feature with no remaining work carries ForecastService's day-0 sentinel {0: 0}. It is a
            // fact, not a forecast, and must not degrade to "no percentile date" (AC-02.3).
            var noRemainingWork = CreateForecast(histogram: new Dictionary<int, int> { { 0, 0 } });

            var aggregate = new AggregatedWhenForecast([noRemainingWork]);

            using (Assert.EnterMultipleScope())
            {
                foreach (var percentile in Percentiles)
                {
                    Assert.That(aggregate.GetProbability(percentile), Is.Zero, $"p{percentile} is today");
                }

                Assert.That(aggregate.GetLikelihood(0), Is.EqualTo(100));
            }
        }

        [Test]
        public void EveryContributorWithoutTrials_KeepsEveryContributorDayRegardlessOfOrder()
        {
            var doneToday = CreateForecast(histogram: new Dictionary<int, int> { { 0, 0 } });
            var doneLater = CreateForecast(histogram: new Dictionary<int, int> { { 5, 0 } });

            var forwards = new AggregatedWhenForecast([doneToday, doneLater]);
            var backwards = new AggregatedWhenForecast([doneLater, doneToday]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forwards.SimulationResult.Keys, Is.EqualTo(new[] { 0, 5 }));
                Assert.That(backwards.SimulationResult, Is.EqualTo(forwards.SimulationResult));
            }
        }

        [Test]
        public void ContributorWithoutTrials_IsExcludedFromTheMathsButStillCountsForProvenance()
        {
            var forecasted = CreateForecast(histogram: new Dictionary<int, int> { { 1, 5 }, { 4, 5 } }, numberOfItems: 3);
            var withoutThroughput = CreateForecast(histogram: [], numberOfItems: 2);

            var aggregate = new AggregatedWhenForecast([forecasted, withoutThroughput]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(aggregate.SimulationResult, Is.EqualTo(forecasted.SimulationResult));
                Assert.That(aggregate.NumberOfItems, Is.EqualTo(5));
            }
        }

        private static WhenForecast CreateForecast(
            bool hasSufficientData = true,
            bool filterApplied = false,
            string? excludedSummary = null,
            Dictionary<int, int>? histogram = null,
            int numberOfItems = 0,
            DateTime? creationTime = null,
            Team? team = null)
        {
            return new WhenForecast(histogram ?? new Dictionary<int, int> { { 10, 100 } })
            {
                HasSufficientData = hasSufficientData,
                FilterApplied = filterApplied,
                ExcludedSummary = excludedSummary,
                NumberOfItems = numberOfItems,
                CreationTime = creationTime ?? DateTime.UtcNow,
                Team = team,
                TeamId = team?.Id,
            };
        }
    }
}
