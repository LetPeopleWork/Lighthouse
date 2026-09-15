using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;
using Moq;
using Moq.Protected;

namespace Lighthouse.Backend.Tests.Services.Implementation.WorkTrackingConnectors.Jira
{
    /// <summary>
    /// Epic #5511 slice 04, AC-04.2 — the number, against the connector that carries the worst measurement
    /// the Epic has: Data Center went 468 856 ms to 2 087 ms in Epic #5687 by changing how this pages, which
    /// is what says the wall-clock of a refresh is inside this loop.
    ///
    /// The acceptance scenarios cancel a refresh and watch it stop, but they stop a <em>mock</em> that was
    /// written to honour the token. That proves the ask arrives; it cannot prove a real connector acts on
    /// it. This counts the HTTP round trips a real <c>JiraWorkTrackingConnector</c> makes against a tracker
    /// with more pages than it will be allowed to read, which is the only way the granularity claim is
    /// worth anything.
    ///
    /// Jira Cloud and Data Center page by completely different mechanisms - a continuation token and an
    /// offset - and neither endpoint exists on the other, so both are counted.
    ///
    /// The page walk is not the whole of what a Cloud download does. Any issue carrying more than thirty
    /// changelog entries is re-read on its own endpoint, which pages again - so one page of fifty such
    /// issues is fifty nested walks, and none of them used to look at the token. The granularity AC-04.2
    /// records was measured on the sweep and was simply false here.
    /// </summary>
    [TestFixture]
    public class JiraCancellationGranularityTest
    {
        /// <summary>
        /// Far more than any test here lets it read, so "it stopped" cannot be satisfied by a tracker that
        /// ran out of pages to give.
        /// </summary>
        private const int TotalTheTrackerClaims = 100_000;

        /// <summary>Over the thirty that make the connector re-read an issue's changelog on its own endpoint.</summary>
        private const int ChangelogEntriesThatForceASecondRead = 100;

        /// <summary>
        /// Five issues on the page, four changelog pages each: twenty nested round trips available, so
        /// stopping after three is unmistakably stopping rather than running out.
        /// </summary>
        private const int IssuesOnThePage = 5;

        private const int ChangelogPagesPerIssue = 4;

        [TestCase("Server", TestName = "DataCenter_CancelledBeforeItStarts_NeverAsksTheTrackerAnything")]
        [TestCase("Cloud", TestName = "Cloud_CancelledBeforeItStarts_NeverAsksTheTrackerAnything")]
        public async Task CancelledBeforeItStarts_NeverReachesTheSearchEndpoint(string deployment)
        {
            var searches = 0;
            var connector = AConnectorCounting(deployment, () => searches++);

            using var alreadyCancelled = new CancellationTokenSource();
            await alreadyCancelled.CancelAsync();

            Assert.That(
                async () => await connector.GetWorkItemsForTeam(ATeam(), alreadyCancelled.Token),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(searches, Is.Zero,
                "A refresh cancelled before it began must not spend the rate limit an operator cancelled to "
                + "protect. The first page is already too many.");
        }

        [TestCase("Server", TestName = "DataCenter_CancelledWhileItPages_StopsWithinOnePageRoundTrip")]
        [TestCase("Cloud", TestName = "Cloud_CancelledWhileItPages_StopsWithinOnePageRoundTrip")]
        public void CancelledWhileItPages_StopsWithinOnePageRoundTrip(string deployment)
        {
            using var stopAfterTheThirdPage = new CancellationTokenSource();

            var searches = 0;
            var connector = AConnectorCounting(deployment, () =>
            {
                searches++;
                if (searches == 3)
                {
                    stopAfterTheThirdPage.Cancel();
                }
            });

            Assert.That(
                async () => await connector.GetWorkItemsForTeam(ATeam(), stopAfterTheThirdPage.Token),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(searches, Is.EqualTo(3),
                "This is the number AC-04.2 asserts against: a refresh told to stop mid-walk stops after the "
                + "round trip it is already in, not after the whole result set. A fourth page means the "
                + "checkpoint is outside the loop, where the tracker has thousands of pages left to give.");
        }

        [Test]
        public void Cloud_CancelledWhileItRereadsChangelogs_StopsWithinOneChangelogRoundTrip()
        {
            using var stopAfterTheThirdChangelog = new CancellationTokenSource();

            var changelogReads = 0;
            var connector = AConnectorWhoseIssuesAllCarryLongChangelogs(() =>
            {
                changelogReads++;
                if (changelogReads == 3)
                {
                    stopAfterTheThirdChangelog.Cancel();
                }
            });

            Assert.That(
                async () => await connector.GetWorkItemsForTeam(ATeam(), stopAfterTheThirdChangelog.Token),
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(changelogReads, Is.EqualTo(3),
                "The re-read is where a Cloud download actually spends its time: one walk per issue with a "
                + "long history, nested inside the page walk. Reading the remaining seventeen after an "
                + "operator said stop is the whole cost this slice exists to stop paying.");
        }

        /// <summary>
        /// A Cloud tracker whose every issue carries a history long enough to be re-read, and whose changelog
        /// endpoint has several pages per issue. Bounded on both axes: a connector that ignores the token
        /// finishes and fails the count rather than hanging.
        /// </summary>
        private static JiraWorkTrackingConnector AConnectorWhoseIssuesAllCarryLongChangelogs(Action onChangelog)
        {
            var changelogPagesServed = new Dictionary<string, int>(StringComparer.Ordinal);

            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    token.ThrowIfCancellationRequested();

                    var path = request.RequestUri?.AbsolutePath ?? string.Empty;

                    if (path.Contains("/changelog", StringComparison.Ordinal))
                    {
                        onChangelog();

                        var issueKey = path.Split('/')[^2];
                        changelogPagesServed.TryGetValue(issueKey, out var served);
                        changelogPagesServed[issueKey] = served + 1;

                        return Task.FromResult(AnAnswerOf(JiraWireFormat.AChangelogPage(
                            ChangelogEntriesThatForceASecondRead, isLast: served + 1 >= ChangelogPagesPerIssue)));
                    }

                    var body = path switch
                    {
                        _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                            => "{\"deploymentType\":\"Cloud\"}",
                        _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal)
                            => "[]",
                        _ when path.Contains("rest/api/3/search/jql", StringComparison.Ordinal)
                            => APageOfIssuesWithLongChangelogs(),
                        _ => "{}",
                    };

                    return Task.FromResult(AnAnswerOf(body));
                });

            return JiraConnectorTestSetup.AConnectorOver(handler.Object);
        }

