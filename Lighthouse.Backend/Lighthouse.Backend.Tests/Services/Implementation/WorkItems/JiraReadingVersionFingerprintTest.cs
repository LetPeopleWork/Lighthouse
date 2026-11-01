using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkItems;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkItems
{
    /// <summary>
    /// Items a German-token Team stored before the upgrade carry German states, and a cheap refresh skips an
    /// item whose change stamp did not move. A reading-version marker in the Jira fingerprint gives every
    /// Jira owner exactly one full refresh after the upgrade, and leaves every other tracker's fingerprint
    /// alone so nobody else pays for it.
    ///
    /// The digests pinned for Jira owners in <c>FetchFingerprintPinnedDigestTest</c> and
    /// <c>FetchFingerprintAcrossTheConnectionDefaultsUpgradeTest</c> are left as they are here on purpose:
    /// adding the marker changes them, and they are re-pinned in the same change that adds it. The same
    /// change pins the Jira digest that results from the marker to an exact value here, so a later change of
    /// the marker is a visible decision. The digests below for every other tracker must not move at all.
    /// </summary>
    [TestFixture]
    public class JiraReadingVersionFingerprintTest
    {
        private static readonly Dictionary<WorkTrackingSystems, (string Team, string Portfolio)> TheDigestsBeforeTheUpgrade = new()
        {
            [WorkTrackingSystems.AzureDevOps] = ("f48d8898f4bb88689e49e32cf2157900", "76e776854e9b34fd3161e7fc20a67610"),
            [WorkTrackingSystems.Linear] = ("84866d55cd6efc072cafa3778f552239", "d598704e4cb277f4c2130ce7019d2cae"),
            [WorkTrackingSystems.Csv] = ("fa04b1f34de09f03ffc3e6b499c96c9b", "b3ab6134323d977db36b76e1cf86c1cb"),
            [WorkTrackingSystems.ServiceNow] = ("120a72199f268e6cc33a517ef1836092", "22ad2181303e509bf52ed10e725637c5"),
        };

        private const string AJiraTeamsFingerprintBeforeTheUpgrade = "ded783898abaa53fb1df75c6ab05ff64";

        private const string AJiraPortfoliosFingerprintBeforeTheUpgrade = "091b24376c85e4ad5afed8a066ab11dd";

        private const string AJiraTeamsFingerprintWithTheReadingVersion = "550c254f514ed280b0950bf6930cfc58";

        private const string AJiraPortfoliosFingerprintWithTheReadingVersion = "63438e52e318e6c8a26f9946f1d2975e";

        public static IEnumerable<WorkTrackingSystems> EveryTrackerButJira
            => Enum.GetValues<WorkTrackingSystems>().Where(tracker => tracker != WorkTrackingSystems.Jira);

        [Test]
        public void A_Jira_Team_stored_before_the_upgrade_no_longer_matches_so_it_is_refreshed_in_full_once()
        {
            Assert.That(FetchFingerprint.For(ATeamOn(WorkTrackingSystems.Jira)), Is.Not.EqualTo(AJiraTeamsFingerprintBeforeTheUpgrade));
        }

        [Test]
        public void A_Jira_Portfolio_stored_before_the_upgrade_no_longer_matches_so_it_is_refreshed_in_full_once()
        {
            Assert.That(FetchFingerprint.For(APortfolioOn(WorkTrackingSystems.Jira)), Is.Not.EqualTo(AJiraPortfoliosFingerprintBeforeTheUpgrade));
        }

        [Test]
        public void A_Jira_Team_carries_exactly_the_fingerprint_the_reading_version_gives_it()
        {
            Assert.That(FetchFingerprint.For(ATeamOn(WorkTrackingSystems.Jira)), Is.EqualTo(AJiraTeamsFingerprintWithTheReadingVersion));
        }

        [Test]
        public void A_Jira_Portfolio_carries_exactly_the_fingerprint_the_reading_version_gives_it()
        {
            Assert.That(FetchFingerprint.For(APortfolioOn(WorkTrackingSystems.Jira)), Is.EqualTo(AJiraPortfoliosFingerprintWithTheReadingVersion));
        }

        [Test]
        public void Every_tracker_but_Jira_has_its_stored_digests_pinned()
        {
            Assert.That(TheDigestsBeforeTheUpgrade.Keys, Is.EquivalentTo(EveryTrackerButJira),
                "A tracker added later must get its digests pinned here, or it could join the Jira branch unnoticed.");
        }

        [TestCaseSource(nameof(EveryTrackerButJira))]
        public void A_Team_on_any_other_tracker_keeps_its_stored_fingerprint_byte_for_byte(WorkTrackingSystems tracker)
        {
            Assert.That(FetchFingerprint.For(ATeamOn(tracker)), Is.EqualTo(TheDigestsBeforeTheUpgrade[tracker].Team));
        }

        [TestCaseSource(nameof(EveryTrackerButJira))]
        public void A_Portfolio_on_any_other_tracker_keeps_its_stored_fingerprint_byte_for_byte(WorkTrackingSystems tracker)
        {
            Assert.That(FetchFingerprint.For(APortfolioOn(tracker)), Is.EqualTo(TheDigestsBeforeTheUpgrade[tracker].Portfolio));
        }

        [Test]
        public void A_Jira_Teams_fingerprint_is_the_same_on_every_call()
        {
            var first = FetchFingerprint.For(ATeamOn(WorkTrackingSystems.Jira));
            var second = FetchFingerprint.For(ATeamOn(WorkTrackingSystems.Jira));

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void An_owner_with_no_connection_still_has_a_fingerprint_and_it_is_stable()
        {
            var first = ATeamOn(WorkTrackingSystems.Jira);
            first.WorkTrackingSystemConnection = null!;
            var second = ATeamOn(WorkTrackingSystems.Jira);
            second.WorkTrackingSystemConnection = null!;

            var fingerprint = FetchFingerprint.For(first);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fingerprint, Is.Not.Empty);
                Assert.That(FetchFingerprint.For(second), Is.EqualTo(fingerprint));
            }
        }

        private static Team ATeamOn(WorkTrackingSystems tracker)
            => new()
            {
                Id = 31,
                Name = "Fraud Signals",
                DataRetrievalValue = "project = FRAUD",
                WorkItemTypes = ["Story"],
                ToDoStates = ["To Do"],
                DoingStates = ["In Progress"],
                DoneStates = ["Done"],
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnectionTo(tracker),
            };

        private static Portfolio APortfolioOn(WorkTrackingSystems tracker)
            => new()
            {
                Id = 32,
                Name = "Fleet Platform",
                DataRetrievalValue = "project = FLEET",
                WorkItemTypes = ["Epic"],
                ToDoStates = ["Funnel"],
                DoingStates = ["Implementing"],
                DoneStates = ["Done"],
                WorkTrackingSystemConnectionId = 7,
                WorkTrackingSystemConnection = AConnectionTo(tracker),
            };

        private static WorkTrackingSystemConnection AConnectionTo(WorkTrackingSystems tracker)
            => new()
            {
                Id = 7,
                Name = "Tracker",
                WorkTrackingSystem = tracker,
                AuthenticationMethodKey = tracker switch
                {
                    WorkTrackingSystems.Jira => AuthenticationMethodKeys.JiraCloud,
                    WorkTrackingSystems.AzureDevOps => AuthenticationMethodKeys.AzureDevOpsPat,
                    WorkTrackingSystems.ServiceNow => AuthenticationMethodKeys.ServiceNowBasic,
                    WorkTrackingSystems.Csv => AuthenticationMethodKeys.None,
                    _ => AuthenticationMethodKeys.LinearApiKey,
                },
            };
    }
}
