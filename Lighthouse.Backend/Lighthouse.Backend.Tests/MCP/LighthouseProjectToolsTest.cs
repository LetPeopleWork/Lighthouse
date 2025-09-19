using Lighthouse.Backend.MCP;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.API;
using Moq;
using Newtonsoft.Json;
using NuGet.Protocol;

namespace Lighthouse.Backend.Tests.MCP
{
    public class LighthouseProjectToolsTest : LighthosueToolsBaseTest
    {
        private Mock<IRepository<Project>> projectRepositoryMock;
        private Mock<IForecastService> forecastServiceMock;

        [SetUp]
        public void Setup()
        {
            projectRepositoryMock = new Mock<IRepository<Project>>();
            forecastServiceMock = new Mock<IForecastService>();
            SetupServiceProviderMock(projectRepositoryMock.Object);
            SetupServiceProviderMock(forecastServiceMock.Object);
        }

        [Test]
        public void GetAllProjects_ReturnsIdAndNameAndCounts()
        {
            var project = CreateProject();

            projectRepositoryMock.Setup(x => x.GetAll()).Returns(new List<Project> { project });

            var subject = CreateSubject();
            var result = subject.GetAllProjects();

            using (Assert.EnterMultipleScope())
            {
                var projects = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(projects, Has.Count.EqualTo(1));

                var projectToVerify = projects.Single();
                int projectId = Convert.ToInt32(projectToVerify.Id);
                string projectName = Convert.ToString(projectToVerify.Name);
                int teamCount = Convert.ToInt32(projectToVerify.TeamCount);
                int featureCount = Convert.ToInt32(projectToVerify.FeatureCount);
                int milestoneCount = Convert.ToInt32(projectToVerify.MilestoneCount);

                Assert.That(projectId, Is.EqualTo(project.Id));
                Assert.That(projectName, Is.EqualTo(project.Name));
                Assert.That(teamCount, Is.EqualTo(project.Teams.Count));
                Assert.That(featureCount, Is.EqualTo(project.Features.Count));
                Assert.That(milestoneCount, Is.EqualTo(project.Milestones.Count));
            }
        }

        [Test]
        public void GetProjectByName_WithExistingProject_ReturnsProjectDetails()
        {
            var project = CreateProject();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(1)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectByName(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var projectData = result.FromJson<dynamic>();

                int projectId = Convert.ToInt32(projectData.Id);
                string projectName = Convert.ToString(projectData.Name);

                Assert.That(projectId, Is.EqualTo(project.Id));
                Assert.That(projectName, Is.EqualTo(project.Name));
            }
        }

        [Test]
        public void GetProjectByName_WithNonExistingProject_ReturnsNotFoundMessage()
        {
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns((Project?)null);

            var subject = CreateSubject();
            var result = subject.GetProjectByName("NonExistentProject");

            Assert.That(result, Is.EqualTo("No project found with name NonExistentProject"));
        }

        [Test]
        public void GetProjectFeatures_WithExistingProject_ReturnsFeatures()
        {
            var project = CreateProjectWithFeatures();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(1)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectFeatures(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var features = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(features, Has.Count.EqualTo(1));

                var featureToVerify = features.Single();
                int featureId = Convert.ToInt32(featureToVerify.Id);
                string featureName = Convert.ToString(featureToVerify.Name);

                Assert.That(featureId, Is.EqualTo(project.Features[0].Id));
                Assert.That(featureName, Is.EqualTo(project.Features[0].Name));
            }
        }

        [Test]
        public void GetProjectTeams_WithExistingProject_ReturnsTeams()
        {
            var project = CreateProjectWithTeams();

            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(1)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectTeams(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var teams = result.FromJson<IEnumerable<dynamic>>().ToList();

                Assert.That(teams, Has.Count.EqualTo(1));

                var teamToVerify = teams.Single();
                int teamId = Convert.ToInt32(teamToVerify.Id);
                string teamName = Convert.ToString(teamToVerify.Name);

                Assert.That(teamId, Is.EqualTo(project.Teams[0].Id));
                Assert.That(teamName, Is.EqualTo(project.Teams[0].Name));
            }
        }

        private LighthouseProjectTools CreateSubject()
        {
            return new LighthouseProjectTools(ServiceScopeFactory);
        }

        private Project CreateProject()
        {
            return new Project
            {
                Id = 1,
                Name = "Test Project"
            };
        }

        private Project CreateProjectWithFeatures()
        {
            var project = CreateProject();
            var feature = new Feature
            {
                Id = 1,
                Name = "Test Feature",
                ReferenceId = "TEST-1",
                State = "Active",
                StateCategory = StateCategories.Doing,
                OwningTeam = "Development Team",
                Url = "https://example.com/feature/1"
            };
            project.Features.Add(feature);
            return project;
        }

