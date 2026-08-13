using Lighthouse.Backend.Models;
using NUnit.Framework;
using Serilog.Events;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5687 slice 04 — a work tracking system that hands
    /// back the same record twice in one scan.
    ///
    /// Backend-observable contract: a repeated reference id is collapsed to one copy before anything else in
    /// the cycle sees it, so the download asks for it once, storage holds one row, and the operator is told
    /// how many copies were dropped rather than being left to work it out from a count that does not add up.
    ///
    /// The connector is faked here by policy, which is what makes this fixture deployment-agnostic: it
    /// describes what a refresh does with a repeated record, not how Jira Data Center comes to repeat one.
    /// The Data Center transport itself is asserted in
    /// <c>Services/Implementation/WorkTrackingConnectors/Jira/JiraIncrementalSyncTest</c>, where the stub
    /// transport can see the actual requests.
    /// </summary>
    public partial class Slice04JiraDataCenterDeltaTest : FasterUpdatesAcceptanceTest
    {
        private const string TheRepeatedIssue = "ITEM-1";
        private const string TheOtherIssue = "ITEM-2";

        private const string TheRepeatedFeature = "FEAT-1";
        private const string TheOtherFeature = "FEAT-2";

        /// <summary>What the collapse says when it drops a copy, as a log pipeline would grep for it.</summary>
        private const string TheCollapseNotice = "duplicate copies for";

        private static readonly DateTime AWhileAgo = new(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime JustNow = new(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc);

        private static readonly string[] TheTwoIssues = [TheRepeatedIssue, TheOtherIssue];
        private static readonly string[] TheTwoFeatures = [TheRepeatedFeature, TheOtherFeature];

        private readonly record struct SeededTeam(int Id, string Name);

        private readonly record struct SeededPortfolio(int Id, string Name);

        // --- Given ---

        private SeededTeam GivenATeamWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var teamName = $"Team {Guid.NewGuid():N}";
            var teamId = SeedTeam(connectionId, teamName);

            TheTrackerCanBeScanned();

            return new SeededTeam(teamId, teamName);
        }

        private SeededPortfolio GivenAPortfolioWhoseTrackerCanBeScanned()
        {
            var connectionId = SeedConnection();
            var portfolioName = $"Portfolio {Guid.NewGuid():N}";
            var portfolioId = SeedPortfolio(connectionId, portfolioName);

            TheTrackerCanBeScanned();

            return new SeededPortfolio(portfolioId, portfolioName);
        }

        private void GivenTheOperatorAskedForTheCheaperRefresh() => TheOperatorAsksForTheCheaperRefresh();

        private void GivenTheTrackerHoldsTwoIssues()
            => TheTrackerHolds(
                new RemoteRecord(TheRepeatedIssue, AWhileAgo),
                new RemoteRecord(TheOtherIssue, AWhileAgo));

        private void GivenTheTrackerHoldsTwoFeatures()
            => TheTrackerHoldsFeatures(
                new RemoteRecord(TheRepeatedFeature, AWhileAgo),
                new RemoteRecord(TheOtherFeature, AWhileAgo));

        /// <summary>
        /// Pillar 2: the second cycle's precondition is the first cycle, run through the same driving port
        /// with the same step method — not a hand-built row that happens to look like its result.
        /// </summary>
        private Task GivenTheTeamHasAlreadyBeenRefreshed(SeededTeam team) => WhenTheScheduledRefreshRuns(team);

        private Task GivenThePortfolioHasAlreadyBeenRefreshed(SeededPortfolio portfolio) => WhenTheScheduledRefreshRuns(portfolio);

        /// <summary>
        /// The tracker is re-stated from one whole picture rather than nudged, so the issue that was not
        /// repeated keeps the change stamp it had and the repetition is the only new thing about the answer.
        /// The repeated copies moved together, the way one edited issue returned on two pages would.
        /// </summary>
        private void GivenTheTrackerStartsReportingOneIssueTwiceAndThatIssueMoved()
            => TheTrackerHolds(
                new RemoteRecord(TheRepeatedIssue, JustNow),
                new RemoteRecord(TheRepeatedIssue, JustNow),
                new RemoteRecord(TheOtherIssue, AWhileAgo));

        private void GivenTheTrackerStartsReportingOneFeatureTwiceAndThatFeatureMoved()
            => TheTrackerHoldsFeatures(
                new RemoteRecord(TheRepeatedFeature, JustNow),
                new RemoteRecord(TheRepeatedFeature, JustNow),
                new RemoteRecord(TheOtherFeature, AWhileAgo));

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        // --- Then: what the tracker was asked for ---

        /// <summary>
        /// The observation that separates a scan which collapsed from one which did not. Both end with one
        /// stored row, because the download collapses a second time on the way in — but only a scan that
        /// collapsed asks for the record once, and paying twice for the same payload on every cycle is the
        /// cost this epic exists to remove.
        /// </summary>
        private void ThenTheRepeatedIssueWasAskedForOnce()
        {
            Assert.That(PayloadDownloads, Has.Count.EqualTo(1),
                "One cycle asks for the changed payloads once. Requests: " + RenderRequests(PayloadDownloads));

            Assert.That(PayloadDownloads[0].FindAll(referenceId => referenceId == TheRepeatedIssue), Has.Count.EqualTo(1),
                "The scan named the same issue twice, so a download list built straight from it names it twice too. "
                + "Requested: " + RenderRequests(PayloadDownloads));
        }

        private void ThenTheRepeatedFeatureWasAskedForOnce()
        {
            Assert.That(FeaturePayloadDownloads, Has.Count.EqualTo(1),
                "One cycle asks for the changed Feature payloads once. Requests: " + RenderRequests(FeaturePayloadDownloads));

            Assert.That(FeaturePayloadDownloads[0].FindAll(referenceId => referenceId == TheRepeatedFeature), Has.Count.EqualTo(1),
                "The scan named the same Feature twice, so a download list built straight from it names it twice too. "
                + "Requested: " + RenderRequests(FeaturePayloadDownloads));
        }

        private static string RenderRequests(List<List<string>> requests)
            => string.Join(" | ", requests.ConvertAll(request => string.Join(",", request)));

        // --- Then: what is stored, and what the operator reads ---

        private void ThenTheTeamHasOneCopyOfEachIssue(SeededTeam team)
        {
            var stored = TheStoredWorkItemsFor(team.Id).ConvertAll(issue => issue.ReferenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored, Is.EquivalentTo(TheTwoIssues),
                    "A second row for the same reference id is what breaks every later cycle: the lookup that matches a "
                    + "fetched record to a stored one finds two and throws. Stored: " + string.Join(",", stored));
                Assert.That(stored.FindAll(referenceId => referenceId == TheRepeatedIssue), Has.Count.EqualTo(1),
                    "positive control: the repeated issue has to be stored at all, or the count above is a pass by absence.");
            }
        }

        private void ThenThePortfolioHasOneCopyOfEachFeature(SeededPortfolio portfolio)
        {
            var stored = TheFeaturesInThePortfolio(portfolio.Id).ConvertAll(feature => feature.ReferenceId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored, Is.EquivalentTo(TheTwoFeatures),
                    "A portfolio claiming the same Feature twice double counts everything derived from it - remaining work, "
                    + "the forecast, the size percentile. Stored: " + string.Join(",", stored));
                Assert.That(stored.FindAll(referenceId => referenceId == TheRepeatedFeature), Has.Count.EqualTo(1),
                    "positive control: the repeated Feature has to be in the portfolio at all, or the count above is a pass by absence.");
            }
        }

        private void ThenTheOperatorIsToldHowManyCopiesWereDropped(string referenceId)
        {
            var warnings = CapturedLogs.AtOrAbove(LogEventLevel.Warning);
            var collapses = warnings.Where(line => line.Contains(TheCollapseNotice, StringComparison.Ordinal)).ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(collapses, Is.Not.Empty,
                    "A silent collapse leaves the operator reading a scanned count that does not match what their tracker "
                    + "reports, with nothing saying why. Lines: " + string.Join(" | ", warnings));
                Assert.That(collapses, Has.Some.Contains(referenceId),
                    "The count alone does not say which record repeated, and on a decades-old instance that is the "
                    + "difference between a fixable data problem and a mystery. Lines: " + string.Join(" | ", collapses));
                Assert.That(collapses, Has.Some.Contains($"1 {TheCollapseNotice}"),
                    "How many copies were dropped is the number that says whether this is one stray record or a paging "
                    + "fault worth taking to the Jira admin. Lines: " + string.Join(" | ", collapses));
            }
        }
    }
}
