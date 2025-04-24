using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Microsoft.Extensions.Logging;

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

            team.DoingStates.Add("Development");

            var workItems = await subject.GetWorkItemsForTeam(team);

            Assert.Multiple(() =>
            {
                Assert.That(workItems.ToList(), Has.Count.EqualTo(1));
                var workItem = workItems.Single();

                Assert.That(workItem.ReferenceId, Is.EqualTo("lig-13"));
                Assert.That(workItem.Name, Is.EqualTo("Bug 1"));
                Assert.That(workItem.State, Is.EqualTo("Development"));
                Assert.That(workItem.Type, Is.EqualTo("Bug"));
                Assert.That(workItem.ParentReferenceId, Is.EqualTo("LIG-5"));
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

            team.WorkItemTypes.Clear();
            team.WorkItemTypes.Add("Default");

            team.ToDoStates.Clear();
            team.DoingStates.Clear();

            team.DoneStates.Clear();
            team.DoneStates.Add("Done");

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

        private Team CreateTeam()
        {
            var connection = CreateConnection();

            return new Team
            {
                Name = "Test Team",
                WorkItemQuery = "LighthouseDemo",
                WorkTrackingSystemConnection = connection
            };
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
