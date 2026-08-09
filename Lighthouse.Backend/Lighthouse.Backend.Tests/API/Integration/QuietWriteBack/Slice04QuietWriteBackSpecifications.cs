using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.QuietWriteBack
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5500 slice 04 - quiet Jira write-back.
    /// Backend-observable contract: whether the tracker could be kept quiet survives the refresh as a
    /// fact, and reaches the administrator as exactly one Warning naming the connection, the affected
    /// projects and the remedy - and only when suppression, rather than the write itself, was refused
    /// (ADR-142 §6, D-A4).
    /// </summary>
    public partial class Slice04QuietWriteBackTest : QuietWriteBackAcceptanceTest
    {
        private const string StaleSize = "1";
        private const string StaleForecast = "1999-01-01";

        /// <summary>
        /// The phrase an admin has to be able to act on. Asserting the remedy rather than the whole
        /// sentence keeps the copy free to improve without reding the scenario.
        /// </summary>
        private const string Remedy = "Administer Projects";

        /// <summary>What the warning is about, in the admin's terms rather than the API's.</summary>
        private const string NoisyPhrase = "email";

        private readonly record struct SeededPortfolio(int Id, int SizeFieldId, int ForecastFieldId, int TeamId);

        // --- Given ---

        private SeededPortfolio GivenAPortfolioWhoseSizeAndForecastAreWrittenBack()
            => GivenAPortfolioOn(WorkTrackingSystems.Jira);

        private SeededPortfolio GivenAnAzureDevOpsPortfolioWhoseSizeAndForecastAreWrittenBack()
            => GivenAPortfolioOn(WorkTrackingSystems.AzureDevOps);

        private SeededPortfolio GivenAPortfolioOn(WorkTrackingSystems system)
        {
            var (connectionId, sizeFieldId, forecastFieldId) = SeedPortfolioConnection(system);
            var portfolioId = SeedPortfolio(connectionId);
            var teamId = SeedTeam(connectionId, portfolioId);

            return new SeededPortfolio(portfolioId, sizeFieldId, forecastFieldId, teamId);
        }

        private void GivenAFeatureWhoseStoredSizeAndForecastAreBothOutOfDate(SeededPortfolio portfolio, string referenceId, int size)
            => SeedFeature(portfolio.Id, portfolio.TeamId, referenceId, size,
                (portfolio.SizeFieldId, StaleSize),
                (portfolio.ForecastFieldId, StaleForecast));

        private void GivenTheForecastRunCompletesIn(int workingDays) => TheForecastRunProduces(workingDays);

        /// <summary>
        /// SPIKE-03 Q4 after ADR-142's retry: the credential cannot discard the notification, so the
        /// value lands and the watchers hear about it.
        /// </summary>
        private void GivenJiraWritesTheValueButWillNotStaySilent() => TheTrackerCouldNotSilence(_ => true);

        private void GivenJiraRefusesTheWriteAltogether()
            => TheTrackerRefusedTheWriteEntirely(_ => true, "Jira returned 403 Forbidden");

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        // --- Then ---

        private void ThenTheAdministratorIsWarnedThatWatchersWereEmailed()
        {
            Assert.That(TheSuppressionWarnings(), Has.Count.EqualTo(1),
                "Until slice 05's connection surface ships, this one line is the whole signal — and one per connection per flush, not one per issue. Warnings: "
                + string.Join(" | ", CapturedLogs.Warnings));
        }

        private void ThenThatWarningNamesTheProject(string projectKey)
            => Assert.That(TheSuppressionWarnings().Single(), Does.Contain(projectKey),
                "The permission is project-scoped, so a warning that does not name the project is not actionable.");

        private void ThenThatWarningNamesTheRemedy()
            => Assert.That(TheSuppressionWarnings().Single(), Does.Contain(Remedy),
                "A warning without the remedy is a complaint.");

        private void ThenTheAdministratorIsNotWarnedAboutNotifications()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(CapturedLogs.SawAnything, Is.True,
                    "positive control: the capture saw no log at all, so the assertion below cannot fail");
                Assert.That(TheSuppressionWarnings(), Is.Empty,
                    "Only a refused suppression is a permission problem worth warning about.");
            }
        }

        /// <summary>
        /// Warnings about notification suppression, told apart from any other Warning the refresh may
        /// emit by the phrase an admin acts on. Counting at Warning level is deliberate: this message has
        /// to be visible at default production log levels (ADR-142 §6).
        /// </summary>
        private List<string> TheSuppressionWarnings()
            => [.. CapturedLogs.Warnings.Where(warning =>
                warning.Contains(NoisyPhrase, StringComparison.OrdinalIgnoreCase)
                || warning.Contains(Remedy, StringComparison.OrdinalIgnoreCase))];

        private void ThenTheStoredSizeIs(SeededPortfolio portfolio, string referenceId, string size)
            => Assert.That(TheStoredValueOf(referenceId, portfolio.SizeFieldId), Is.EqualTo(size),
                "A connection that cannot suppress still writes - that is the whole reason the slice was revised.");
    }
}
