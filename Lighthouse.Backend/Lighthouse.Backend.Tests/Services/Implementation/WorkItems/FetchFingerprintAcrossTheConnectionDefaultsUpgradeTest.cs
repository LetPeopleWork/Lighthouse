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
        private const string TheOwningTeamsStoredFingerprint = "23f7d6888b4dac84ff45d4742760cef4";

        private const string TheTeamNamingNothingsStoredFingerprint = "6a24ccb89d0884fb806e48d6554e6b12";

        private const string TheOwningPortfoliosStoredFingerprint = "71bb307dee02d10c33214b4f1d7b7b82";

        private const string ThePortfolioNamingNothingsStoredFingerprint = "aaf2516f22a47f6c7c8889483bfb2e23";

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

        private static Team ATeam(int? parentOverride)
            => new Team()
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
            }.OwningParentOverride(parentOverride);

        private static Portfolio APortfolio(bool namesItsOwnFields)
            => new Portfolio()
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
                DependencyOverrideAdditionalFieldDefinitionId = namesItsOwnFields ? BlockedByKeys : null,
                SizeEstimateAdditionalFieldDefinitionId = namesItsOwnFields ? TShirtSize : null,
                FeatureOwnerAdditionalFieldDefinitionId = namesItsOwnFields ? ProductOwner : null,
                WorkTrackingSystemConnectionId = 21,
                WorkTrackingSystemConnection = AConnection(),
            }.OwningParentOverride(namesItsOwnFields ? CausedBy : null);
    }
}
