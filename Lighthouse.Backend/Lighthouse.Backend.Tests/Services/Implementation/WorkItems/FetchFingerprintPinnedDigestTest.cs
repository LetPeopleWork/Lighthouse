using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// The fingerprint every Team and Portfolio has already stored, written out for one Team, one
    /// Portfolio, and that Portfolio read without its connection, with every input spelled out. The
    /// fingerprint decides whether the next update downloads everything, so if a change to how it is
    /// worked out moved these digests, every Team and Portfolio on every instance would download
    /// everything on the first update after the upgrade. Taking the connection's part of the fingerprint
    /// out into a function of its own must leave all three unchanged.
    /// </summary>
    [TestFixture]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("contract-shape:pure-function")]
    public class FetchFingerprintPinnedDigestTest
    {
        private const string TheTeamsStoredFingerprint = "31fea2f9b446425c48714adde896e11d";

        private const string ThePortfoliosStoredFingerprint = "427b6dece3da51911144616de4cc404f";

        private const string ThePortfolioWithoutItsConnectionsStoredFingerprint = "9a5ae7627477a3dbe4f80c4204789044";

        [Test]
        public void A_Team_keeps_the_fingerprint_it_has_already_stored()
            => Assert.That(FetchFingerprint.For(ATeam()), Is.EqualTo(TheTeamsStoredFingerprint));

        [Test]
        public void A_Portfolio_keeps_the_fingerprint_it_has_already_stored()
            => Assert.That(FetchFingerprint.For(APortfolio()), Is.EqualTo(ThePortfoliosStoredFingerprint));

        [Test]
        public void A_Portfolio_read_without_its_connection_keeps_the_fingerprint_it_has_already_stored()
        {
            var portfolio = APortfolio();
            portfolio.WorkTrackingSystemConnection = null!;

            Assert.That(FetchFingerprint.For(portfolio), Is.EqualTo(ThePortfolioWithoutItsConnectionsStoredFingerprint));
        }

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 7,
                Name = "Nordwind Jira DC",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraDataCenter,
            };

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 1, DisplayName = "Story Points", Reference = "customfield_10016" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = 2, DisplayName = "Releases", Reference = "fixVersions", IsPredefined = true });

            return connection;
        }

        private static Team ATeam()
            => new()
            {
                Id = 4,
                Name = "Atlas",
                DataRetrievalValue = "project = ATLAS AND type in (Story, Bug)",
                WorkItemTypes = ["Story", "Bug"],
                DoneItemsCutoffDays = 180,
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress", "In Review"],
                DoneStates = ["Done"],
                StateMappings = [new StateMapping { Name = "In Progress", States = ["In Progress", "In Review"] }],
                ParentOverrideAdditionalFieldDefinitionId = 1,
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnection(),
            };

        private static Portfolio APortfolio()
            => new()
            {
                Id = 3,
                Name = "Fleet Platform",
                DataRetrievalValue = "project = FLEET AND type = Epic",
                WorkItemTypes = ["Epic"],
                DoneItemsCutoffDays = 365,
                ToDoStates = ["Funnel"],
                DoingStates = ["Implementing"],
                DoneStates = ["Done"],
                StateMappings = [],
                ParentOverrideAdditionalFieldDefinitionId = null,
                FeatureOwnerAdditionalFieldDefinitionId = 1,
                SizeEstimateAdditionalFieldDefinitionId = 1,
                DependencyOverrideAdditionalFieldDefinitionId = null,
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnection(),
            };
    }
}
