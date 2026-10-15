using System.Text.Json;
using Lighthouse.Backend.Factories;
using Lighthouse.Backend.Models;
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
                Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, February1) }),
                    "The moment the link was made is the one fact the oldest-link rule needs, and the history is where Jira keeps it.");
                Assert.That(issue.StandingLinks[0].AddedAt.Kind, Is.EqualTo(DateTimeKind.Utc),
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

            Assert.That(issue.StandingLinks.Select(link => link.CounterpartKey), Is.EquivalentTo(OnlyTheOtherFeature),
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

            Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, July1) }),
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

            Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, July1) }),
                "Cloud and Data Center do not promise an order for a history. Read in the order given, this link would date from February and a removal would seem to end it.");
        }

        /// <summary>
        /// Removing a link and adding it back within one second stamps both entries at the same instant, and
        /// Cloud lists the newer first. The history id Cloud gives each entry only grows, so it tells which came
        /// first; read in the order listed, the re-added link would seem removed.
        /// </summary>
        [Test]
        [Category("edge")]
        public void Entries_at_the_same_instant_are_read_in_the_order_of_their_history_ids()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedAt(May1, AFeature, Sentence(AFeature), 30002),
                JiraWireFormat.ALinkRemovedAt(May1, AFeature, Sentence(AFeature), 30001),
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)));

            Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, May1) }),
                "The link was removed and added back, so it stands, dated from when it was added back.");
        }

        /// <summary>
        /// Data Center sends only part of a long history with a search, and says so in the changelog's total.
        /// A link dated from part of its history may have been removed and added back in the part that is
        /// missing, so a partial history dates nothing.
        /// </summary>
        [Test]
        [Category("edge")]
        public void A_history_that_holds_only_part_of_its_entries_dates_no_link()
        {
            var issue = AnIssueWithAPartOfItsHistory(
                5,
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AnotherFeature, Sentence(AnotherFeature)));

            Assert.That(issue.StandingLinks, Is.Empty,
                "Three of the five entries are missing, and any of them could have removed or re-added one of these links.");
        }

        [Test]
        [Category("edge")]
        public void A_history_that_holds_every_one_of_its_entries_dates_its_links()
        {
            var issue = AnIssueWithAPartOfItsHistory(
                2,
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AnotherFeature, Sentence(AnotherFeature)));

            Assert.That(issue.StandingLinks, Is.EquivalentTo(new[] { ACausedByLink(AFeature, February1), ACausedByLink(AnotherFeature, April10) }),
                "positive control: a history that says it is complete is read as it always was.");
        }

        [Test]
        [Category("edge")]
        public void A_key_named_only_in_the_sentence_is_read_from_the_sentence()
        {
            var issue = AnIssueWithTheLinksAndHistory(
                [Causes.LinkWhoseInwardIssueIs(AnotherFeature)],
                JiraWireFormat.ALinkAddedNamingTheCounterpartOnlyInItsSentence(April10, "10443", Sentence(AnotherFeature)));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AnotherFeature, April10) }),
                    "Where the entry carries a link id instead of a key, the sentence is the only place the counterpart is named. The key is read out of it, never the renamable phrase around it.");
                Assert.That(TheParentCandidatesOf(issue, LinkDirection.Both), Is.EqualTo(new[] { new ParentCandidate(AnotherFeature, April10) }),
                    "The parent link is dated from that entry too.");
            }
        }

        [Test]
        [Category("edge")]
        public void A_removal_whose_adding_was_never_recorded_changes_nothing()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkRemovedAt(February1, AnotherFeature, Sentence(AnotherFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AFeature, Sentence(AFeature)));

            Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, April10) }),
                "A history that starts part way through is ordinary for an imported issue, and it must not cost the links it does date.");
        }

        /// <summary>
        /// One edit can add a link and move the issue between statuses, and Jira writes both into a single
        /// history entry. Each is read from it, and the status change comes out exactly as it does from an
        /// entry that holds nothing else.
        /// </summary>
        [Test]
        [Category("edge")]
        public void A_status_change_in_the_same_entry_as_a_link_is_read_as_if_it_stood_alone()
        {
            var issue = AnIssueWithTheHistory(
                JiraWireFormat.ALinkAddedBesideAStatusChangeAt(April10, AFeature, Sentence(AFeature), "To Do", "In Progress"));
            var theSameStatusChangeAlone = AnIssueWithTheHistory(JiraWireFormat.AStatusChangeAt(April10, "To Do", "In Progress"))
                .StateTransitions.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(issue.StateTransitions, Has.Count.EqualTo(1),
                    "The link item in the entry is not a status change.");
                Assert.That(issue.StateTransitions[0].FromState, Is.EqualTo("To Do"));
                Assert.That(issue.StateTransitions[0].ToState, Is.EqualTo("In Progress"));
                Assert.That(issue.StateTransitions[0].TransitionedAt, Is.EqualTo(theSameStatusChangeAlone.TransitionedAt),
                    "Every cycle-time figure is built from these instants, so sharing an entry with a link must not move one.");
                Assert.That(issue.StandingLinks, Is.EqualTo(new[] { ACausedByLink(AFeature, April10) }),
                    "The link in the same entry is dated by it too.");
            }
        }

        /// <summary>
        /// An issue can be linked to the same Feature in more than one way. Only the history of the link the
        /// parent is read through dates that parent: a later "relates to" says nothing about when the item
        /// was hung under the Feature, and dating it from that would hand the pick to the wrong Feature.
        /// </summary>
        [Test]
        [Category("edge")]
        public void Another_kind_of_link_to_the_same_issue_added_later_does_not_re_date_the_parent_link()
        {
            var issue = AnIssueWithTheLinksAndHistory(
                [Causes.LinkWhoseInwardIssueIs(AFeature), Relates.LinkWhoseOutwardIssueIs(AFeature)],
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(July1, AFeature, RelatesSentence(AFeature)));

            Assert.That(TheParentCandidatesOf(issue, LinkDirection.Both), Is.EqualTo(new[] { new ParentCandidate(AFeature, February1) }),
                "The parent link was made in February. The July entry belongs to a link of another type.");
        }

        [Test]
        [Category("edge")]
        public void Another_kind_of_link_to_the_same_issue_added_and_removed_does_not_undate_the_parent_link()
        {
            var issue = AnIssueWithTheLinksAndHistory(
                [Causes.LinkWhoseInwardIssueIs(AFeature)],
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(April10, AFeature, RelatesSentence(AFeature)),
                JiraWireFormat.ALinkRemovedAt(May1, AFeature, RelatesSentence(AFeature)));

            Assert.That(TheParentCandidatesOf(issue, LinkDirection.Both), Is.EqualTo(new[] { new ParentCandidate(AFeature, February1) }),
                "Removing a link of another type leaves the parent link standing, and it keeps the date it was made on.");
        }

        [Test]
        [Category("edge")]
        public void The_other_end_of_the_same_link_type_to_the_same_issue_does_not_re_date_the_parent_link()
        {
            var issue = AnIssueWithTheLinksAndHistory(
                [Causes.LinkWhoseInwardIssueIs(AFeature), Causes.LinkWhoseOutwardIssueIs(AFeature)],
                JiraWireFormat.ALinkAddedAt(February1, AFeature, Sentence(AFeature)),
                JiraWireFormat.ALinkAddedAt(July1, AFeature, $"This issue causes {AFeature}"));

            Assert.That(TheParentCandidatesOf(issue, LinkDirection.Inward), Is.EqualTo(new[] { new ParentCandidate(AFeature, February1) }),
                "'causes' and 'is caused by' are two different links, so the one this item reads its parent through keeps its own date.");
        }

        private const string TheUpwardPhrase = "is caused by";

        private static readonly JiraLinkType Causes = new("Causes", TheUpwardPhrase, "causes");

        private static readonly JiraLinkType Relates = new("Relates", "relates to", "relates to");

        private static readonly string[] OnlyTheOtherFeature = [AnotherFeature];

        private static string Sentence(string counterpart) => $"This issue {TheUpwardPhrase} {counterpart}";

        private static StandingLink ACausedByLink(string counterpart, DateTime addedAt) => new(counterpart, Sentence(counterpart), addedAt);

        private static string RelatesSentence(string counterpart) => $"This issue relates to {counterpart}";

        private static IReadOnlyList<ParentCandidate> TheParentCandidatesOf(Issue issue, LinkDirection direction)
            => issue.Fields.ResolveParentFromLinks(TheUpwardPhrase, direction, issue.StandingLinks).CandidatesWithTheirLinkDates;

        private static Issue AnIssueWithTheHistory(params string[] histories) => AnIssueWithTheLinksAndHistory([], histories);

        private static Issue AnIssueWithTheLinksAndHistory(string[] links, params string[] histories)
            => AnIssueAsJiraSendsIt(links, histories, null);

        private static Issue AnIssueWithAPartOfItsHistory(int entriesInAll, params string[] histories)
            => AnIssueAsJiraSendsIt([], histories, entriesInAll);

        private static Issue AnIssueAsJiraSendsIt(string[] links, string[] histories, int? entriesInAll)
        {
            var json = JiraWireFormat.AnIssueWithItsHistory(
                TheItem,
                "Story",
                JiraWireFormat.AnInstant(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                JiraWireFormat.AnInstant(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc)),
                string.Empty,
                links,
                histories,
                entriesInAll);

            using var document = JsonDocument.Parse(json);

            return new IssueFactory(Mock.Of<ILogger<IssueFactory>>())
                .CreateIssueFromJson(document.RootElement, JiraConnectorTestSetup.ATeamOnJiraCloud());
        }
    }
}
