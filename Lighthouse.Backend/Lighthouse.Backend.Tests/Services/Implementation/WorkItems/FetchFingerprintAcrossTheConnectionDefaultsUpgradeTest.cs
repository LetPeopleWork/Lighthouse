using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// The fingerprints Teams and Portfolios store today, for an owner that names its own fields and for one
    /// that names none, written out with every input spelled out. Once owners can follow defaults set on the
    /// connection, the fingerprint is worked out from what the owner actually reads. On the day of the upgrade
    /// no connection has a default yet, so an owner that names nothing still reads nothing, and every one of
    /// these digests must come out the same; if one moved, every such owner on every instance would download
    /// everything on its first update after the upgrade.
    ///
    /// The digests were re-pinned on purpose when the Jira reading-version marker was added to the
    /// connection's part of the fingerprint: these owners sit on a Jira connection, so each one takes one full
    /// refresh after that upgrade and keeps its new digest from then on.
    /// </summary>
    [TestFixture]
    [Category("epic-6303-configure-once-per-connection")]
    [Category("story-6304")]
    [Category("slice-05a")]
    [Category("ac-4.4")]
    [Category("active-guard")]
    [Category("contract-shape:pure-function")]
    public class FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest
    {
        private const string TheOwningTeamsStoredFingerprint = "154b09f400b4278eb953f8f7eb0d272c";

        private const string TheTeamNamingNothingsStoredFingerprint = "274b9fd5da57e1dd4b78eb37fe38c4ae";

        private const string TheOwningPortfoliosStoredFingerprint = "5675e74a9054426808923484dd1acd35";

        private const string ThePortfolioNamingNothingsStoredFingerprint = "e9416bc3bf27ec045dd03282526f553c";

        [Test]
        public void A_Team_that_names_its_own_Parent_Override_Field_keeps_its_fingerprint()
            => Assert.That(FetchFingerprint.For(ATeam(parentOverride: CausedBy)), Is.EqualTo(TheOwningTeamsStoredFingerprint));

        [Test]
        public void A_Team_that_names_no_Parent_Override_Field_keeps_its_fingerprint()
            => Assert.That(FetchFingerprint.For(ATeam(parentOverride: null)), Is.EqualTo(TheTeamNamingNothingsStoredFingerprint));

        [Test]
        public void A_Portfolio_that_names_its_own_fields_keeps_its_fingerprint()
            => Assert.That(FetchFingerprint.For(APortfolio(namesItsOwnFields: true)), Is.EqualTo(TheOwningPortfoliosStoredFingerprint));

        [Test]
        public void A_Portfolio_that_names_no_field_keeps_its_fingerprint()
            => Assert.That(FetchFingerprint.For(APortfolio(namesItsOwnFields: false)), Is.EqualTo(ThePortfolioNamingNothingsStoredFingerprint));

        private const int CausedBy = 11;

        private const int BlockedByKeys = 12;

        private const int TShirtSize = 13;

        private const int ProductOwner = 14;

        private static WorkTrackingSystemConnection AConnection()
        {
            var connection = new WorkTrackingSystemConnection
            {
                Id = 21,
                Name = "Jira DC – Payments",
                WorkTrackingSystem = WorkTrackingSystems.Jira,
                AuthenticationMethodKey = AuthenticationMethodKeys.JiraDataCenter,
            };

            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = CausedBy, DisplayName = "Caused by", Reference = "customfield_10500" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = BlockedByKeys, DisplayName = "Blocked By Keys", Reference = "customfield_10700" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = TShirtSize, DisplayName = "T-Shirt Size", Reference = "customfield_10800" });
            connection.AdditionalFieldDefinitions.Add(new AdditionalFieldDefinition { Id = ProductOwner, DisplayName = "Product Owner", Reference = "customfield_10900" });

            return connection;
        }

        // The upgrade leaves an owner that named a field owning it, and one that named none following the
        // connection's default, which no connection has set yet.
        private static TOwner AsTheUpgradeLeavesIt<TOwner>(TOwner owner, int? namedField)
            where TOwner : WorkTrackingSystemOptionsOwner
            => namedField is { } field ? owner.OwningParentOverride(field) : owner.FollowingParentOverrideDefault();

        private static Team ATeam(int? parentOverride)
            => AsTheUpgradeLeavesIt(new Team()
            {
                Id = 31,
                Name = "Fraud Signals",
                DataRetrievalValue = "project = FRAUD AND type in (Story, Bug)",
                WorkItemTypes = ["Story", "Bug"],
                DoneItemsCutoffDays = 180,
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                StateMappings = [],
                WorkTrackingSystemConnectionId = 21,
                WorkTrackingSystemConnection = AConnection(),
            }, parentOverride);

        private static Portfolio APortfolio(bool namesItsOwnFields)
        {
            var portfolio = APortfolioFollowingItsFieldDefaults(namesItsOwnFields);
            return namesItsOwnFields ? portfolio.OwningDependencyField(BlockedByKeys).OwningSizeEstimateField(TShirtSize).OwningFeatureOwnerField(ProductOwner) : portfolio;
        }

        private static Portfolio APortfolioFollowingItsFieldDefaults(bool namesItsOwnFields)
            => AsTheUpgradeLeavesIt(new Portfolio()
            {
                Id = 41,
                Name = "Card Issuing",
                DataRetrievalValue = "project = CARD AND type = Epic",
                WorkItemTypes = ["Epic"],
                DoneItemsCutoffDays = 365,
                ToDoStates = ["Funnel"],
                DoingStates = ["Implementing"],
                DoneStates = ["Done"],
                StateMappings = [],
                WorkTrackingSystemConnectionId = 21,
                WorkTrackingSystemConnection = AConnection(),
            }, namesItsOwnFields ? CausedBy : null);
    }
}
