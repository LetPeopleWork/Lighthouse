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

        [Test]
        public void YieldsTheOutwardCounterpartOfALinkWhoseInwardEndThisIssueHolds()
        {
            var fields = FieldsOfAnIssueWith(Cloners.LinkWhoseOutwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

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

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

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

            var resolution = fields.ResolveParentFromLinks(reference);

            Assert.That(resolution.Key, Is.EqualTo(TheParent));
        }

        [Test]
        public void YieldsNothingWhenNoLinkCarriesTheConfiguredType()
        {
            var fields = FieldsOfAnIssueWith(JiraLinkType.Blocks("is blocked by").LinkWhoseInwardIssueIs(TheParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

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

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

            Assert.That(resolution.Candidates, Is.Empty);
        }

        [Test]
        public void NamesBothCandidatesWhenTwoDifferentIssuesAreLinkedByTheConfiguredType()
        {
            var fields = FieldsOfAnIssueWith(
                Cloners.LinkWhoseOutwardIssueIs(TheParent),
                Cloners.LinkWhoseInwardIssueIs(AnotherParent));

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

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

            var resolution = fields.ResolveParentFromLinks(LinkTypeName);

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

            var resolution = fields.ResolveParentFromLinks(RelatesLabel);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(resolution.IsResolved, Is.True);
                Assert.That(resolution.Candidates, Has.Count.EqualTo(1));
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

        private static JsonElement FieldsOfAnIssueWith(params string[] links)
        {
            var issue = JiraWireFormat.AnIssueOfType("PROJ-24", "Story", links);

            return JsonDocument.Parse(issue).RootElement.Clone().GetProperty("fields");
        }
    }
}
