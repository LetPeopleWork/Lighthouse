using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.QuietWriteBack
{
    /// <summary>
    /// DISTILL acceptance scenarios (Epic 5500 - Quiet write-back), slice 04: a refresh that could not
    /// keep Jira quiet says so, once, and never blames a permission that was not the problem.
    /// Driving port: the scheduled refresh. US-01, AC-01.1 ... AC-01.5.
    ///
    /// What the fake connector cannot show here is the query parameter itself - it lives below
    /// <c>IWorkTrackingConnector</c>, and is asserted in <c>JiraQuietWriteBackTest</c>. What these
    /// scenarios do own is the half a stub cannot: that the suppression outcome survives the whole
    /// refresh and reaches the admin's log at a level they will actually see.
    /// </summary>
    [TestFixture]
    [Category("acceptance")]
    [Category("epic-5500-quiet-writeback")]
    [Category("slice-04")]
    public partial class Slice04QuietWriteBackTest
    {
        // @walking_skeleton @driving_port @real-io @AC-01.1
        [Test]
        public async Task A_refresh_whose_writes_could_not_be_silenced_tells_the_administrator_once()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            GivenJiraWritesTheValueButWillNotStaySilent();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheAdministratorIsWarnedThatWatchersWereEmailed();
            ThenThatWarningNamesTheProject("PROJ");
            ThenThatWarningNamesTheRemedy();
        }

        // @driving_port @AC-01.1 - the anti-regression criterion, seen from the refresh rather than the wire.
        [Test]
        public async Task A_refresh_whose_writes_could_not_be_silenced_still_wrote_the_values()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            GivenJiraWritesTheValueButWillNotStaySilent();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheStoredSizeIs(portfolio, "PROJ-1", "5");
        }

        // @error @driving_port - ADR-142 §3: a 403 that survived the retry was never about notifications.
        [Test]
        public async Task A_refresh_whose_writes_were_refused_outright_does_not_blame_permissions()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);
            GivenJiraRefusesTheWriteAltogether();

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheAdministratorIsNotWarnedAboutNotifications();
        }

        // @driving_port - the quiet case stays quiet in the log too.
        [Test]
        public async Task A_refresh_that_was_silenced_says_nothing_about_notifications()
        {
            var portfolio = GivenAPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "PROJ-1", size: 5);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheAdministratorIsNotWarnedAboutNotifications();
        }

        // @driving_port @AC-01.4 - Azure DevOps suppresses without a permission, so it never appears here.
        [Test]
        public async Task An_azure_devops_refresh_never_warns_about_notifications()
        {
            var portfolio = GivenAnAzureDevOpsPortfolioWhoseSizeAndForecastAreWrittenBack();
            GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(portfolio, "42", size: 5);
            GivenTheForecastRunCompletesIn(10);

            await WhenTheScheduledRefreshRuns(portfolio);

            ThenTheAdministratorIsNotWarnedAboutNotifications();
        }
    }
}
