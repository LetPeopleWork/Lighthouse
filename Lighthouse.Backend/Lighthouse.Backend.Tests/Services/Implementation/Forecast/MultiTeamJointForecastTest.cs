using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;
using Lighthouse.Backend.Services.Implementation.Forecast;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Lighthouse.Backend.Tests.TestDoubles;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.Forecast
{
    // Story #5569, Tier 3 of the slice-01 test strategy: real Monte Carlo through the real aggregation.
    // Throughput [1, 3] with 3 items has the closed form {1: .50, 2: .25, 3: .25} per team, so the joint
    // CDF is .25 / .5625 / 1.00. Assertions are on percentile *days* rather than probabilities, which is
    // robust against the ~0.5 % sampling error at 10 000 trials.
    public class MultiTeamJointForecastTest
    {
        private Mock<IRepository<Feature>> featureRepositoryMock;
        private Mock<ITeamMetricsService> teamMetricsServiceMock;

        private int idCounter;

        [SetUp]
        public void Setup()
        {
            featureRepositoryMock = new Mock<IRepository<Feature>>();
            teamMetricsServiceMock = new Mock<ITeamMetricsService>();
        }

        [Test]
        public async Task FeatureForecast_TwoTeamsWithTwoValueThroughput_IsLaterThanEveryContributingTeam()
        {
            var subject = CreateSubject(new DrawsAfreshEveryTime());
            var feature = SetupFeature(CreateTeam([1, 3]), CreateTeam([1, 3]));

            await subject.UpdateForecastsForPortfolio(CreatePortfolio(feature));

            using (Assert.EnterMultipleScope())
            {
                foreach (var contributor in feature.Forecasts)
                {
                    Assert.That(contributor.GetProbability(70), Is.EqualTo(2), "contributing team p70");
                }

                Assert.That(feature.Forecast.GetProbability(70), Is.EqualTo(3), "joint p70");

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    foreach (var contributor in feature.Forecasts)
                    {
                        Assert.That(feature.Forecast.GetProbability(percentile), Is.GreaterThanOrEqualTo(contributor.GetProbability(percentile)), $"p{percentile}");
                    }
                }
            }
        }

        [Test]
        public async Task FeatureForecast_ConstantThroughputTeams_MatchesTheSlowestTeam()
        {
            // Plumbing anchor only. A team at throughput 1/day finishes on a single day with probability 1,
            // and the product of point masses IS their maximum - this passes against the old worst-team copy
            // too, so it proves wiring, never the fix. The discriminating fixtures are the two-value ones.
            var subject = CreateSubject(new DrawsTheSameNumberEveryTime());
            var feature = SetupFeature((CreateTeam([1]), 6), (CreateTeam([1]), 3));

            await subject.UpdateForecastsForPortfolio(CreatePortfolio(feature));

            Assert.That(feature.Forecast.GetProbability(85), Is.EqualTo(6));
        }

        private ForecastService CreateSubject(IDrawStreamFactory draws)
        {
            return new ForecastService(new RandomNumberService(), Mock.Of<ILogger<ForecastService>>(), teamMetricsServiceMock.Object, featureRepositoryMock.Object, new NothingWaitsForAnything(), draws, ForecastSimulationLimits.Default);
        }

        private Team CreateTeam(int[] throughput)
        {
            var team = new Team
            {
                Name = $"Team {idCounter}",
                FeatureWIP = 1,
                Id = idCounter++,
            };

            var runChart = new RunChartData(RunChartDataGenerator.GenerateRunChartData(throughput));
            teamMetricsServiceMock.Setup(x => x.GetCurrentThroughputForTeamForecast(team, ThroughputFilterMode.RespectTeamSetting)).Returns(runChart);
            teamMetricsServiceMock.Setup(x => x.GetForecastThroughputStatus(team, ThroughputFilterMode.RespectTeamSetting)).Returns(new ForecastThroughputStatus(runChart, false, null));

            return team;
        }

        private Feature SetupFeature(params Team[] teams)
        {
            return SetupFeature(teams.Select(team => (team, 3)).ToArray());
        }

        private Feature SetupFeature(params (Team team, int remainingItems)[] work)
        {
            var feature = new Feature(work.Select(w => (w.team, w.remainingItems, w.remainingItems)))
            {
                Id = idCounter,
                ReferenceId = $"{idCounter++}",
            };

            featureRepositoryMock.Setup(x => x.GetAll()).Returns([feature]);

            return feature;
        }

        private Portfolio CreatePortfolio(params Feature[] features)
        {
            var portfolio = new Portfolio
            {
                Name = "Portfolio",
                Id = idCounter++,
                UpdateTime = DateTime.UtcNow,
            };

            portfolio.UpdateFeatures(features);

            return portfolio;
        }
    }
}
