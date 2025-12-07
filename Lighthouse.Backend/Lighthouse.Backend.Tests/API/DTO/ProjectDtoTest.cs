using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class ProjectDtoTest
    {
        [Test]
        public void CreateProjectDto_GivenLastUpdatedTime_ReturnsDateAsUTC()
        {
            var projectUpdateTime = DateTime.Now;
            var project = new Portfolio
            {
                UpdateTime = projectUpdateTime
            };

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.LastUpdated, Is.EqualTo(projectUpdateTime));
                Assert.That(subject.LastUpdated.Kind, Is.EqualTo(DateTimeKind.Utc));
            }
            ;
        }

        [Test]
        public void CreateProjectDto_ProjectWithNoFeatures_RemainingWorkIsZero()
        {
            var project = new Portfolio();

            var subject = CreateSubject(project);

            Assert.That(subject.RemainingWorkItems, Is.Zero);
        }

        [Test]
        public void CreateProjectDto_ProjectWithNoFeatures_TotalWorkIsZero()
        {
            var project = new Portfolio();

            var subject = CreateSubject(project);

            Assert.That(subject.TotalWorkItems, Is.Zero);
        }

        [Test]
        public void CreateProjectDto_ProjectWithSingleFeature_RemainingWorkMatchesFeatureWork()
        {
            var team = new Team { Name = "Team A" };
            var feature = new Feature(team, 10);
            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            Assert.That(subject.RemainingWorkItems, Is.EqualTo(10));
        }

        [Test]
        public void CreateProjectDto_ProjectWithSingleFeature_TotalWorkMatchesFeatureWork()
        {
            var team = new Team { Name = "Team A" };
            var feature = new Feature(team, 15);
            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            Assert.That(subject.TotalWorkItems, Is.EqualTo(15));
        }

        [Test]
        public void CreateProjectDto_ProjectWithMultipleFeatures_RemainingWorkIsSum()
        {
            var team = new Team { Name = "Team A" };
            var feature1 = new Feature(team, 10);
            var feature2 = new Feature(team, 15);
            var feature3 = new Feature(team, 7);

            var project = new Portfolio();
            project.UpdateFeatures([feature1, feature2, feature3]);

            var subject = CreateSubject(project);

            Assert.That(subject.RemainingWorkItems, Is.EqualTo(32));
        }

        [Test]
        public void CreateProjectDto_ProjectWithMultipleFeatures_TotalWorkIsSum()
        {
            var team = new Team { Name = "Team A" };
            var feature1 = new Feature(team, 10);
            var feature2 = new Feature(team, 15);
            var feature3 = new Feature(team, 7);

            var project = new Portfolio();
            project.UpdateFeatures([feature1, feature2, feature3]);

            var subject = CreateSubject(project);

            Assert.That(subject.TotalWorkItems, Is.EqualTo(32));
        }

        [Test]
        public void CreateProjectDto_ProjectWithMultipleTeamsOnSameFeature_RemainingWorkIsSummed()
        {
            var teamA = new Team { Name = "Team A" };
            var teamB = new Team { Name = "Team B" };
            var feature = new Feature([(teamA, 10, 20), (teamB, 15, 25)]);

            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            Assert.That(subject.RemainingWorkItems, Is.EqualTo(25));
        }

        [Test]
        public void CreateProjectDto_ProjectWithMultipleTeamsOnSameFeature_TotalWorkIsSummed()
        {
            var teamA = new Team { Name = "Team A" };
            var teamB = new Team { Name = "Team B" };
            var feature = new Feature([(teamA, 10, 20), (teamB, 15, 25)]);

            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            Assert.That(subject.TotalWorkItems, Is.EqualTo(45));
        }

        [Test]
        public void CreateProjectDto_ProjectWithFeaturesWithDifferentWorkAmounts_CalculatesCorrectTotals()
        {
            var teamA = new Team { Name = "Team A" };
            var teamB = new Team { Name = "Team B" };

            var feature1 = new Feature([(teamA, 5, 10), (teamB, 3, 8)]);
            var feature2 = new Feature(teamA, 12);
            var feature3 = new Feature([(teamA, 7, 15), (teamB, 9, 20)]);

            var project = new Portfolio();
            project.UpdateFeatures([feature1, feature2, feature3]);

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.RemainingWorkItems, Is.EqualTo(36)); // 5+3+12+7+9
                Assert.That(subject.TotalWorkItems, Is.EqualTo(65)); // 10+8+12+15+20
            }
        }

        [Test]
        public void CreateProjectDto_ProjectWithNoFeatures_ForecastsIsEmpty()
        {
            var project = new Portfolio();

            var subject = CreateSubject(project);

            Assert.That(subject.Forecasts, Is.Empty);
        }

        [Test]
        public void CreateProjectDto_ProjectWithSingleFeatureWithForecast_ReturnsForecasts()
        {
            var team = new Team { Name = "Team A" };
            var feature = CreateFeatureWithForecast(team, 10);

            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Forecasts, Has.Count.EqualTo(4));
                Assert.That(subject.Forecasts.Select(f => f.Probability), Is.EquivalentTo([50, 70, 85, 95]));
            }
        }

        [Test]
        public void CreateProjectDto_ProjectForecasts_ContainsAllRequiredProbabilities()
        {
            var team = new Team { Name = "Team A" };
            var feature = CreateFeatureWithForecast(team, 10);

            var project = new Portfolio();
            project.UpdateFeatures([feature]);

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                var probabilities = subject.Forecasts.Select(f => f.Probability).ToList();
                Assert.That(probabilities, Contains.Item(50));
                Assert.That(probabilities, Contains.Item(70));
                Assert.That(probabilities, Contains.Item(85));
                Assert.That(probabilities, Contains.Item(95));
            }
        }

        [Test]
        public void CreateProjectDto_WithCompletedFeature_CountsWorkCorrectly()
        {
            var team = new Team { Name = "Team A" };
            var completedFeature = new Feature([(team, 0, 25)]); // No remaining work, 25 total
            var activeFeature = new Feature(team, 10);

            var project = new Portfolio();
            project.UpdateFeatures([completedFeature, activeFeature]);

            var subject = CreateSubject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.RemainingWorkItems, Is.EqualTo(10)); // Only active feature
                Assert.That(subject.TotalWorkItems, Is.EqualTo(35)); // Both features
            }
        }

        private ProjectDto CreateSubject(Portfolio project)
        {
            return new ProjectDto(project);
        }

        private Feature CreateFeatureWithForecast(Team team, int remainingWork, int daysAt85Percentile = 20)
        {
            var feature = new Feature(team, remainingWork);

            // Create a simulation result with forecast data
            var simulationResult = new SimulationResult(team, feature, remainingWork);

            // Add simulation results to create a proper forecast distribution
            // The 85th percentile will be at daysAt85Percentile
            simulationResult.SimulationResults.Add(daysAt85Percentile - 10, 10);  // 50th percentile
            simulationResult.SimulationResults.Add(daysAt85Percentile - 5, 20);   // 70th percentile  
            simulationResult.SimulationResults.Add(daysAt85Percentile, 30);       // 85th percentile
            simulationResult.SimulationResults.Add(daysAt85Percentile + 10, 40);  // 95th percentile

            var forecast = new WhenForecast(simulationResult);
            feature.SetFeatureForecasts([forecast]);

            return feature;
        }
    }
}
