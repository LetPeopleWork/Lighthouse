using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Linear
{
    public class LinearWorkTrackingConnectorTest
    {
        [Test]
        public async Task ValidateConnection_GivenValidSettings_ReturnsTrue()
        {
            var subject = CreateSubject();

            var apiKey = Environment.GetEnvironmentVariable("LinearAPIKey") ?? throw new NotSupportedException("Can run test only if Environment Variable 'LinearAPIKey' is set!");

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Linear, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = LinearWorkTrackingOptionNames.ApiKey, Value = apiKey, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("Yah-yah-yah, Coco Jamboo, yah-yah-yeh")]
        [TestCase("")]
        public async Task ValidateConnection_GivenInvalidApiKey_ReturnsFalse(string apiKey)
        {
            var subject = CreateSubject();

            var connectionSetting = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Linear, Name = "Test Setting" };
            connectionSetting.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = LinearWorkTrackingOptionNames.ApiKey, Value = apiKey, IsSecret = true },
                ]);

            var isValid = await subject.ValidateConnection(connectionSetting);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task ValidateTeamSettings_GivenValidTeamId_ReturnsTrue()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("00000000-0000-0000-0000-000000000000")]
        [TestCase("")]
        public async Task ValidateTeamSettings_GivenInvalidTeamId_ReturnsFalse(string teamId)
        {
            var subject = CreateSubject();

            var team = CreateTeam();
            team.DataRetrievalValue = teamId;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task GetWorkItemsForTeam_AllWorkItemsHaveIssueType()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            var workItems = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.ToList(), Has.Count.GreaterThan(0));
                Assert.That(workItems.All(w => w.Type == "Issue"), Is.True);
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_FiltersForSpecifiedStates_ReturnsExpectedWorkItems()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            team.ToDoStates.Clear();
            team.DoingStates.Clear();

            var workItems = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.ToList(), Has.Count.GreaterThan(1));
                var workItem = workItems.Single();

                Assert.That(workItem.ReferenceId, Is.EqualTo("lig-5"));
                Assert.That(workItem.Name, Is.EqualTo("Customize settings"));
                Assert.That(workItem.State, Is.EqualTo("Done"));
                Assert.That(workItem.Type, Is.EqualTo("Issue"));
                Assert.That(workItem.ParentReferenceId, Is.Not.Null);
                Assert.That(workItem.Url, Is.EqualTo("https://linear.app/lighthousedemo/issue/LIG-5/customize-settings"));

                Assert.That(workItem.Order, Is.Not.Null);

                Assert.That(workItem.CreatedDate, Is.EqualTo(new DateTime(2025, 04, 23, 07, 12, 32, 958, DateTimeKind.Utc)));
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 04, 23, 08, 27, 38, 556, DateTimeKind.Utc)));
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 04, 23, 08, 27, 38, 556, DateTimeKind.Utc)));
            }
        }

        [Test]
        public async Task GetWorkItemsForTeam_SetsOrderCorrect()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            team.DoneStates.Clear();

            var workItems = await subject.GetWorkItemsForTeam(team);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(workItems.ToList(), Has.Count.GreaterThan(1));
                var item1 = workItems.Single(x => x.ReferenceId == "lig-2");
                var item2 = workItems.Single(x => x.ReferenceId == "lig-13");

                var item1Order = double.Parse(item1.Order);
                var item2Order = double.Parse(item2.Order);

                Assert.That(item2Order, Is.GreaterThan(item1Order));
            }
        }

        [Test]
        public async Task ValidatePortfolioSettings_WorkspaceWithProjects_ReturnsTrue()
        {
            var subject = CreateSubject();

            var project = CreatePortfolio();

            var isValid = await subject.ValidatePortfolioSettings(project);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task ValidatePortfolioSettings_DataRetrievalValueIgnored_StillReturnsTrue()
        {
            var subject = CreateSubject();

            var project = CreatePortfolio();
            project.DataRetrievalValue = "NonExistentProject";

            var isValid = await subject.ValidatePortfolioSettings(project);

            Assert.That(isValid, Is.True);
        }

        [Test]
        public async Task GetFeaturesForProject_ReturnsProjectsAsFeatures()
        {
            var project = CreatePortfolio();

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Has.Count.GreaterThan(0));
                Assert.That(features.All(f => f.Type == "Project"), Is.True);
                Assert.That(features.All(f => !string.IsNullOrEmpty(f.ReferenceId)), Is.True);
                Assert.That(features.All(f => !string.IsNullOrEmpty(f.Name)), Is.True);
            }
        }

        [Test]
        public async Task GetFeaturesForProject_WithNonMatchingStates_ReturnsNoFeatures()
        {
            var project = CreatePortfolio();
            project.ToDoStates.Clear();
            project.DoingStates.Clear();
            project.DoneStates.Clear();
            project.ToDoStates.Add("NonExistentState");

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            Assert.That(features, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetFeaturesForProject_FeaturesHaveStableProjectIdReference()
        {
            var project = CreatePortfolio();

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(features, Has.Count.GreaterThan(0));
                Assert.That(features.All(f => !string.IsNullOrEmpty(f.ReferenceId)), Is.True);
                Assert.That(features.All(f => f.ReferenceId != f.Name), Is.True, "ReferenceId should be a UUID, not a name");
            }
        }

        [Test]
        public async Task GetFeaturesForProject_ProjectsLinkedToInitiatives_HaveParentReferenceId()
        {
            var project = CreatePortfolio();

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            // At least verify the ParentReferenceId is set for projects that have initiatives
            // Not all projects may be linked to initiatives, so we just check the field is populated when present
            Assert.That(features, Has.Count.GreaterThan(0));

            var featuresWithInitiative = features.Where(f => !string.IsNullOrEmpty(f.ParentReferenceId)).ToList();

            foreach (var feature in featuresWithInitiative)
            {
                Assert.That(feature.ParentReferenceId, Does.Not.Contain(" "),
                    $"ParentReferenceId '{feature.ParentReferenceId}' for {feature.Name} should be a UUID");
            }
        }

        [Test]
        public async Task GetParentFeaturesDetails_WithInitiativeIds_ReturnsRealInitiatives()
        {
            var project = CreatePortfolio();

            var subject = CreateSubject();

            // First get features to find any initiative references
            var features = await subject.GetFeaturesForProject(project);
            var initiativeIds = features
                .Where(f => !string.IsNullOrEmpty(f.ParentReferenceId))
                .Select(f => f.ParentReferenceId)
                .Distinct()
                .ToList();

            var parentFeatures = await subject.GetParentFeaturesDetails(project, initiativeIds);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parentFeatures, Has.Count.GreaterThan(0));
                Assert.That(parentFeatures.All(f => f.Type == "Initiative"), Is.True, "Parent features should be typed as Initiative");
                Assert.That(parentFeatures.All(f => f.Name != "Parent Feature"), Is.True, "Parent features should have real names, not placeholder");
                Assert.That(parentFeatures.All(f => !string.IsNullOrEmpty(f.ReferenceId)), Is.True);
            }
        }

        [Test]
        public async Task GetParentFeaturesDetails_WithInvalidIds_ReturnsEmptyList()
        {
            var project = CreatePortfolio();

            var subject = CreateSubject();

            var invalidIds = new[] { "00000000-0000-0000-0000-000000000000" };

            var parentFeatures = await subject.GetParentFeaturesDetails(project, invalidIds);

            Assert.That(parentFeatures, Has.Count.EqualTo(0));
        }

        [Test]
        public async Task GetFeatureWithParent_SetsCorrectParentReferenceId()
        {
            var project = CreatePortfolio();
            var subject = CreateSubject();
            
            var features = await subject.GetFeaturesForProject(project);

            var featureWithParent = features.Single(x => x.Name == "Integration Test Project");
            Assert.That(featureWithParent.ParentReferenceId, Is.EqualTo("b87bc74b-cb77-4c45-84a5-4dd7460c6873"));
        }

        [Test]
        public async Task GetParentFeaturesDetails_WithParentReferenceId_SetsCorrectParentReferenceId()
        {
            var project = CreatePortfolio();
            var subject = CreateSubject();
            
            var parentDetails = await subject.GetParentFeaturesDetails(project, ["b87bc74b-cb77-4c45-84a5-4dd7460c6873"]);

            var testInitiative = parentDetails.Single();
            Assert.That(testInitiative.Name, Is.EqualTo("Test Initative"));
        }

        private static Team CreateTeam()
        {
            var connection = CreateConnection();

            var team = new Team
            {
                Name = "Test Team",
                DataRetrievalValue = "LighthouseDemo",
                WorkTrackingSystemConnection = connection
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Issue");

            team.ToDoStates.Clear();
            team.ToDoStates.Add("Backlog");
            team.ToDoStates.Add("Planned");

            team.DoingStates.Clear();
            team.DoingStates.Add("Development");
            team.DoingStates.Add("Resolved");

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");

            return team;
        }

        private static Portfolio CreatePortfolio()
        {
            var connection = CreateConnection();

            var project = new Portfolio
            {
                Name = "Integration Test Project",
                DataRetrievalValue = string.Empty,
                WorkTrackingSystemConnection = connection
            };

            project.WorkItemTypes.Clear();

            project.ToDoStates.Clear();
            project.ToDoStates.Add("Planned");
            project.ToDoStates.Add("Backlog");

            project.DoingStates.Clear();
            project.DoingStates.Add("In Progress");

            project.DoneStates.Clear();
            project.DoneStates.Add("Completed");

            return project;
        }

        private static WorkTrackingSystemConnection CreateConnection()
        {
            var apiKey = Environment.GetEnvironmentVariable("LinearAPIKey") ?? throw new NotSupportedException("Can run test only if Environment Variable 'LinearAPIKey' is set!");

            var connection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Linear, Name = "Test Connection" };
            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = LinearWorkTrackingOptionNames.ApiKey, Value = apiKey, IsSecret = true },
            ]);

            return connection;
        }

        private static LinearWorkTrackingConnector CreateSubject()
        {
            return new LinearWorkTrackingConnector(Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
