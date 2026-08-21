using Lighthouse.Backend.Models.Dependencies;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
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

        [Test]
        public async Task GetFeaturesForProject_AFeatureCarriesTheIssuesItsInwardLinksPointAt()
        {
            var recorded = new RecordedJiraRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var waitedOn = features.Single().DependsOnReferences.Select(reference => reference.ReferenceId);
            var expected = new[] { "PROJ-2" };

            Assert.That(waitedOn, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetFeaturesForProject_AFeatureThatOnlyBlocksOthersWaitsOnNothing()
        {
            var recorded = new RecordedJiraRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlocksLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(features.Single().DependsOnReferences, Is.Empty);
        }

        [Test]
        public async Task GetFeaturesForProject_EveryReferenceIsMarkedAsHavingComeFromTheTracker()
        {
            var recorded = new RecordedJiraRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            var features = await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(features.Single().DependsOnReferences.Single().Source, Is.EqualTo(DependencySource.TrackerLink));
        }

        /// <summary>
        /// Everything the connector already read off a Jira issue, read again with the link mapping in
        /// place. The claim is that adding dependencies moved nothing else, which only a comparison over
        /// the whole mapped Feature can make.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_ReadingTheLinksLeavesEveryOtherMappedValueAlone()
        {
            var recorded = new RecordedJiraRequests();
            var withoutLinks = await AConnectorReturning(recorded, AnEpic("PROJ-1"))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var withLinks = await AConnectorReturning(new RecordedJiraRequests(), AnEpic("PROJ-1", BlockedByLink("PROJ-2")))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var before = withoutLinks.Single();
            var after = withLinks.Single();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.ReferenceId, Is.EqualTo(before.ReferenceId));
                Assert.That(after.Name, Is.EqualTo(before.Name));
                Assert.That(after.State, Is.EqualTo(before.State));
                Assert.That(after.Type, Is.EqualTo(before.Type));
                Assert.That(after.Order, Is.EqualTo(before.Order));
                Assert.That(after.ParentReferenceId, Is.EqualTo(before.ParentReferenceId));
                Assert.That(after.CreatedDate, Is.EqualTo(before.CreatedDate));
                Assert.That(after.StartedDate, Is.EqualTo(before.StartedDate));
                Assert.That(after.ClosedDate, Is.EqualTo(before.ClosedDate));
                Assert.That(after.EstimatedSize, Is.EqualTo(before.EstimatedSize));
                Assert.That(after.OwningTeam, Is.EqualTo(before.OwningTeam));
                Assert.That(before.DependsOnReferences, Is.Empty);
            }
        }

        /// <summary>
        /// The links ride on the response the fetch already asks for. Asking for them separately would
        /// undo Epic 5687's Data Center sweep, which is the reason this is asserted rather than assumed.
        /// </summary>
        [Test]
        public async Task GetFeaturesForProject_ReadingTheLinksCostsTheRefreshNoRequestOfItsOwn()
        {
            var withoutLinks = new RecordedJiraRequests();
            await AConnectorReturning(withoutLinks, AnEpic("PROJ-1"))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            var withLinks = new RecordedJiraRequests();
            await AConnectorReturning(withLinks, AnEpic("PROJ-1", BlockedByLink("PROJ-2")))
                .GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(withLinks.Paths, Is.EqualTo(withoutLinks.Paths));
        }

        [Test]
        public async Task GetFeaturesForProject_TheSearchStillAsksForEveryFieldAndNothingIsAddedToIt()
        {
            var recorded = new RecordedJiraRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));

            await subject.GetFeaturesForProject(JiraConnectorTestSetup.APortfolioOnJiraCloud());

            Assert.That(FieldsAskedForIn(recorded.LastSearchUrl), Is.EqualTo("*all"));
        }

        /// <summary>
        /// The sweep is the change-detection pass and never maps a field, so a dependency it downloaded
        /// would be thrown away unread. Keeping it at identity plus the stamp is what took a Data Center
        /// portfolio refresh from about eight minutes to about two seconds, and widening it here is the
        /// one edit in this slice that could hand that back.
        /// </summary>
        [Test]
        public async Task SweepFeaturesForPortfolio_TheIdentitySweepStillAsksForNothingButIdentityAndTheStamp()
        {
            var recorded = new RecordedJiraRequests();
            var subject = AConnectorReturning(recorded, AnEpic("PROJ-1", BlockedByLink("PROJ-2")));
            var portfolio = JiraConnectorTestSetup.APortfolioOnJiraCloud();

            // The sweep refuses an instance Lighthouse has never reached, because the two Jira deployments
            // page their results differently and neither endpoint answers on the other's path.
            await subject.GetFeaturesForProject(portfolio);

            await subject.SweepFeaturesForPortfolio(portfolio);

            Assert.That(FieldsAskedForIn(recorded.LastSearchUrl), Is.EqualTo("key,updated"));
        }

        private static string FieldsAskedForIn(string url)
            => System.Web.HttpUtility.ParseQueryString(new Uri(url).Query)["fields"] ?? string.Empty;

        private static JiraWorkTrackingConnector AConnectorReturning(RecordedJiraRequests recorded, params string[] issues)
            => JiraConnectorTestSetup.AConnectorOver(HandlerReturning(recorded, issues));

        private static HttpMessageHandler HandlerReturning(RecordedJiraRequests recorded, string[] issues)
        {
            var mock = new Mock<HttpMessageHandler>();
            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
                {
                    recorded.Record(request);

                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(BodyFor(request, issues), Encoding.UTF8, "application/json"),
                    });
                });

            return mock.Object;
        }

        private static string BodyFor(HttpRequestMessage request, string[] issues)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal))
            {
                return "{\"deploymentType\":\"Cloud\"}";
            }

            if (path.EndsWith("rest/api/latest/field", StringComparison.Ordinal))
            {
                return "[]";
            }

            if (path.Contains("/search", StringComparison.Ordinal))
            {
                return "{\"issues\":[" + string.Join(",", issues) + "],\"isLast\":true}";
            }

            return "{}";
        }

        private static string AnEpic(string key, params string[] links)
        {
            var fields = "{\"summary\": \"" + key + " summary\""
                + ", \"issuetype\": {\"name\": \"Epic\"}"
                + ", \"status\": {\"name\": \"In Progress\"}"
                + ", \"created\": \"2026-01-01T00:00:00.000+0000\""
                + ", \"updated\": \"2026-01-02T00:00:00.000+0000\""
                + ", \"labels\": []"
                + ", \"issuelinks\": [" + string.Join(",", links) + "]}";

            return "{\"key\": \"" + key + "\", \"fields\": " + fields + "}";
        }

        /// <summary>
        /// Every request the connector made, in order. Two runs of the same fetch have to agree on this
        /// exactly, which is what makes "no extra request" a testable claim rather than a reading of the code.
        /// </summary>
        private sealed class RecordedJiraRequests
        {
            private readonly List<string> urls = [];

            public IReadOnlyList<string> Urls => urls;

            public string LastSearchUrl
            {
                get
                {
                    var searches = urls.FindAll(url => url.Contains("/search", StringComparison.Ordinal));

                    return searches[searches.Count - 1];
                }
            }

            public IReadOnlyList<string> Paths => urls.ConvertAll(url => new Uri(url).AbsolutePath);

            public void Record(HttpRequestMessage request)
            {
                if (request.RequestUri is not null)
                {
                    urls.Add(request.RequestUri.AbsoluteUri);
                }
            }
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
