using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.OAuth;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;

namespace Lighthouse.Backend.Tests.API.DTO
{
    public class WorkTrackingSystemConnectionDtoTest
    {
        [Test]
        public void Create_SetsIdAndNameCorrect()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.Id = 37;
            workTrackingSystemConnection.Name = "My Connect";
            workTrackingSystemConnection.WorkTrackingSystem = WorkTrackingSystems.AzureDevOps;

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.Id, Is.EqualTo(workTrackingSystemConnection.Id));
                Assert.That(subject.Name, Is.EqualTo(workTrackingSystemConnection.Name));
                Assert.That(subject.WorkTrackingSystem, Is.EqualTo(workTrackingSystemConnection.WorkTrackingSystem));
            };
        }

        [Test]
        public void Create_SetsTrackingOptions()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.Options.Add(new WorkTrackingSystemConnectionOption());

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.Options, Has.Count.EqualTo(1));
        }

        [Test]
        public void Create_SetsAuthenticationMethodKey()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                AuthenticationMethodKey = "ado.pat"
            };

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.AuthenticationMethodKey, Is.EqualTo("ado.pat"));
        }

        [Test]
        public void Create_AuthenticationMethodKey_WhenEmpty_RemainsEmpty()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.AuthenticationMethodKey, Is.EqualTo(string.Empty));
        }

        [Test]
        public void Create_SetsAdditionalFieldDefinitions()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 42,
                DisplayName = "Iteration Path",
                Reference = "System.IterationPath"
            });

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(subject.AdditionalFieldDefinitions, Has.Count.EqualTo(1));
                Assert.That(subject.AdditionalFieldDefinitions[0].DisplayName, Is.EqualTo("Iteration Path"));
                Assert.That(subject.AdditionalFieldDefinitions[0].Reference, Is.EqualTo("System.IterationPath"));
                Assert.That(subject.AdditionalFieldDefinitions[0].Id, Is.EqualTo(42));
            }
        }

        [Test]
        public void Create_AdditionalFieldDefinitions_PreservesId()
        {
            var field = new AdditionalFieldDefinition
            {
                Id = 123,
                DisplayName = "Field Name",
                Reference = "field.ref"
            };
            var workTrackingSystemConnection = new WorkTrackingSystemConnection();
            workTrackingSystemConnection.AdditionalFieldDefinitions.Add(field);

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.AdditionalFieldDefinitions[0].Id, Is.EqualTo(123));
        }

        [Test]
        public void Create_RequiresReconnect_DefaultsToFalse()
        {
            var workTrackingSystemConnection = new WorkTrackingSystemConnection
            {
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud
            };

            var subject = new WorkTrackingSystemConnectionDto(workTrackingSystemConnection);

            Assert.That(subject.RequiresReconnect, Is.False);
        }

        private static readonly JsonSerializerOptions AsTheWireSendsIt = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

        public enum WriteBack { None, Mapped }

        // Every way a connection can stand towards write consent: which sign-in it uses, what that sign-in
        // was granted (null = signed in before rights were recorded, or no sign-in at all), and whether
        // write-back is set up. Rights are shown only for an Azure DevOps or Jira OAuth sign-in, and a warning only
        // when write-back is set up on a sign-in that may only read.
        private static IEnumerable<TestCaseData> WriteConsentCases()
        {
            yield return new TestCaseData(AuthenticationMethodKeys.AzureDevOpsOAuth, true, null, WriteBack.Mapped, "ReadAndWrite", false)
                .SetName("An Azure DevOps sign-in from before rights were recorded reads and writes and needs no more consent");
            yield return new TestCaseData(AuthenticationMethodKeys.AzureDevOpsOAuth, true, WorkItemRights.Read, WriteBack.Mapped, "Read", true)
                .SetName("An Azure DevOps sign-in that may only read needs write consent once write-back is set up");
            yield return new TestCaseData(AuthenticationMethodKeys.AzureDevOpsOAuth, true, WorkItemRights.Read, WriteBack.None, "Read", false)
                .SetName("An Azure DevOps sign-in that may only read needs nothing more without write-back");
            yield return new TestCaseData(AuthenticationMethodKeys.AzureDevOpsOAuth, true, WorkItemRights.ReadAndWrite, WriteBack.Mapped, "ReadAndWrite", false)
                .SetName("An Azure DevOps sign-in granted write rights needs nothing more with write-back");
            yield return new TestCaseData(AuthenticationMethodKeys.JiraOAuth, true, null, WriteBack.Mapped, "Read", true)
                .SetName("A Jira sign-in from before rights were recorded counts as read only and needs write consent once write-back is set up");
            yield return new TestCaseData(AuthenticationMethodKeys.JiraOAuth, true, WorkItemRights.Read, WriteBack.None, "Read", false)
                .SetName("A Jira sign-in that may only read needs nothing more without write-back");
            yield return new TestCaseData(AuthenticationMethodKeys.JiraOAuth, true, WorkItemRights.ReadAndWrite, WriteBack.Mapped, "ReadAndWrite", false)
                .SetName("A Jira sign-in granted write rights needs nothing more with write-back");
            yield return new TestCaseData(AuthenticationMethodKeys.JiraOAuth, false, null, WriteBack.Mapped, null, false)
                .SetName("A Jira OAuth connection not signed in yet shows no rights and needs no write consent");
            yield return new TestCaseData(AuthenticationMethodKeys.AzureDevOpsPat, false, null, WriteBack.Mapped, null, false)
                .SetName("A personal access token connection shows no rights and needs no write consent");
        }

        [TestCaseSource(nameof(WriteConsentCases))]
        public void TheConnectionListSaysWhichRightsTheSignInHoldsAndWhetherWriteBackNeedsMore(
            string authenticationMethodKey,
            bool hasSignedIn,
            WorkItemRights? grantedAtSignIn,
            WriteBack writeBack,
            string? expectedRights,
            bool expectedNeedsWriteConsent)
        {
            var connection = AConnection(authenticationMethodKey, writeBack);
            var credential = hasSignedIn ? new OAuthCredential { WorkTrackingSystemConnectionId = 7, GrantedRights = grantedAtSignIn } : null;

            var wire = WhatTheListSends(connection, credential);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wire["grantedRights"]?.GetValue<string>(), Is.EqualTo(expectedRights),
                    "The connection list must name the rights the sign-in holds as Read or ReadAndWrite for an Azure DevOps or Jira OAuth sign-in, and nothing for any other connection, " +
                    "because the settings page shows a Rights line from it. Set grantedRights from the stored credential's write consent.");
                Assert.That(wire["needsWriteConsent"]?.GetValue<bool>(), Is.EqualTo(expectedNeedsWriteConsent),
                    "The connection list must say needsWriteConsent (true or false, never missing) because the warning with Sign in again shows from it. " +
                    "Set it to whether the credential lacks the rights the connection's write-back needs.");
            }
        }

        [Test]
        public void ASavedConnectionCannotClaimRightsItsSignInWasNotGranted()
        {
            const string savedBody = """
                {"id":7,"name":"Fabrikam","workTrackingSystem":"AzureDevOps","authenticationMethodKey":"ado.oauth","grantedRights":"ReadAndWrite","needsWriteConsent":true}
                """;

            var saved = JsonSerializer.Deserialize<WorkTrackingSystemConnectionDto>(savedBody, AsTheWireSendsIt)!;
            var wire = JsonNode.Parse(JsonSerializer.Serialize(saved, AsTheWireSendsIt))!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(wire["grantedRights"]?.GetValue<string>(), Is.Null,
                    "A body sent to save a connection must not set the rights its sign-in holds, because only the server knows what was granted. Keep the grantedRights setter private.");
                Assert.That(wire["needsWriteConsent"]?.GetValue<bool>(), Is.False,
                    "A body sent to save a connection must not set needsWriteConsent, because the server works it out. Keep its setter private so it stays false and is still sent.");
            }
        }

        private static WorkTrackingSystemConnection AConnection(string authenticationMethodKey, WriteBack writeBack)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 7,
                Name = "Fabrikam",
                WorkTrackingSystem = authenticationMethodKey == AuthenticationMethodKeys.JiraOAuth ? WorkTrackingSystems.Jira : WorkTrackingSystems.AzureDevOps,
                AuthenticationMethodKey = authenticationMethodKey,
            };

            if (writeBack == WriteBack.Mapped)
            {
                connection.WriteBackMappingDefinitions.Add(new WriteBackMappingDefinition());
            }

            return connection;
        }

        private static JsonNode WhatTheListSends(WorkTrackingSystemConnection connection, OAuthCredential? credential)
            => JsonNode.Parse(JsonSerializer.Serialize(new WorkTrackingSystemConnectionDto(connection, credential), AsTheWireSendsIt))!;
    }
}
