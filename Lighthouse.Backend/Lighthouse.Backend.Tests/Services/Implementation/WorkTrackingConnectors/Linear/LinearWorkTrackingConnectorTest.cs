using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Microsoft.Extensions.Logging;
using Lighthouse.Backend.Services.Interfaces.WorkTrackingConnectors;

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
        public async Task ValidateTeamSettings_GivenValidTeamName_ReturnsTrue()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("NonExistentTeam")]
        [TestCase("")]
        public async Task ValidateTeamSettings_GivenInvalidTeamName_ReturnsFalse(string teamName)
        {
            var subject = CreateSubject();

            var team = CreateTeam();
            team.WorkItemQuery = teamName;

            var isValid = await subject.ValidateTeamSettings(team);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task GetWorkItemsForTeam_FiltersForSpecificedWorkItemTypes_ReturnsExcpectedWorkItems()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Bug");

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.Multiple(() =>
            {
                Assert.That(workItems.ToList(), Has.Count.EqualTo(1));
                var workItem = workItems.Single();

                Assert.That(workItem.ReferenceId, Is.EqualTo("lig-13"));
                Assert.That(workItem.Name, Is.EqualTo("Bug 1"));
                Assert.That(workItem.State, Is.EqualTo("Development"));
                Assert.That(workItem.Type, Is.EqualTo("Bug"));
                Assert.That(workItem.ParentReferenceId, Is.EqualTo("lig-5"));
                Assert.That(workItem.Url, Is.EqualTo("https://linear.app/lighthousedemo/issue/LIG-13/bug-1"));

                Assert.That(workItem.Order, Is.Not.Null);

                Assert.That(workItem.CreatedDate, Is.EqualTo(new DateTime(2025, 04, 24, 11, 05, 51, 877, DateTimeKind.Utc)));
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 04, 24, 11, 06, 40, 918, DateTimeKind.Utc)));
                Assert.That(workItem.ClosedDate, Is.Null);
            });
        }

        [Test]
        public async Task GetWorkItemsForTeam_FiltersForSpecificedStates_ReturnsExcpectedWorkItems()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            team.ToDoStates.Clear();
            team.DoingStates.Clear();

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.Multiple(() =>
            {
                Assert.That(workItems.ToList(), Has.Count.EqualTo(1));
                var workItem = workItems.Single();

                Assert.That(workItem.ReferenceId, Is.EqualTo("lig-5"));
                Assert.That(workItem.Name, Is.EqualTo("Customize settings"));
                Assert.That(workItem.State, Is.EqualTo("Done"));
                Assert.That(workItem.Type, Is.EqualTo("Default"));
                Assert.That(workItem.ParentReferenceId, Is.EqualTo(string.Empty));
                Assert.That(workItem.Url, Is.EqualTo("https://linear.app/lighthousedemo/issue/LIG-5/customize-settings"));

                Assert.That(workItem.Order, Is.Not.Null);

                Assert.That(workItem.CreatedDate, Is.EqualTo(new DateTime(2025, 04, 23, 07, 12, 32, 958, DateTimeKind.Utc)));
                Assert.That(workItem.StartedDate, Is.EqualTo(new DateTime(2025, 04, 23, 08, 27, 38, 556, DateTimeKind.Utc)));
                Assert.That(workItem.ClosedDate, Is.EqualTo(new DateTime(2025, 04, 23, 08, 27, 38, 556, DateTimeKind.Utc)));
            });
        }

        [Test]
        public async Task GetWorkItemsForTeam_SetsOrderCorrect()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoingStates.Add("Development");

            team.DoneStates.Clear();

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.Multiple(() =>
            {
                Assert.That(workItems.ToList(), Has.Count.EqualTo(2));
                var item1 = workItems.First();
                var item2 = workItems.Last();

                Assert.That(item2.ReferenceId, Is.EqualTo("lig-2"));
                Assert.That(item1.ReferenceId, Is.EqualTo("lig-13"));

                var item1Order = double.Parse(item1.Order);
                var item2Order = double.Parse(item2.Order);

                // Ordering = Manual, Less = "higher up"
                Assert.That(item2Order, Is.LessThan(item1Order));
            });
        }

        [Test]
        [TestCase(RelativeOrder.Below)]
        [TestCase(RelativeOrder.Above)]
        public void GetAdjacentOrderIndex_NoExistingItems_Returns0(RelativeOrder relativeOrder)
        {
            var subject = CreateSubject();

            var orderIndex = subject.GetAdjacentOrderIndex([], relativeOrder);
            
            Assert.That(orderIndex, Is.EqualTo("0"));
        }

        [Test]
        [TestCase(RelativeOrder.Below, new string[] { "-92.3", "-82.9", "-83.23" }, "-93.3")]
        [TestCase(RelativeOrder.Above, new string[] { "-92.3", "-82.9", "-83.23" }, "-81.9")]
        [TestCase(RelativeOrder.Below, new string[] { "92.3", "82.9", "83.23" }, "81.9")]
        [TestCase(RelativeOrder.Above, new string[] { "92.3", "82.9", "83.23" }, "93.3")]
        [TestCase(RelativeOrder.Above, new string[] { "13.37", "BANANA?", "188.6" }, "189.6")]
        public void GetAdjacentOrderIndex_ExistingItems_ReturnsCorrectOrderIndex(RelativeOrder relativeOrder, string[] existingItemsOrder, string expectedOrderIndex)
        {
            var subject = CreateSubject();

            var orderIndex = subject.GetAdjacentOrderIndex(existingItemsOrder, relativeOrder);
            
            Assert.That(orderIndex, Is.EqualTo(expectedOrderIndex));
        }

        [Test]
        public async Task GetWorkItemsIdsForTeamWithAdditionalQuery_GivenQuery_IgnoresAdditionalQueryAndReturnsWorkItems()
        {
            var subject = CreateSubject();

            var team = CreateTeam();

            var expectedItems = await subject.GetWorkItemsForTeam(team);

            var actualItems = await subject.GetWorkItemsIdsForTeamWithAdditionalQuery(team, "SomeAdditionalQuery");
            
            Assert.That(actualItems, Is.EquivalentTo(expectedItems.Select(x => x.ReferenceId)));
        }

        [Test]
        public async Task ValidateProjectSettings_GivenValidProjectName_ReturnsTrue()
        {
            var subject = CreateSubject();

            var project = CreateProject();

            var isValid = await subject.ValidateProjectSettings(project);

            Assert.That(isValid, Is.True);
        }

        [Test]
        [TestCase("NonExistentProject")]
        [TestCase("")]
        public async Task ValidateProjectSettings_GivenInvalidProjectName_ReturnsFalse(string projectName)
        {
            var subject = CreateSubject();

            var project = CreateProject();
            project.WorkItemQuery = projectName;

            var isValid = await subject.ValidateProjectSettings(project);

            Assert.That(isValid, Is.False);
        }

        [Test]
        public async Task GetFeaturesForProject_FilterForState_ReturnsCorrectFeature()
        {
            var project = CreateProject();
            project.ToDoStates.Clear();
            project.ToDoStates.Add("Backlog");

            project.DoingStates.Clear();
            project.DoneStates.Clear();

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(features.ToList(), Has.Count.EqualTo(1));
                var feature = features.Single();

                Assert.That(feature.ReferenceId, Is.EqualTo("lig-11"));

                Assert.That(feature.Name, Is.EqualTo("Feature 1"));
                Assert.That(feature.State, Is.EqualTo("Backlog"));
                Assert.That(feature.Type, Is.EqualTo("Feature"));
                Assert.That(feature.ParentReferenceId, Is.EqualTo(string.Empty));
                Assert.That(feature.Url, Is.EqualTo("https://linear.app/lighthousedemo/issue/LIG-11/feature-1"));

                Assert.That(feature.Order, Is.Not.Null);

                Assert.That(feature.CreatedDate, Is.EqualTo(new DateTime(2025, 04, 23, 07, 25, 09, 648, DateTimeKind.Utc)));
                Assert.That(feature.StartedDate, Is.Null);
                Assert.That(feature.ClosedDate, Is.Null);
            });
        }

        [Test]
        public async Task GetFeaturesForProject_FilterForType_ReturnsCorrectFeature()
        {
            var project = CreateProject();

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Feature");

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            Assert.Multiple(() =>
            {
                Assert.That(features.ToList(), Has.Count.EqualTo(1));
                var feature = features.Single();

                Assert.That(feature.ReferenceId, Is.EqualTo("lig-11"));

                Assert.That(feature.Name, Is.EqualTo("Feature 1"));
                Assert.That(feature.State, Is.EqualTo("Backlog"));
                Assert.That(feature.Type, Is.EqualTo("Feature"));
                Assert.That(feature.ParentReferenceId, Is.EqualTo(string.Empty));
                Assert.That(feature.Url, Is.EqualTo("https://linear.app/lighthousedemo/issue/LIG-11/feature-1"));

                Assert.That(feature.Order, Is.Not.Null);

                Assert.That(feature.CreatedDate, Is.EqualTo(new DateTime(2025, 04, 23, 07, 25, 09, 648, DateTimeKind.Utc)));
                Assert.That(feature.StartedDate, Is.Null);
                Assert.That(feature.ClosedDate, Is.Null);
            });
        }

        [Test]
        public async Task GetFeaturesForProject_FiltersOutSubIssues()
        {
            var project = CreateProject();

            var subject = CreateSubject();

            var features = await subject.GetFeaturesForProject(project);

            Assert.That(features.ToList(), Has.Count.EqualTo(2));
        }

        private Team CreateTeam()
        {
            var connection = CreateConnection();

            var team = new Team
            {
                Name = "Test Team",
                WorkItemQuery = "LighthouseDemo",
                WorkTrackingSystemConnection = connection
            };

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Default");
            team.WorkItemTypes.Add("Bug");

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

        private Project CreateProject()
        {
            var connection = CreateConnection();

            var project = new Project
            {
                Name = "Test Project",
                WorkItemQuery = "My Demo Project",
                WorkTrackingSystemConnection = connection
            };

            project.WorkItemTypes.Clear();
            project.WorkItemTypes.Add("Feature");
            project.WorkItemTypes.Add("Default");

            project.ToDoStates.Clear();
            project.ToDoStates.Add("Backlog");
            project.ToDoStates.Add("Planned");

            project.DoingStates.Clear();
            project.DoingStates.Add("Development");
            project.DoingStates.Add("Resolved");

            project.DoneStates.Clear();
            project.DoneStates.Add("Done");

            return project;
        }

        private WorkTrackingSystemConnection CreateConnection()
        {
            var apiKey = Environment.GetEnvironmentVariable("LinearAPIKey") ?? throw new NotSupportedException("Can run test only if Environment Variable 'LinearAPIKey' is set!");

            var connection = new WorkTrackingSystemConnection { WorkTrackingSystem = WorkTrackingSystems.Linear, Name = "Test Connection" };
            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = LinearWorkTrackingOptionNames.ApiKey, Value = apiKey, IsSecret = true },
            ]);

            return connection;
        }

        private LinearWorkTrackingConnector CreateSubject()
        {
            return new LinearWorkTrackingConnector(Mock.Of<ILogger<LinearWorkTrackingConnector>>(), new FakeCryptoService());
        }
    }
}