        private Project CreateProjectWithTeams()
        {
            var project = CreateProject();
            var team = new Team
            {
                Id = 1,
                Name = "Development Team",
                WorkTrackingSystemConnectionId = 1
            };
            project.Teams.Add(team);
            return project;
        }

        private Project CreateProjectWithFeaturesAndForecasts()
        {
            var project = CreateProject();
            
            // Create features with different forecast completion dates
            var feature1 = CreateFeatureWithForecast(1, "Feature 1", 10, 20); // Completes earlier
            var feature2 = CreateFeatureWithForecast(2, "Feature 2", 20, 30); // Completes later (critical path)
            var feature3 = CreateFeatureWithForecast(3, "Feature 3", 15, 25); // Completes in middle
            
            project.Features.Add(feature1);
            project.Features.Add(feature2);
            project.Features.Add(feature3);
            
            return project;
        }

        private Feature CreateFeatureWithForecast(int id, string name, int remainingWork, int forecastDays)
        {
            var feature = new Feature
            {
                Id = id,
                Name = name,
                ReferenceId = $"TEST-{id}",
                State = "Active",
                StateCategory = StateCategories.Doing
            };
            
            // Add remaining work
            var featureWork = new FeatureWork
            {
                TotalWorkItems = remainingWork + 10,
                RemainingWorkItems = remainingWork
            };
            feature.FeatureWork.Add(featureWork);
            
            // Create mock forecast with different probabilities
            var simulationResult = new SimulationResult(new Team(), feature, remainingWork);
            simulationResult.SimulationResults.Add(forecastDays - 5, 2);      // 50th percentile 
            simulationResult.SimulationResults.Add(forecastDays, 3);          // 70th percentile
            simulationResult.SimulationResults.Add(forecastDays + 5, 4);      // 85th percentile
            simulationResult.SimulationResults.Add(forecastDays + 10, 1);     // 95th percentile
            
            var forecast = new WhenForecast(simulationResult);
            feature.SetFeatureForecasts(new List<WhenForecast> { forecast });
            
            return feature;
        }

        private Project CreateProjectWithMilestones()
        {
            var project = CreateProjectWithFeaturesAndForecasts();
            
            // Add milestones at different dates
            var milestone1 = new Milestone
            {
                Id = 1,
                Name = "Beta Release",
                Date = DateTime.Today.AddDays(15)
            };
            
            var milestone2 = new Milestone
            {
                Id = 2,
                Name = "Final Release", 
                Date = DateTime.Today.AddDays(35)
            };
            
            project.Milestones.Add(milestone1);
            project.Milestones.Add(milestone2);
            
            return project;
        }

        [Test]
        public void RunProjectWhenForecast_ProjectDoesNotExist_ReturnsErrorMessage()
        {
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns((Project?)null);

            var subject = CreateSubject();
            var result = subject.RunProjectWhenForecast("NonExistentProject");

            Assert.That(result, Is.EqualTo("No project found with name NonExistentProject"));
        }

        [Test]
        public void RunProjectWhenForecast_ProjectHasNoFeatures_ReturnsNoFeaturesMessage()
        {
            var project = CreateProject();
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.RunProjectWhenForecast(project.Name);

            Assert.That(result, Is.EqualTo("Project Test Project has no features to forecast"));
        }

        [Test]
        public void RunProjectWhenForecast_AllFeaturesCompleted_ReturnsCompletionMessage()
        {
            var project = CreateProject();
            
            // Create feature with no remaining work
            var completedFeature = new Feature
            {
                Id = 1,
                Name = "Completed Feature",
                ReferenceId = "TEST-1"
            };
            var featureWork = new FeatureWork
            {
                TotalWorkItems = 10,
                RemainingWorkItems = 0 // No remaining work
            };
            completedFeature.FeatureWork.Add(featureWork);
            project.Features.Add(completedFeature);
            
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.RunProjectWhenForecast(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var response = result.FromJson<dynamic>();
                
                string message = Convert.ToString(response.Message);
                string status = Convert.ToString(response.ProjectCompletionStatus);
                
                Assert.That(message, Is.EqualTo("All features in the project are completed"));
                Assert.That(status, Is.EqualTo("Complete"));
            }
        }

