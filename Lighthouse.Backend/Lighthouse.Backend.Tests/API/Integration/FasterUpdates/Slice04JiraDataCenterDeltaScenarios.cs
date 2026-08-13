using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.FasterUpdates
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5687 — Faster Updates), slice 04: a work tracking system that
    /// hands back the same record twice in one scan costs one download and one stored row, and says so.
    /// Driving port: the scheduled refresh. US-04, AC-4.3.
    ///
    /// This fixture is deliberately small, and the reason is worth stating. Slice 04 is transport work: what
    /// changes is which requests the Jira connector issues for an on-premise instance, and the acceptance
    /// suite fakes the connector by policy, so it cannot see a single one of them. Those assertions live in
    /// <c>Services/Implementation/WorkTrackingConnectors/Jira/JiraIncrementalSyncTest</c>, against a stub
    /// transport that records the requests.
    ///
    /// What is left for this level is AC-4.3, and only AC-4.3: a repeated reference id is collapsed before
    /// the rest of the cycle sees it. That is a promise about the refresh, not about Jira — every other
    /// promise US-04 makes (AC-4.4) is US-02's and US-03's, already asserted in slices 02 and 03 against the
    /// same faked connector, and those assertions do not know or care which deployment answered.
    ///
    /// Both scenarios pass on arrival: the collapse already exists, and the scan already goes through it.
    /// They are guards, and each carries its own positive control - the repeated record has to be stored and
    /// downloaded at all, or "exactly one copy" would be satisfied by nothing being there.
    ///
    /// Both ship [Ignore]d. DELIVER un-ignores one at a time; each is one TDD cycle, and a guard's cycle is
    /// the shortest kind - un-ignore, watch it pass, and prove it can fail by taking the collapse out.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5687-faster-updates")]
    [Category("slice-04")]
    public partial class Slice04JiraDataCenterDeltaTest
    {
        // @driving_port @real-io @AC-4.3 @D2 @declared_guard @contract-shape:bounded-change
        // Offset paging over an unordered query is the documented way this instance repeats a record. The
        // collapse has to happen in the scan, not only on the way into storage: a download list built from a
        // scan that repeats itself pays twice for the same payload on every cycle.
        [Test]
        [Ignore(Pending)]
        public async Task An_issue_the_tracker_reports_twice_is_downloaded_once_and_stored_once()
        {
            var team = GivenATeamWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoIssues();
            await GivenTheTeamHasAlreadyBeenRefreshed(team);

            GivenTheTrackerStartsReportingOneIssueTwiceAndThatIssueMoved();
            await WhenTheScheduledRefreshRuns(team);

            ThenTheRepeatedIssueWasAskedForOnce();
            ThenTheTeamHasOneCopyOfEachIssue(team);
            ThenTheOperatorIsToldHowManyCopiesWereDropped(TheRepeatedIssue);
        }

        // @driving_port @real-io @AC-4.3 @D2 @declared_guard @contract-shape:bounded-change
        // The portfolio half reaches the same collapse from its own call site, and a Feature counted twice
        // doubles the remaining work, the size percentile and the forecast that reads them.
        [Test]
        [Ignore(Pending)]
        public async Task A_feature_the_tracker_reports_twice_is_downloaded_once_and_claimed_once()
        {
            var portfolio = GivenAPortfolioWhoseTrackerCanBeScanned();
            GivenTheOperatorAskedForTheCheaperRefresh();
            GivenTheTrackerHoldsTwoFeatures();
            await GivenThePortfolioHasAlreadyBeenRefreshed(portfolio);

            GivenTheTrackerStartsReportingOneFeatureTwiceAndThatFeatureMoved();
            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheRepeatedFeatureWasAskedForOnce();
            ThenThePortfolioHasOneCopyOfEachFeature(portfolio);
            ThenTheOperatorIsToldHowManyCopiesWereDropped(TheRepeatedFeature);
        }
    }
}
