using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// What a Jira issue says it is waiting on. Jira writes a link once and offers it from both ends:
    /// the waiting issue sees an inwardIssue, the issue being waited on sees an outwardIssue. Only the
    /// first is read, because taking both would record every dependency in the instance twice.
    /// </summary>
    public class JiraDependencyLinkTest
    {
        [Test]
        public void ExtractDependencyReferences_YieldsTheIssueAnInwardBlockingLinkPointsAt()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-42" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingForAnIssueThatOnlyBlocksOthers()
        {
            var fields = FieldsWith(BlocksLink("PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        public void ExtractDependencyReferences_YieldsOnlyTheInwardEndWhenBothDirectionsAreLinked()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-1"), BlocksLink("PROJ-2"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-1" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsEveryInwardBlockingLinkInTheOrderJiraWroteThem()
        {
            var fields = FieldsWith(BlockedByLink("PROJ-1"), BlockedByLink("PROJ-2"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-1", "PROJ-2" };

            Assert.That(references, Is.EqualTo(expected));
        }

        /// <summary>
        /// Jira has several link types and only one of them means waiting. "relates to" and "duplicates"
        /// are links in exactly the same shape, so reading every inwardIssue would turn every loosely
        /// related issue into a blocker.
        /// </summary>
        [Test]
        [TestCase("relates to")]
        [TestCase("duplicates")]
        [TestCase("is cloned by")]
        public void ExtractDependencyReferences_YieldsNothingForAnInwardLinkThatDoesNotMeanWaiting(string inwardName)
        {
            var fields = FieldsWith(InwardLink(inwardName, "PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        [TestCase("Is Blocked By")]
        [TestCase("IS BLOCKED BY")]
        public void ExtractDependencyReferences_ReadsTheLinkNameWithoutMindingItsCapitalisation(string inwardName)
        {
            var fields = FieldsWith(InwardLink(inwardName, "PROJ-42"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-42" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_KeepsTheKeyExactlyAsJiraWroteIt()
        {
            var fields = FieldsWith(BlockedByLink("lower-99"));

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "lower-99" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void ExtractDependencyReferences_YieldsNothingWhenTheIssueCarriesNoLinksAtAll()
        {
            var fields = Fields("{}");

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        [TestCase("""{"issuelinks": []}""")]
        [TestCase("""{"issuelinks": null}""")]
        [TestCase("""{"issuelinks": "not a list"}""")]
        [TestCase("""{"issuelinks": [{}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {}, "inwardIssue": {"key": "PROJ-1"}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}, "inwardIssue": {}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": "is blocked by"}, "inwardIssue": {"key": ""}}]}""")]
        [TestCase("""{"issuelinks": [{"type": {"inward": null}, "inwardIssue": {"key": "PROJ-1"}}]}""")]
        public void ExtractDependencyReferences_YieldsNothingAndThrowsNothingForAPayloadItCannotRead(string rawFields)
        {
            var fields = Fields(rawFields);

            var references = fields.ExtractDependencyReferences();

            Assert.That(references, Is.Empty);
        }

        [Test]
        public void ExtractDependencyReferences_ReadsTheGoodLinksBesideOneItCannotRead()
        {
            var fields = Fields("""
                {"issuelinks": [
                    {"type": {"inward": "is blocked by"}},
                    {"type": {"inward": "is blocked by"}, "inwardIssue": {"key": "PROJ-7"}}
                ]}
                """);

            var references = fields.ExtractDependencyReferences();

            var expected = new[] { "PROJ-7" };

            Assert.That(references, Is.EqualTo(expected));
        }

        [Test]
        public void InwardLinkNames_NamesEveryInwardLinkTheIssueCarries()
        {
            var fields = FieldsWith(InwardLink("is halted by", "PROJ-1"), InwardLink("relates to", "PROJ-2"));

            var names = fields.InwardLinkNames();

            var expected = new[] { "is halted by", "relates to" };

            Assert.That(names, Is.EqualTo(expected));
        }

        /// <summary>
        /// An outward link is the far end of somebody else's dependency, so an instance that has renamed
        /// its link type is not evidenced by one. Naming them would send an administrator looking at the
        /// wrong half of their configuration.
        /// </summary>
        [Test]
        public void InwardLinkNames_SaysNothingAboutLinksPointingTheOtherWay()
        {
            var fields = FieldsWith(BlocksLink("PROJ-42"));

            var names = fields.InwardLinkNames();

            Assert.That(names, Is.Empty);
        }

        [Test]
        public void InwardLinkNames_SaysNothingWhenTheIssueCarriesNoLinksAtAll()
        {
            var fields = Fields("{}");

            var names = fields.InwardLinkNames();

            Assert.That(names, Is.Empty);
        }

        private static string BlockedByLink(string key) => InwardLink("is blocked by", key);

        private static string InwardLink(string inwardName, string key)
            => Link("inwardIssue", inwardName, key);

        private static string BlocksLink(string key)
            => Link("outwardIssue", "is blocked by", key);

        private static string Link(string end, string inwardName, string key)
        {
            var type = "{\"name\": \"Blocks\", \"inward\": \"" + inwardName + "\", \"outward\": \"blocks\"}";
            var issue = "{\"key\": \"" + key + "\", \"fields\": {\"summary\": \"Something\"}}";

            return "{\"type\": " + type + ", \"" + end + "\": " + issue + "}";
        }

        private static JsonElement FieldsWith(params string[] links)
            => Fields($$"""{"issuelinks": [{{string.Join(",", links)}}]}""");

        private static JsonElement Fields(string rawFields)
            => JsonDocument.Parse(rawFields).RootElement.Clone();
    }
}
