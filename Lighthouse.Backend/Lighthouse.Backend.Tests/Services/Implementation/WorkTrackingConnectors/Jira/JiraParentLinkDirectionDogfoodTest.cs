using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.DeliverySources;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// A hierarchy drawn with one link type at every level, read from the real demo Jira. Three issues
    /// hang in a chain by the Causes type: a child "is caused by" its parent and the parent "causes" its
    /// children, so the issue in the middle holds both ends of that one type - the link up to its parent
    /// and the links down to its children.
    ///
    /// A real instance is what settles it, because the whole question is which end of each link Jira
    /// hands to which issue. A payload written here would be written by the very assumption under test.
    ///
    /// The top of the chain is left out of the query on purpose: the middle issue has to find its
    /// parent on its own links, not on something fetched alongside it.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("JiraIntegration")]
    public class JiraParentLinkDirectionDogfoodTest
    {
        private const string OrganizationUrl = "https://letpeoplework.atlassian.net";

        private const string TokenEnvironmentVariable = "JiraLighthouseIntegrationTestToken";
        private const string UsernameEnvironmentVariable = "JiraLighthouseIntegrationTestUsername";
        private const string DefaultUsername = "atlassian.pushchair@huser-berta.com";

        private const string ThePhraseAChildReadsTowardsItsParent = "is caused by";

        private const string TheLinkTypesName = "Causes";

        private const string TheTopOfTheChain = "LGHTHSDMO-8430";

        private const string TheMiddleOfTheChain = "LGHTHSDMO-8431";

        private const string ALeaf = "LGHTHSDMO-8432";

        private const string AnotherLeaf = "LGHTHSDMO-8433";

        private const string TheChainWithItsTopLeftOut =
            $"key in ({TheMiddleOfTheChain}, {ALeaf}, {AnotherLeaf})";

        private string? apiToken;

        private Dictionary<string, string> readByThePhrase = [];

        private Dictionary<string, string> readByTheName = [];

        [OneTimeSetUp]
        public async Task AskTheInstanceWhatTheChainHangsUnder()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);

            if (string.IsNullOrEmpty(apiToken))
            {
                Assert.Ignore($"{TokenEnvironmentVariable} is not set - there is no instance to ask.");
            }

            readByThePhrase = await TheParentOfEachItemWhenTheOverrideNames(ThePhraseAChildReadsTowardsItsParent);
            readByTheName = await TheParentOfEachItemWhenTheOverrideNames(TheLinkTypesName);
        }

        [Test]
        public void GetWorkItemsForTeam_ThePhraseAChildReadsTowardsItsParent_PlacesTheMiddleOfTheChain()
        {
            Assert.That(readByThePhrase[TheMiddleOfTheChain], Is.EqualTo(TheTopOfTheChain),
                "The middle issue holds the link up to its parent and the links down to its children. The "
                + "phrase that was typed is what says which of them leads up, and ignoring it leaves every "
                + "middle level of the hierarchy with no parent.");
        }

        [Test]
        public void GetWorkItemsForTeam_ThePhraseAChildReadsTowardsItsParent_KeepsTheLeavesUnderTheMiddle()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readByThePhrase[ALeaf], Is.EqualTo(TheMiddleOfTheChain));
                Assert.That(readByThePhrase[AnotherLeaf], Is.EqualTo(TheMiddleOfTheChain));
            }
        }

        [Test]
        public void GetWorkItemsForTeam_TheLinkTypesName_StillReadsBothWaysAndRefusesTheMiddle()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(readByTheName[TheMiddleOfTheChain], Is.Empty,
                    "A type's name points at neither end, so the middle issue has a parent and children to choose "
                    + "between - and the type is named the same word as its outward phrase, which must still count "
                    + "as the name.");
                Assert.That(readByTheName[ALeaf], Is.EqualTo(TheMiddleOfTheChain),
                    "A leaf holds only the link up to its parent. Were the name resolving to nothing at all, the "
                    + "middle issue would come back empty for the wrong reason, and this is what tells the two apart.");
            }
        }

        private async Task<Dictionary<string, string>> TheParentOfEachItemWhenTheOverrideNames(string reference)
        {
            var team = ATeamReading(TheChainWithItsTopLeftOut);

            team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Parent",
                Reference = reference,
            });

            team.ParentOverrideAdditionalFieldDefinitionId = 1;

            var workItems = await CreateSubject().GetWorkItemsForTeam(team, CancellationToken.None);

            return workItems.ToDictionary(workItem => workItem.ReferenceId, workItem => workItem.ParentReferenceId);
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

        /// <summary>
        /// Issue types, states and the done-item cutoff are all switched off, so the keys in the query are
        /// the only thing deciding what comes back - a test that also pinned the demo project's states
        /// would go red the day somebody renames one.
        /// </summary>
        private Team ATeamReading(string query)
        {
            var team = new Team
            {
                Name = "Parent Link Direction Dogfood",
                DataRetrievalValue = query,
                DoneItemsCutoffDays = 0,
                WorkTrackingSystemConnection = AConnectionToTheDemoInstance(),
            };

            team.WorkItemTypes.Clear();
            team.ToDoStates.Clear();
            team.DoingStates.Clear();
            team.DoneStates.Clear();

            return team;
        }

        private WorkTrackingSystemConnection AConnectionToTheDemoInstance()
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

            return connection;
        }
    }
}
