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
    /// Reading a parent off an issue's links against the real demo Jira, rather than against a payload
    /// written here. Every other test of this behaviour hands the connector a response somebody on this
    /// side composed, so all they can prove is that the code copes with the shape its author expected.
    /// This says the shape is what Jira sends.
    ///
    /// One claim can only be settled here. Jira stores a link once and offers it from both ends, so an
    /// item is expected to carry the link even when the issue at the other end is not part of the same
    /// fetch - and everything about this feature rests on that, because it is what makes one pass over
    /// the items a team's own query returns enough. A handwritten payload cannot test it: the payload
    /// would be written by the very assumption in question. So these items are fetched with the issue
    /// they all point at deliberately left out of the query, and it staying out is asserted.
    ///
    /// The other thing worth a network call is that one of these items already has a parent of Jira's
    /// own. A link type named in Parent Override Field has to beat it, not merely fill a gap, and only a
    /// real instance can offer a real populated parent field to beat.
    /// </summary>
    [TestFixture]
    [Category("Integration")]
    [Category("JiraIntegration")]
    public class JiraParentLinkDogfoodTest
    {
        private const string OrganizationUrl = "https://letpeoplework.atlassian.net";

        private const string TokenEnvironmentVariable = "JiraLighthouseIntegrationTestToken";
        private const string UsernameEnvironmentVariable = "JiraLighthouseIntegrationTestUsername";
        private const string DefaultUsername = "atlassian.pushchair@huser-berta.com";

        private const string TheLinkTypeTheOverrideNames = "Cloners";

        private const string TheIssueEveryChildPointsAt = "LGHTHSDMO-1716";

        private const string TheChildWhoseLinkCarriesAnInwardIssue = "LGHTHSDMO-24";

        private const string AChildWhoseLinkCarriesAnOutwardIssue = "LGHTHSDMO-1725";

        private const string AnotherChildWhoseLinkCarriesAnOutwardIssue = "LGHTHSDMO-1726";

        private const string TheChildrenWithTheirParentLeftOut =
            "key in (LGHTHSDMO-24, LGHTHSDMO-1725, LGHTHSDMO-1726)";

        private const string TheIssueEveryChildPointsAtOnItsOwn = "key in (LGHTHSDMO-1716)";

        private static readonly string[] OnlyTheChildren =
        [
            TheChildWhoseLinkCarriesAnInwardIssue,
            AChildWhoseLinkCarriesAnOutwardIssue,
            AnotherChildWhoseLinkCarriesAnOutwardIssue,
        ];

        private string? apiToken;

        private Dictionary<string, string> theParentOfEachChild = [];

        private Dictionary<string, string> theParentOfTheIssueTheyAllPointAt = [];

        private Dictionary<string, string> theParentJiraReportsForTheChildrenItself = [];

        [OneTimeSetUp]
        public async Task AskTheInstanceWhatTheseItemsHangUnder()
        {
            apiToken = Environment.GetEnvironmentVariable(TokenEnvironmentVariable);

            if (string.IsNullOrEmpty(apiToken))
            {
                Assert.Ignore($"{TokenEnvironmentVariable} is not set - there is no instance to ask.");
            }

            theParentOfEachChild = await AsATeamReadingParentsFromTheLinkTypeSees(TheChildrenWithTheirParentLeftOut);
            theParentOfTheIssueTheyAllPointAt = await AsATeamReadingParentsFromTheLinkTypeSees(TheIssueEveryChildPointsAtOnItsOwn);
            theParentJiraReportsForTheChildrenItself = await AsATeamWithNoParentOverrideSees(TheChildrenWithTheirParentLeftOut);
        }

        [Test]
        public void GetWorkItemsForTeam_AnItemWhoseLinkCarriesAnInwardIssue_HangsUnderTheCounterpart()
        {
            Assert.That(theParentOfEachChild[TheChildWhoseLinkCarriesAnInwardIssue], Is.EqualTo(TheIssueEveryChildPointsAt));
        }

        [Test]
        public void GetWorkItemsForTeam_AnItemWhoseLinkCarriesAnOutwardIssue_HangsUnderTheCounterpart()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theParentOfEachChild[AChildWhoseLinkCarriesAnOutwardIssue], Is.EqualTo(TheIssueEveryChildPointsAt));
                Assert.That(theParentOfEachChild[AnotherChildWhoseLinkCarriesAnOutwardIssue], Is.EqualTo(TheIssueEveryChildPointsAt));
            }
        }

        [Test]
        public void GetWorkItemsForTeam_AnItemThatAlreadyHasAParentOfJirasOwn_HangsUnderTheLinkedOneInstead()
        {
            var whatJiraReportsForItself = theParentJiraReportsForTheChildrenItself[TheChildWhoseLinkCarriesAnInwardIssue];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(whatJiraReportsForItself, Is.Not.Empty,
                    "This item is in the fixture because it already hangs under something in Jira. If that parent "
                    + "were ever cleared on the instance, the test below would still pass and would no longer be "
                    + "saying anything: filling an empty parent is not the same as overruling a populated one.");
                Assert.That(whatJiraReportsForItself, Is.Not.EqualTo(TheIssueEveryChildPointsAt),
                    "The two have to disagree for the override to be observable at all.");
                Assert.That(theParentOfEachChild[TheChildWhoseLinkCarriesAnInwardIssue], Is.EqualTo(TheIssueEveryChildPointsAt),
                    "Naming a link type in Parent Override Field declares it authoritative, so the parent Jira "
                    + "reports for itself is not consulted while it is set.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_TheIssueTheChildrenPointAtIsNotInTheQuery_AndTheyCarryTheLinkAnyway()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theParentOfEachChild.Keys, Is.EquivalentTo(OnlyTheChildren),
                    "The issue they all point at must stay out of the result set, or they could be reading it "
                    + "from something that was fetched alongside them rather than from their own payload.");
                Assert.That(theParentOfEachChild.Values, Is.All.EqualTo(TheIssueEveryChildPointsAt),
                    "Jira writes a link once and hands it to both ends, so an item fetched without its "
                    + "counterpart still carries it. A refresh only ever walks the items a team's own query "
                    + "returns, and if this were false every parent outside that query would silently go missing.");
            }
        }

        [Test]
        public void GetWorkItemsForTeam_AnItemLinkedToSeveralCounterparts_HangsUnderNoneOfThem()
        {
            Assert.That(theParentOfTheIssueTheyAllPointAt[TheIssueEveryChildPointsAt], Is.Empty,
                "Several issues answer the same link type here and there is nothing to choose between them. "
                + "Keeping one would move work under something it does not belong to and look like correct data.");
        }

        private async Task<Dictionary<string, string>> AsATeamReadingParentsFromTheLinkTypeSees(string query)
        {
            var team = ATeamReading(query);

            team.WorkTrackingSystemConnection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition
            {
                Id = 1,
                DisplayName = "Parent",
                Reference = TheLinkTypeTheOverrideNames,
            });

            team.ParentOverrideAdditionalFieldDefinitionId = 1;

            return await TheParentOfEachItemReturnedFor(team);
        }

        private async Task<Dictionary<string, string>> AsATeamWithNoParentOverrideSees(string query)
        {
            return await TheParentOfEachItemReturnedFor(ATeamReading(query));
        }

        private static async Task<Dictionary<string, string>> TheParentOfEachItemReturnedFor(Team team)
        {
            var subject = CreateSubject();

            var workItems = await subject.GetWorkItemsForTeam(team, CancellationToken.None);

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
        /// Everything this instance could still be asked about - issue types, statuses, how far back a
        /// finished item counts - is left switched off, so the keys named above are the only thing deciding
        /// what comes back. A test that also pinned the demo project's statuses would go red the day an
        /// administrator renames one, and a test that cries wolf gets excluded from CI, after which it
        /// proves nothing at all.
        /// </summary>
        private Team ATeamReading(string query)
        {
            var team = new Team
            {
                Name = "Parent Link Dogfood",
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
