using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// The parent an issue names through one of its links. Jira writes a link once and offers it from
    /// both ends, handing each issue a pointer to the other one, so an entry carrying an outwardIssue
    /// sits on the issue holding the inward end and the other way round. Both shapes arrive from a real
    /// instance, which is why both are exercised here.
    /// </summary>
    public class IssueExtensionsParentLinkTest
    {
        private const string LinkTypeName = "Cloners";

        private const string InwardLabel = "is cloned by";

        private const string OutwardLabel = "clones";

        private const string TheParent = "PROJ-1716";

        private const string AnotherParent = "PROJ-3";

        private const string RelatesLabel = "relates to";

        private static readonly JiraLinkType Cloners = new(LinkTypeName, InwardLabel, OutwardLabel);

        private static readonly string[] BothCandidates = [TheParent, AnotherParent];

        private const string AChild = "PROJ-31";

        private const string AnotherChild = "PROJ-32";

        private static readonly string[] BothChildren = [AChild, AnotherChild];

        private static readonly string[] TheParentAndBothChildren = [TheParent, AChild, AnotherChild];

        private static readonly StandingLink[] NoLinkDates = [];

        private static readonly DateTime TheDayTheParentWasLinked = new(2026, 3, 4, 9, 15, 0, DateTimeKind.Utc);

        [Test]
        public void YieldsTheOutwardCounterpartOfALinkWhoseInwardEndThisIssueHolds()
        {
            var fields = FieldsOfAnIssueWith(Cloners.LinkWhoseOutwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.True);
                Assert.That(resolution.Key, Is.EqualTo(TheParent));
            }
        }

        [Test]
        public void YieldsTheInwardCounterpartOfALinkWhoseOutwardEndThisIssueHolds()
        {
            var fields = FieldsOfAnIssueWith(Cloners.LinkWhoseInwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.True);
                Assert.That(resolution.Key, Is.EqualTo(TheParent));
            }
        }

        [TestCase("CLONERS")]
        [TestCase("Is Cloned By")]
        [TestCase("CLONES")]
        public void MatchesAnyOfTheTypesThreePhrasesWhateverTheCasing(string reference)
        {
            var fields = FieldsOfAnIssueWith(Cloners.LinkWhoseOutwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(reference, LinkDirection.Both, NoLinkDates);

            Assert.That(resolution.Key, Is.EqualTo(TheParent));
        }

        [Test]
        public void YieldsNothingWhenNoLinkCarriesTheConfiguredType()
        {
            var fields = FieldsOfAnIssueWith(JiraLinkType.Blocks("is blocked by").LinkWhoseInwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.False);
                Assert.That(resolution.IsAmbiguous, Is.False);
                Assert.That(resolution.Candidates, Is.Empty);
            }
        }

        [TestCase("""{"type": {"name": "Cloners", "inward": "is cloned by", "outward": "clones"}}""")]
        [TestCase("""{"outwardIssue": {"key": "PROJ-9"}}""")]
        [TestCase("""{"type": {"name": "Cloners"}, "outwardIssue": {}}""")]
        [TestCase("\"a link entry that is not an object at all\"")]
        public void SkipsALinkItCannotReadRatherThanThrowing(string link)
        {
            var fields = FieldsOfAnIssueWith(link);

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            Assert.That(resolution.Candidates, Is.Empty);
        }

        /// <summary>
        /// A type that defines only its name leaves the two end phrases empty, and an empty string is
        /// not a phrase anybody typed. Were it treated as one, a blank or missing field name would
        /// silently resolve a parent from whatever links an issue happens to carry.
        /// </summary>
        [Test]
        public void DoesNotMatchAPhraseTheLinkTypeLeavesUndefined()
        {
            var fields = FieldsOfAnIssueWith(
                """{"type": {"name": "Cloners"}, "outwardIssue": {"key": "PROJ-1716"}}""");

            var resolution = fields.ResolveParentFromLinks(string.Empty, LinkDirection.Both, NoLinkDates);

            Assert.That(resolution.Candidates, Is.Empty);
        }

        [Test]
        public void NamesBothCandidatesWhenTwoDifferentIssuesAreLinkedByTheConfiguredType()
        {
            var fields = FieldsOfAnIssueWith(
                Cloners.LinkWhoseOutwardIssueIs(TheParent),
                Cloners.LinkWhoseInwardIssueIs(AnotherParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsAmbiguous, Is.True);
                Assert.That(resolution.IsResolved, Is.False);
                Assert.That(resolution.Key, Is.Empty);
                Assert.That(resolution.Candidates, Is.EqualTo(BothCandidates));
            }
        }

        /// <summary>
        /// Two links to the same issue are a tidy tracker, not a contradiction, and an item that would
        /// otherwise take a parent must not lose one over it.
        /// </summary>
        [Test]
        public void CountsTwoLinksNamingTheSameIssueAsOneParent()
        {
            var fields = FieldsOfAnIssueWith(
                Cloners.LinkWhoseOutwardIssueIs(TheParent),
                Cloners.LinkWhoseInwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsAmbiguous, Is.False);
                Assert.That(resolution.Key, Is.EqualTo(TheParent));
            }
        }

        /// <summary>
        /// A live instance defines Relates with both of its ends reading the same phrase. Counting a
        /// match once per phrase rather than once per link would turn that single link into two.
        /// </summary>
        [Test]
        public void ReadsALinkTypeWhoseTwoEndsAreNamedAlikeAsOneLink()
        {
            var relates = new JiraLinkType("Relates", RelatesLabel, RelatesLabel);
            var fields = FieldsOfAnIssueWith(relates.LinkWhoseOutwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(RelatesLabel, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.True);
                Assert.That(resolution.Candidates, Has.Count.EqualTo(1));
            }
        }

        /// <summary>
        /// One link type drawn at every level of a hierarchy hands an item in the middle both ends: the
        /// link to its parent, and the links to each of its children. Typing the phrase one end reads as is
        /// what tells the parent apart from the children.
        /// </summary>
        [Test]
        public void ReadsOnlyTheInwardEndsWhenTheInwardPhraseWasTyped()
        {
            var resolution = FieldsOfAMiddleItem().ResolveParentFromLinks(LinkTypeName, LinkDirection.Inward, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.True);
                Assert.That(resolution.Key, Is.EqualTo(TheParent));
            }
        }

        [Test]
        public void ReadsOnlyTheOutwardEndsWhenTheOutwardPhraseWasTyped()
        {
            var resolution = FieldsOfAMiddleItem().ResolveParentFromLinks(LinkTypeName, LinkDirection.Outward, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsAmbiguous, Is.True);
                Assert.That(resolution.Candidates, Is.EqualTo(BothChildren));
            }
        }

        [Test]
        public void ReadsBothEndsWhenNoDirectionWasTyped()
        {
            var resolution = FieldsOfAMiddleItem().ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, NoLinkDates);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsAmbiguous, Is.True);
                Assert.That(resolution.Candidates, Is.EquivalentTo(TheParentAndBothChildren));
            }
        }

        [Test]
        public void TwoResolutionsNamingTheSameCandidatesAreTheSameAnswer()
        {
            var resolution = ParentResolution.From([TheParent]);
            var anotherReadingOfTheSameIssue = ParentResolution.From([TheParent]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution, Is.EqualTo(anotherReadingOfTheSameIssue));
                Assert.That(resolution.GetHashCode(), Is.EqualTo(anotherReadingOfTheSameIssue.GetHashCode()));
                Assert.That(ParentResolution.From([AnotherParent]), Is.Not.EqualTo(resolution));
            }
        }

        /// <summary>
        /// The refresh decides among several parents by when each standing link was added, so every
        /// candidate carries the date the issue's history gives it, and one the history never mentions
        /// stays undated rather than borrowing another's.
        /// </summary>
        [Test]
        public void CarriesTheDayEachCandidatesLinkWasAddedAndLeavesTheOthersUndated()
        {
            var fields = FieldsOfAnIssueWith(
                Cloners.LinkWhoseOutwardIssueIs(TheParent),
                Cloners.LinkWhoseInwardIssueIs(AnotherParent));
            StandingLink[] linkAddedAt = [new(TheParent, $"This issue {OutwardLabel} {TheParent}", TheDayTheParentWasLinked), new(AChild, $"This issue {OutwardLabel} {AChild}", DateTime.UnixEpoch)];

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, linkAddedAt);

            Assert.That(resolution.CandidatesWithTheirLinkDates, Is.EqualTo(new[]
            {
                new ParentCandidate(TheParent, TheDayTheParentWasLinked),
                new ParentCandidate(AnotherParent, null),
            }));
        }

        /// <summary>
        /// A link is dated only by an entry that reads as this link: this end's phrase before the key. When no
        /// entry does, or two do and disagree, the date is not known, and an undated link loses to a dated one
        /// rather than winning on a date that belongs to some other link.
        /// </summary>
        [Test]
        public void LeavesALinkUndatedWhenNoEntryReadsAsItOrTheEntriesThatDoDisagree()
        {
            var fields = FieldsOfAnIssueWith(
                Cloners.LinkWhoseOutwardIssueIs(TheParent),
                Cloners.LinkWhoseOutwardIssueIs(AnotherParent));
            StandingLink[] standingLinks =
            [
                new(TheParent, $"This issue {OutwardLabel} {TheParent}", TheDayTheParentWasLinked),
                new(TheParent, $"This work item {OutwardLabel} {TheParent}", DateTime.UnixEpoch),
                new(AnotherParent, $"This issue {InwardLabel} {AnotherParent}", TheDayTheParentWasLinked),
            ];

            var resolution = fields.ResolveParentFromLinks(LinkTypeName, LinkDirection.Both, standingLinks);

            Assert.That(resolution.CandidatesWithTheirLinkDates, Is.EqualTo(new[]
            {
                new ParentCandidate(TheParent, null),
                new ParentCandidate(AnotherParent, null),
            }));
        }

        private static JsonElement FieldsOfAMiddleItem()
            => FieldsOfAnIssueWith(
                Cloners.LinkWhoseInwardIssueIs(TheParent),
                Cloners.LinkWhoseOutwardIssueIs(AChild),
                Cloners.LinkWhoseOutwardIssueIs(AnotherChild));

        private static JsonElement FieldsOfAnIssueWith(params string[] links)
        {
            var issue = JiraWireFormat.AnIssueOfType("PROJ-24", "Story", links);

            return JsonDocument.Parse(issue).RootElement.Clone().GetProperty("fields");
        }
    }
}
