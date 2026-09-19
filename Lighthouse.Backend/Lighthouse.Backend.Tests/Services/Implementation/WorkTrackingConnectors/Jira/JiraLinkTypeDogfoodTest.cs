using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Validation;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Resolving an Additional Field's reference against the real demo Jira's issue link types, rather
    /// than against a payload written here. Every other test of this behaviour hands the connector a
    /// response somebody on this side composed, so all they can prove is that the code copes with the
    /// shape its author expected. This says the shape is what Jira sends.
    ///
    /// What makes that worth a network call is how Jira answers a credential it will not accept for this
    /// endpoint: not by refusing, but with 200 and an empty list. A rejected credential is therefore
    /// indistinguishable from an instance that genuinely defines no link types, and a test written
    /// against a handwritten payload would never meet the ambiguity. So the list coming back non-empty
    /// is the assertion these tests are built around, not a warm-up to them.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("JiraIntegration")]
    public class JiraLinkTypeDogfoodTest
    {
        private const string OrganizationUrl = "https://letpeoplework.atlassian.net";

        private const string TokenEnvironmentVariable = "JiraLighthouseIntegrationTestToken";
        private const string UsernameEnvironmentVariable = "JiraLighthouseIntegrationTestUsername";
        private const string DefaultUsername = "atlassian.pushchair@huser-berta.com";

        /// <summary>
        /// One type, not the whole list. An administrator can add or rename a link type on this instance
        /// without telling anyone here, so a test pinning the exact set would go red for a reason that is
        /// not a regression. That any link type at all comes back is the load-bearing part; naming one is
        /// what stops "comes back" from being satisfied by noise.
        /// </summary>
        private const string ALinkTypeTheInstanceDefines = "Blocks";

        private const string AReferenceTheInstanceCannotResolve = "Neither A Field Nor A Link Type";

        private string? apiToken;

        [SetUp]
        public void ReadTheCredential()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);

            if (string.IsNullOrEmpty(apiToken))
            {
                Assert.Ignore($"{TokenEnvironmentVariable} is not set - there is no instance to ask.");
            }
        }

        [Test]
        public async Task ValidateConnection_AReferenceNamingALinkTypeTheInstanceReallyDefines_Validates()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(ALinkTypeTheInstanceDefines);

            Assert.That(verdict.IsValid, Is.True,
                $"'{ALinkTypeTheInstanceDefines}' is a link type this Jira defines, so an Additional Field naming it "
                + $"must validate. Jira said: {verdict.Message}");
        }

        [Test]
        public async Task ValidateConnection_AReferenceNamingNothing_FailsAndNamesTheLinkTypesThisInstanceDefines()
        {
            var verdict = await TheVerdictOnAConnectionAskingFor(AReferenceTheInstanceCannotResolve);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdict.IsValid, Is.False,
                    "Nothing on this instance carries that name, so the connection cannot be reported as usable.");
                Assert.That(verdict.Code, Is.EqualTo("additional_fields_invalid"),
                    "The reference was checked and found wanting. A verdict saying the link types could not be read "
                    + "would mean the check never happened.");
                Assert.That(verdict.Message, Does.Contain(AReferenceTheInstanceCannotResolve),
                    "An administrator fixes this by reading which of their references is the bad one.");
                Assert.That(verdict.Message, Does.Contain(ALinkTypeTheInstanceDefines),
                    "The list this instance really returned has to reach the administrator, and its being non-empty "
                    + "is the only thing separating a working credential from one Jira answered anonymously with "
                    + "200 and no link types at all.");
            }
        }

        private async Task<ConnectionValidationResult> TheVerdictOnAConnectionAskingFor(string reference)
        {
            var subject = CreateSubject();

            return await subject.ValidateConnection(AConnectionWithAnAdditionalFieldFor(reference));
        }

        private static JiraWorkTrackingConnector CreateSubject()
        {
            return new JiraWorkTrackingConnector(
                new IssueFactory(Mock.Of<ILogger<IssueFactory>>()),
                Mock.Of<ILogger<JiraWorkTrackingConnector>>(),
                TestAuthStrategyFactory.CreateRealFactory(new FakeCryptoService()),
                new Lighthouse.Backend.Cache.Cache<string, object>(),
                new DeliveryForecastBlockRenderer());
        }

        private WorkTrackingSystemConnection AConnectionWithAnAdditionalFieldFor(string reference)
        {
            var username = Environment.GetEnvironmentVariable(UsernameEnvironmentVariable) ?? DefaultUsername;

            var connection = new WorkTrackingSystemConnection
            {
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                Name = "Demo Jira",
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraCloud,
            };

            connection.Options.AddRange([
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Url, Value = OrganizationUrl, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.Username, Value = username, IsSecret = false },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.ApiToken, Value = apiToken!, IsSecret = true },
                new WorkTrackingSystemConnectionOption { Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds, Value = "100", IsSecret = false },
            ]);

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                DisplayName = "Parent",
                Reference = reference,
            });

            return connection;
        }
    }
}