        private static string APageOfIssuesWithLongChangelogs()
        {
            var issues = string.Join(",", Enumerable.Range(1, IssuesOnThePage).Select(issue =>
                JiraWireFormat.AnIssueWithAChangelogOf($"LGHTHS-{issue}", ChangelogEntriesThatForceASecondRead)));

            return $"{{\"issues\":[{issues}],\"nextPageToken\":\"there-is-always-more\"}}";
        }

        private static HttpResponseMessage AnAnswerOf(string body)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

        private static JiraWorkTrackingConnector AConnectorCounting(string deployment, Action onSearch)
        {
            var handler = new Mock<HttpMessageHandler>();
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((request, token) =>
                {
                    // The client is handed the token too, so an honest stub refuses once it is set - exactly
                    // as a socket would.
                    token.ThrowIfCancellationRequested();

                    var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                    if (path.Contains("/search", StringComparison.Ordinal))
                    {
                        onSearch();
                    }

                    return Task.FromResult(AnAnswerTo(path, deployment));
                });

            return JiraConnectorTestSetup.AConnectorOver(handler.Object);
        }

        /// <summary>
        /// A tracker that always has another page: Data Center is told the total dwarfs the offset, and
        /// Cloud is always handed a next-page token.
        /// </summary>
        private static HttpResponseMessage AnAnswerTo(string path, string deployment)
        {
            var body = path switch
            {
                _ when path.EndsWith("rest/api/2/serverInfo", StringComparison.Ordinal)
                    => $"{{\"deploymentType\":\"{deployment}\"}}",
                _ when path.EndsWith("rest/api/latest/field", StringComparison.Ordinal)
                    => "[]",
                _ when path.Contains("rest/api/latest/search", StringComparison.Ordinal)
                    => $"{{\"startAt\":0,\"maxResults\":50,\"total\":{TotalTheTrackerClaims},\"issues\":[]}}",
                _ when path.Contains("rest/api/3/search/jql", StringComparison.Ordinal)
                    => "{\"issues\":[],\"nextPageToken\":\"there-is-always-more\"}",
                _ => "{}",
            };

            return AnAnswerOf(body);
        }

        private static Team ATeam() => JiraConnectorTestSetup.ATeamOnJiraCloud(null);
    }
}
