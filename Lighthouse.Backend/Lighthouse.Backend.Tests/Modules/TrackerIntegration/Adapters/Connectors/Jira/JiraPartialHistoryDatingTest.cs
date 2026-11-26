using System.Net;
using System.Text;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira;
using Lighthouse.Backend.Tests.TestHelpers;

namespace Lighthouse.Backend.Tests.Modules.TrackerIntegration.Adapters.Connectors.Jira
{
    /// <summary>
    /// A search answer may hold only the latest entries of an item's history while Jira counts more. The item
    /// is then dated from its whole history, read from the route each kind of Jira serves for it: Data Center
    /// hands the whole history back with the issue itself and has no paged changelog, Cloud pages it.
    /// </summary>
    [TestFixture]
    public class JiraPartialHistoryDatingTest
    {
        private const string DataCenter = "Server";

        private const string Cloud = "Cloud";

        private static readonly DateTime OpenedForApproval = new(2025, 5, 12, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime AnalysisBegan = new(2025, 5, 14, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime WorkBegan = new(2025, 5, 16, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime WorkWasSuspended = new(2025, 11, 13, 9, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime WorkWasFinished = new(2025, 6, 20, 9, 0, 0, DateTimeKind.Utc);

        [TestCase(DataCenter, TestName = "DataCenter_ItemInDoingWhoseSearchAnswerHoldsOnlyItsLatestMove_StartsWhenItFirstEnteredDoing")]
        [TestCase(Cloud, TestName = "Cloud_ItemInDoingWhoseSearchAnswerHoldsOnlyItsLatestMove_StartsWhenItFirstEnteredDoing")]
        public async Task ItemInDoingWhoseSearchAnswerHoldsOnlyItsLatestMove_StartsWhenItFirstEnteredDoing(string deployment)
        {
            var tracker = ATrackerHolding(deployment, ASuspendedItem(), entriesInTheSearchAnswer: 1, entriesInTheRereadHistory: 4);

            var item = await TheOnlyItemReadFrom(tracker);

            Assert.That(item.StartedDate, Is.EqualTo(WorkBegan),
                "The item first entered Doing on 16 May 2025, which only its whole history says: the search answer "
                + "holds just the move within Doing. Without that date the item shows no start and falls back to its "
                + "created date. The start must come from the whole history, read before dating.");
        }

        [TestCase(DataCenter, TestName = "DataCenter_FinishedItemWhoseSearchAnswerHoldsOnlyItsLastMove_StartsWhenItFirstEnteredDoing")]
        [TestCase(Cloud, TestName = "Cloud_FinishedItemWhoseSearchAnswerHoldsOnlyItsLastMove_StartsWhenItFirstEnteredDoing")]
        public async Task FinishedItemWhoseSearchAnswerHoldsOnlyItsLastMove_StartsWhenItFirstEnteredDoing(string deployment)
        {
            var tracker = ATrackerHolding(deployment, AFinishedItem(), entriesInTheSearchAnswer: 1, entriesInTheRereadHistory: 2);

            var item = await TheOnlyItemReadFrom(tracker);

            Assert.That((item.StartedDate, item.ClosedDate), Is.EqualTo((WorkBegan, WorkWasFinished)),
                "The item was started on 16 May 2025 and finished on 20 June 2025. A start equal to the closed date "
                + "means it was dated from the truncated answer, which makes its cycle time zero. Date it from the "
                + "whole history.");
        }

        [TestCase(DataCenter, TestName = "DataCenter_ItemWhoseWholeHistoryWasRead_IsMarkedAsHoldingItsWholeHistory")]
        [TestCase(Cloud, TestName = "Cloud_ItemWhoseWholeHistoryWasRead_IsMarkedAsHoldingItsWholeHistory")]
        public async Task ItemWhoseWholeHistoryWasRead_IsMarkedAsHoldingItsWholeHistory(string deployment)
        {
            var tracker = ATrackerHolding(deployment, ASuspendedItem(), entriesInTheSearchAnswer: 1, entriesInTheRereadHistory: 4);

            var item = await TheOnlyItemReadFrom(tracker);

            Assert.That(item.TransitionsAreItsWholeHistory, Is.True,
                "Every entry Jira counts for the item was read, so its history may replace the stored one. A "
                + "history still marked partial leaves the stored transitions as they were.");
        }

        [TestCase(DataCenter, TestName = "DataCenter_ItemWhoseWholeHistoryReadIsStillPartial_StaysMarkedAsNotHoldingItsWholeHistory")]
        [TestCase(Cloud, TestName = "Cloud_ItemWhoseWholeHistoryReadIsStillPartial_StaysMarkedAsNotHoldingItsWholeHistory")]
        public async Task ItemWhoseWholeHistoryReadIsStillPartial_StaysMarkedAsNotHoldingItsWholeHistory(string deployment)
        {
            var tracker = ATrackerHolding(deployment, ASuspendedItem(), entriesInTheSearchAnswer: 1, entriesInTheRereadHistory: 2);

            var item = await TheOnlyItemReadFrom(tracker);

            Assert.That(item.TransitionsAreItsWholeHistory, Is.False,
                "Jira counts four entries and the second read still holds two. Marking such an item whole would "
                + "let a partial history replace the stored transitions. It must stay marked as partial.");
        }

        /// <summary>
        /// Opened for approval, analysed, started on 16 May 2025, then suspended, which is another Doing state,
        /// so only the first move into Doing marks the start.
        /// </summary>
        private static AnItem ASuspendedItem() => new(
            "PROJ-1",
            "Suspended",
            [
                JiraWireFormat.AStatusChangeAt(OpenedForApproval, "Open", "waiting for approval"),
                JiraWireFormat.AStatusChangeAt(AnalysisBegan, "waiting for approval", "In analysis"),
                JiraWireFormat.AStatusChangeAt(WorkBegan, "In analysis", "In Progress"),
                JiraWireFormat.AStatusChangeAt(WorkWasSuspended, "In Progress", "Suspended"),
            ]);

        private static AnItem AFinishedItem() => new(
            "PROJ-2",
            "Done",
            [
                JiraWireFormat.AStatusChangeAt(WorkBegan, "Open", "In Progress"),
                JiraWireFormat.AStatusChangeAt(WorkWasFinished, "In Progress", "Done"),
            ]);

        private static async Task<WorkItem> TheOnlyItemReadFrom(JiraTracker tracker)
        {
            var connector = JiraConnectorTestSetup.AConnectorOver(tracker);

            var items = await connector.GetWorkItemsForTeam(ATeam(), CancellationToken.None);

            return items.Single();
        }

        private static JiraTracker ATrackerHolding(string deployment, AnItem item, int entriesInTheSearchAnswer, int entriesInTheRereadHistory)
            => new(deployment, item, entriesInTheSearchAnswer, entriesInTheRereadHistory);

        private static Team ATeam()
        {
            var team = JiraConnectorTestSetup.ATeamOnJiraCloud();

            team.ToDoStates.AddRange(["Open", "waiting for approval", "In analysis"]);
            team.DoingStates.Add("Suspended");

            return team;
        }

        private sealed record AnItem(string Key, string Status, IReadOnlyList<string> WholeHistory);

        /// <summary>
        /// A Jira whose search answer carries only the latest entries of the item's history, while its own
        /// whole-history route carries the number of entries asked for. Both count every entry of the whole
        /// history as the total, as Jira does.
        /// </summary>
        private sealed class JiraTracker(string deployment, AnItem item, int entriesInTheSearchAnswer, int entriesInTheRereadHistory)
            : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var path = request.RequestUri?.AbsolutePath ?? string.Empty;
                var query = request.RequestUri?.Query ?? string.Empty;

                var isTheItemsChangelog = path.EndsWith($"/issue/{item.Key}/changelog", StringComparison.Ordinal);
                var isTheItemWithItsChangelog = path.EndsWith($"/issue/{item.Key}", StringComparison.Ordinal)
                    && query.Contains("expand=changelog", StringComparison.Ordinal);

                if (isTheItemsChangelog && deployment == DataCenter)
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(BodyFor(path, isTheItemsChangelog, isTheItemWithItsChangelog), Encoding.UTF8, "application/json"),
                });
            }

            private string BodyFor(string path, bool isTheItemsChangelog, bool isTheItemWithItsChangelog)
            {
                if (path.EndsWith(JiraWireFormat.ServerInfoEndpoint, StringComparison.Ordinal))
                {
                    return $"{{\"deploymentType\":\"{deployment}\"}}";
                }

                if (path.EndsWith(JiraWireFormat.FieldListEndpoint, StringComparison.Ordinal))
                {
                    return JiraWireFormat.TheFieldListDefining([]);
                }

                if (JiraWireFormat.TheNameListAnsweredAt(path) is { } nameList)
                {
                    return nameList;
                }

                if (isTheItemsChangelog)
                {
                    return ACloudChangelogPage();
                }

                if (isTheItemWithItsChangelog)
                {
                    return TheItemCarrying(entriesInTheRereadHistory);
                }

                if (path.Contains(JiraWireFormat.SearchEndpoint, StringComparison.Ordinal))
                {
                    return deployment == DataCenter
                        ? $"{{\"startAt\":0,\"maxResults\":50,\"total\":1,\"issues\":[{TheItemCarrying(entriesInTheSearchAnswer)}]}}"
                        : JiraWireFormat.OnePageOf([TheItemCarrying(entriesInTheSearchAnswer)]);
                }

                return "{}";
            }

            private string TheItemCarrying(int latestEntries)
            {
                var entries = item.WholeHistory.TakeLast(latestEntries).ToList();

                return "{\"key\":\"" + item.Key + "\",\"fields\":{\"summary\":\"" + item.Key + " summary\""
                    + ",\"issuetype\":{\"name\":\"Story\"},\"status\":{\"name\":\"" + item.Status + "\"}"
                    + ",\"created\":\"2025-05-01T08:00:00.000+0000\",\"updated\":\"2025-11-14T08:00:00.000+0000\""
                    + ",\"labels\":[],\"issuelinks\":[]}"
                    + ",\"changelog\":{\"startAt\":0,\"maxResults\":" + entries.Count + ",\"total\":" + item.WholeHistory.Count
                    + ",\"histories\":[" + string.Join(",", entries) + "]}}";
            }

            private string ACloudChangelogPage()
                => "{\"values\":[" + string.Join(",", item.WholeHistory.TakeLast(entriesInTheRereadHistory))
                    + "],\"total\":" + item.WholeHistory.Count + ",\"isLast\":true}";
        }
    }
}