        [Test]
        public void RunProjectWhenForecast_WithValidProject_ReturnsCriticalPathAnalysis()
        {
            var project = CreateProjectWithFeaturesAndForecasts();
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.RunProjectWhenForecast(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var response = result.FromJson<dynamic>();
                
                string projectName = Convert.ToString(response.ProjectName);
                int projectId = Convert.ToInt32(response.ProjectId);
                
                // Critical path should be Feature 2 (has latest completion at 85th percentile = 30 + 5 = 35 days)
                string criticalFeatureName = Convert.ToString(response.CriticalPathFeature.Name);
                int criticalFeatureId = Convert.ToInt32(response.CriticalPathFeature.Id);
                
                // Verify project details
                Assert.That(projectName, Is.EqualTo(project.Name));
                Assert.That(projectId, Is.EqualTo(project.Id));
                
                // Verify critical path identification
                Assert.That(criticalFeatureName, Is.EqualTo("Feature 2"));
                Assert.That(criticalFeatureId, Is.EqualTo(2));
                
                // Verify forecast probabilities exist
                Assert.That(response.ProjectForecast.Probability50, Is.Not.Null);
                Assert.That(response.ProjectForecast.Probability70, Is.Not.Null);
                Assert.That(response.ProjectForecast.Probability85, Is.Not.Null);
                Assert.That(response.ProjectForecast.Probability95, Is.Not.Null);
                
                // Verify completion dates are in the future
                DateTime completionDate85 = Convert.ToDateTime(response.EstimatedCompletionDates.Probability85);
                Assert.That(completionDate85, Is.GreaterThan(DateTime.Today));
            }
        }

        [Test]
        public void GetProjectMilestones_ProjectDoesNotExist_ReturnsErrorMessage()
        {
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns((Project?)null);

            var subject = CreateSubject();
            var result = subject.GetProjectMilestones("NonExistentProject");

            Assert.That(result, Is.EqualTo("No project found with name NonExistentProject"));
        }

        [Test]
        public void GetProjectMilestones_ProjectHasNoMilestones_ReturnsNoMilestonesMessage()
        {
            var project = CreateProject();
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectMilestones(project.Name);

            Assert.That(result, Is.EqualTo("Project Test Project has no milestones defined"));
        }

        [Test]
        public void GetProjectMilestones_WithValidProject_ReturnsMilestoneAnalysis()
        {
            var project = CreateProjectWithMilestones();
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectMilestones(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var response = result.FromJson<dynamic>();
                
                string projectName = Convert.ToString(response.ProjectName);
                int totalMilestones = Convert.ToInt32(response.TotalMilestones);
                int futureMilestones = Convert.ToInt32(response.FutureMilestones);
                
                Assert.That(projectName, Is.EqualTo(project.Name));
                Assert.That(totalMilestones, Is.EqualTo(2));
                Assert.That(futureMilestones, Is.EqualTo(2)); // Both milestones are in the future
                
                // Verify milestones array
                var milestones = response.Milestones;
                Assert.That(milestones, Is.Not.Null);
                
                // Convert to array for easier access
                var milestonesArray = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic[]>(milestones.ToString());
                Assert.That(milestonesArray, Has.Length.EqualTo(2));
                
                // Verify first milestone (Beta Release at day 15)
                string firstMilestoneName = Convert.ToString(milestonesArray[0].Name);
                int firstMilestoneDays = Convert.ToInt32(milestonesArray[0].DaysFromNow);
                
                Assert.That(firstMilestoneName, Is.EqualTo("Beta Release"));
                Assert.That(firstMilestoneDays, Is.EqualTo(15));
                
                // Verify risk assessment exists
                string riskAssessment = Convert.ToString(milestonesArray[0].RiskAssessment);
                Assert.That(riskAssessment, Is.Not.Null);
                Assert.That(riskAssessment, Is.Not.Empty);
            }
        }

        [Test]
        public void GetProjectMilestones_OnlyFutureMilestones_FiltersPastMilestones()
        {
            var project = CreateProjectWithMilestones();
            
            // Add a past milestone
            var pastMilestone = new Milestone
            {
                Id = 3,
                Name = "Past Milestone",
                Date = DateTime.Today.AddDays(-10) // In the past
            };
            project.Milestones.Add(pastMilestone);
            
            projectRepositoryMock.Setup(x => x.GetByPredicate(It.IsAny<Func<Project, bool>>())).Returns(project);
            projectRepositoryMock.Setup(x => x.GetById(project.Id)).Returns(project);

            var subject = CreateSubject();
            var result = subject.GetProjectMilestones(project.Name);

            using (Assert.EnterMultipleScope())
            {
                var response = result.FromJson<dynamic>();
                
                int totalMilestones = Convert.ToInt32(response.TotalMilestones);
                int futureMilestones = Convert.ToInt32(response.FutureMilestones);
                
                Assert.That(totalMilestones, Is.EqualTo(3)); // All milestones
                Assert.That(futureMilestones, Is.EqualTo(2)); // Only future milestones analyzed
            }
        }
    }
}