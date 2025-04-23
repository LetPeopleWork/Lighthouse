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
