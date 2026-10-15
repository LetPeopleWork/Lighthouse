using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lighthouse.Backend.Tests.Factories
{
    /// <summary>
    /// When each link an issue still has was made, read from the history Jira sends with it.
    ///
    /// The history entries are written the way Jira Cloud was seen to write them when a link was added,
    /// removed and added again, stamped in the reading account's zone rather than in UTC. The one entry
    /// that names its counterpart only in the sentence is a shape Cloud did not write, kept for other
    /// Jira versions.
    /// </summary>
    [TestFixture]
    [Category("story-6301-only-features-as-parent-candidates")]
    [Category("slice-02")]
    public class IssueFactoryLinkDatesTest
    {
        private const string TheItem = "PAY-420";

        private const string AFeature = "PAY-88";

        private const string AnotherFeature = "PAY-91";

        private static readonly DateTime February1 = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime April10 = new(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime May1 = new(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime July1 = new(2026, 7, 1, 9, 0, 0, DateTimeKind.Utc);

        [Test]
        public void An_added_link_dates_its_counterpart_by_when_it_was_added()
        {
            var issue = AnIssueWithTheHistory(JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.LinkAddedAt, Is.EqualTo(new Dictionary<string, DateTime> { [AFeature] = February1 }),
                    "The moment the link was made is the one fact the oldest-link rule needs, and the history is where Jira keeps it.");
                Assert.That(issue.LinkAddedAt[AFeature].Kind, Is.EqualTo(DateTimeKind.Utc),
                    "The history is stamped in the reading account's zone, and a date that is not kept as UTC turns into a different calendar day wherever the server runs.");
            }
        }

        [Test]
        [Category("edge")]
        public void A_removed_link_leaves_its_counterpart_undated()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AnotherFeature, Sentence(AnotherFeature)),
                JiraWireFormat.ALinkRemovedAt(May1, AFeature, Sentence(AFeature)));

            Assert.That(issue.LinkAddedAt.Keys, Is.EquivalentTo(OnlyTheOtherFeature),
                "A removed link no longer exists, so the date it was made says nothing about any link that stands.");
        }

        [Test]
        [Category("edge")]
        public void A_link_added_again_dates_from_when_it_was_added_again()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkRemovedAt(May1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(July1, AFeature, Sentence(AFeature)));

            Assert.That(issue.LinkAddedAt, Does.ContainKey(AFeature).WithValue(July1),
                "A re-added link counts from when it was re-added, because the earlier date belongs to a link that no longer exists.");
        }

        [Test]
        [Category("edge")]
        public void Entries_out_of_order_are_read_by_when_they_happened()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedAt(July1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkRemovedAt(May1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)));

            Assert.That(issue.LinkAddedAt, Does.ContainKey(AFeature).WithValue(July1),
                "Cloud and Data Center do not promise an order for a history. Read in the order given, this link would date from February and a removal would seem to end it.");
        }

        [Test]
        [Category("edge")]
        public void A_key_named_only_in_the_sentence_is_read_from_the_sentence()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedNamingTheCounterpartOnlyInItsSentence(April10, "10443", Sentence(AnotherFeature)));

            Assert.That(issue.LinkAddedAt, Is.EqualTo(new Dictionary<string, DateTime> { [AnotherFeature] = April10 }),
                "Where the entry carries a link id instead of a key, the sentence is the only place the counterpart is named. The key is read out of it, never the renamable phrase around it.");
        }

        [Test]
        [Category("edge")]
        public void A_removal_whose_adding_was_never_recorded_changes_nothing()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkRemovedAt(February1, AnotherFeature, Sentence(AnotherFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AFeature, Sentence(AFeature)));

            Assert.That(issue.LinkAddedAt, Is.EqualTo(new Dictionary<string, DateTime> { [AFeature] = April10 }),
                "A history that starts part way through is ordinary for an imported issue, and it must not cost the links it does date.");
        }

        /// <summary>Holds today and has to keep holding: link entries sit beside status changes in the same history.</summary>
        [Test]
        [Category("edge")]
        public void Status_changes_beside_link_entries_are_read_as_before()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.AStatusChangeAt(April10, "To Do", "In Progress"));

            Assert.That(issue.StateTransitions, Has.Count.EqualTo(1),
                "Reading links out of the history must not disturb the status changes every cycle-time figure is built from.");
        }

        private static readonly string[] OnlyTheOtherFeature = [AnotherFeature];

        private static string Sentence(string counterpart) => $"This issue is caused by {counterpart}";

        private static Issue AnIssueWithTheHistory(params string[] histories)
        {
            var json = JiraWireFormat.AnIssueWithItsHistory(
                TheItem,
                "Story",
                JiraWireFormat.AnInstant(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                JiraWireFormat.AnInstant(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
                string.Empty,
                Array.Empty<string>(),
                histories);

            using var document = JsonDocument.Parse(json);

            return new IssueFactory(Mock.Of<ILogger<IssueFactory>>())
                .CreateIssueFromJson(document.RootElement, JiraConnectorTestSetup.ATeamOnJiraCloud());
        }
    }
}
