using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// The fingerprint every Team and Portfolio has already stored, written out for one Team, one
    /// Portfolio, and that Portfolio read without its connection, with every input spelled out. The
    /// fingerprint decides whether the next update downloads everything, so if a change to how it is
    /// worked out moved these digests, every Team and Portfolio on every instance would download
    /// everything on the first update after the upgrade. Taking the connection's part of the fingerprint
    /// out into a function of its own must leave all three unchanged.
    ///
    /// The Jira digests were re-pinned on purpose when the Jira reading-version marker was added to the
    /// connection's part of the fingerprint: every Jira Team and Portfolio takes one full refresh after that
    /// upgrade. The Team and the Portfolio here sit on a Jira connection, so their digests moved; the digest of
    /// a Portfolio read without its connection did not.
    /// </summary>
    [TestFixture]
    [Category("story-6307-refresh-on-additional-fields-change")]
    [Category("contract-shape:pure-function")]
    public class FetchFingerprintPinnedDigestTest
    {
        private const string TheTeamsStoredFingerprint = "178f7c190469f76ef4423c1e680ea2d6";

        private const string ThePortfoliosStoredFingerprint = "6d06bf5c2f9c46b0e80f5a077e4e1898";

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
            => new Team()
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
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnection(),
            }.OwningParentOverride(1);

        private static Portfolio APortfolio()
            => new Portfolio()
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
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnection(),
            }.OwningParentOverride(null).OwningDependencyField(null).OwningSizeEstimateField(1).OwningFeatureOwnerField(1);
    }
}
