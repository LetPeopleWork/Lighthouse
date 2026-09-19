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

        private const string AReferenceTheInstanceCannotResolve = "Neither A Field Nor A Link Type";

        /// <summary>
        /// The sentence the connector ends its verdict with when it did read a link type list and the
        /// reference matched none of it. Finding it is how these tests tell that case apart from the one
        /// where Jira returned nothing: both are reported under the same code, because both are a reference
        /// that could not be resolved, and only the empty one means the credential has stopped working.
        /// </summary>
        private const string TheSentenceCarryingTheInstancesOwnList = "The issue link types it does define are: ";

        private string? apiToken;

        private ConnectionValidationResult verdictOnAReferenceNamingNothing = null!;

        /// <summary>
        /// Read off the instance's own answer rather than written down here. An administrator can rename or
        /// remove any link type on this Jira without telling anyone on this side, so a test naming one would
        /// go red for an administrative change rather than for a code regression - and a test that cries wolf
        /// gets excluded from CI, after which it proves nothing at all.
        /// </summary>
        private string[] linkTypesThisInstanceDefines = [];

        [OneTimeSetUp]
        public async Task AskTheInstanceWhichLinkTypesItDefines()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);

            if (string.IsNullOrEmpty(apiToken))
            {
                Assert.Ignore($"{TokenEnvironmentVariable} is not set - there is no instance to ask.");
            }

            verdictOnAReferenceNamingNothing = await TheVerdictOnAConnectionAskingFor(AReferenceTheInstanceCannotResolve);
            linkTypesThisInstanceDefines = TheLinkTypeNamesCarriedBy(verdictOnAReferenceNamingNothing.Message);
        }

        [Test]
        public void ValidateConnection_AReferenceNamingNothing_FailsAndNamesTheLinkTypesThisInstanceDefines()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(verdictOnAReferenceNamingNothing.IsValid, Is.False,
                    "Nothing on this instance carries that name, so the connection cannot be reported as usable.");
                Assert.That(verdictOnAReferenceNamingNothing.Code, Is.EqualTo("additional_fields_invalid"),
                    "The reference was checked and found wanting. A verdict saying the link types could not be read "
                    + "would mean the check never happened.");
                Assert.That(verdictOnAReferenceNamingNothing.Message, Does.Contain(AReferenceTheInstanceCannotResolve),
                    "An administrator fixes this by reading which of their references is the bad one.");
                Assert.That(linkTypesThisInstanceDefines, Is.Not.Empty,
                    "The list this instance really returned has to reach the administrator, and its being non-empty "
                    + "is the only thing separating a working credential from one Jira answered anonymously with "
                    + $"200 and no link types at all. Jira said: {verdictOnAReferenceNamingNothing.Message}");
            }
        }

        [Test]
        public async Task ValidateConnection_AReferenceNamingALinkTypeTheInstanceReallyDefines_Validates()
        {
            Assume.That(linkTypesThisInstanceDefines, Is.Not.Empty,
                "This instance named no link type to ask about, which the other test reports as the failure it is.");

            var aLinkTypeTheInstanceDefines = linkTypesThisInstanceDefines[0];

            var verdict = await TheVerdictOnAConnectionAskingFor(aLinkTypeTheInstanceDefines);

            Assert.That(verdict.IsValid, Is.True,
                $"'{aLinkTypeTheInstanceDefines}' is a link type this Jira has just said it defines, so an Additional "
                + $"Field naming it must validate. Jira said: {verdict.Message}");
        }

        private static string[] TheLinkTypeNamesCarriedBy(string message)
        {
            var listStart = message.IndexOf(TheSentenceCarryingTheInstancesOwnList, StringComparison.Ordinal);

            if (listStart < 0)
            {
                return [];
            }

            var list = message[(listStart + TheSentenceCarryingTheInstancesOwnList.Length)..].TrimEnd('.');

            return list.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
