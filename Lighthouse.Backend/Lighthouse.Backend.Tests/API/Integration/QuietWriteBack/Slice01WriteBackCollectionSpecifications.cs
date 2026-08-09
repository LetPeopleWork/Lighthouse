using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.WriteBack;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.QuietWriteBack
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5500 slice 01 - the write-back collection seam.
    /// Backend-observable contract: one update execution resolves every mapped value it can and reaches
    /// the tracker at most once, and a value the tracker accepted becomes the stored copy so the next
    /// execution finds nothing to write (ADR-144, D-A7 / D-A7-R).
    /// </summary>
    public partial class Slice01WriteBackCollectionTest : QuietWriteBackAcceptanceTest
    {
        private const string StaleSize = "1";
        private const string StaleForecast = "1999-01-01";

        private readonly record struct SeededPortfolio(int Id, int SizeFieldId, int ForecastFieldId, int TeamId);

        private readonly record struct SeededTeam(int Id, int FieldId);

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

        /// <summary>
        /// The no-op case: what the tracker already holds is exactly what this refresh would resolve, so
        /// the existing inequality guard has nothing to report.
        /// </summary>
        private void GivenAFeatureWhoseStoredValuesAreAlreadyCorrect(SeededPortfolio portfolio, string referenceId, int size, int workingDaysToCompletion)
            => SeedFeature(portfolio.Id, portfolio.TeamId, referenceId, size,
                (portfolio.SizeFieldId, size.ToString()),
                (portfolio.ForecastFieldId, ForecastDateAfter(workingDaysToCompletion)));

        private void GivenTheForecastRunCompletesIn(int workingDays) => TheForecastRunProduces(workingDays);

        private SeededTeam GivenATeamWhoseItemAgeIsWrittenBack()
        {
            var (connectionId, fieldId) = SeedTeamConnection();
            return new SeededTeam(SeedTeam(connectionId), fieldId);
        }

        private void GivenAWorkItemWhoseStoredAgeIsOutOfDate(SeededTeam team, string referenceId, int ageInDays)
            => SeedWorkItem(team.Id, referenceId, ageInDays, (team.FieldId, StaleSize));

        private void GivenTheTrackerIsUnreachable()
            => TheTrackerThrows(new HttpRequestException("The tracker is unreachable"));

        private void GivenTheTrackerRefusesTheSizeField()
            => TheTrackerRejects(update => update.TargetFieldReference == FieldReference, "Field is not on the screen");

        // --- When ---

        private Task WhenTheScheduledRefreshRuns(SeededPortfolio portfolio) => ThePortfolioRefreshRuns(portfolio.Id);

        private Task WhenTheForecastRefreshRunsOnItsOwn(SeededPortfolio portfolio) => TheForecastRefreshRuns(portfolio.Id);

        private Task WhenTheScheduledTeamRefreshRuns(SeededTeam team) => TheTeamRefreshRuns(team.Id);

        private void WhenTheTrackerReportsADifferentSizeOnTheNextSync(string referenceId, string size)
            => TheInboundSyncReports(referenceId, TheMappedFields().SizeFieldId, size);

        /// <summary>
        /// Calls the resolver on its own, outside any update execution. After ADR-144 it returns a plan;
        /// the promise this asserts - that resolving performs no I/O - is the same either way, which is
        /// why the scenario does not name the return type.
        /// </summary>
        private async Task WhenTheWriteBackPlanForThePortfolioIsResolved(SeededPortfolio portfolio)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var entity = sp.GetRequiredService<IRepository<Portfolio>>().GetById(portfolio.Id)!;

            await sp.GetRequiredService<IWriteBackTriggerService>().TriggerFeatureWriteBackForPortfolio(entity);
        }

        // --- Then ---

        private void ThenTheTrackerWasWrittenTo(int times)
        {
            Assert.That(ConnectorWrites, Has.Count.EqualTo(times),
                $"The refresh reached the tracker {ConnectorWrites.Count} time(s): {DescribeWrites()}");
        }

        private void ThenTheTrackerWasNeverWrittenTo() => ThenTheTrackerWasWrittenTo(times: 0);

        private void ThenThatWriteCarriedBothTheSizeAndTheForecastFor(string referenceId)
        {
            var updates = ConnectorWrites.Single().Updates.Where(u => u.WorkItemId == referenceId).ToList();

            Assert.That(updates.Select(u => u.TargetFieldReference),
                Is.EquivalentTo(new[] { FieldReference, ForecastFieldReference }),
                "One flush must carry what both passes resolved - that is the whole point of collecting them.");
        }

        private void ThenTheLastWriteCarriedTheForecastFor(string referenceId)
        {
            Assert.That(ConnectorWrites, Is.Not.Empty, "Nothing reached the tracker at all.");
            Assert.That(
                ConnectorWrites[^1].Updates.Any(u => u.WorkItemId == referenceId && u.TargetFieldReference == ForecastFieldReference),
                Is.True,
                $"A forecast that genuinely moved must still be written (D11 stands). Writes: {DescribeWrites()}");
        }

        private void ThenTheRefreshRoundWasRecordedAsComplete(SeededPortfolio portfolio)
        {
            Assert.That(TheRefreshLog().Any(entry => entry.Type == RefreshType.Portfolio && entry.EntityId == portfolio.Id),
                Is.True,
                "A flush failure must not abort the refresh round - the round still has to be recorded.");
        }

        private void ThenTheStoredSizeOfIsStillTheOldOne(string referenceId)
            => Assert.That(TheStoredValueOf(referenceId, TheMappedFields().SizeFieldId), Is.EqualTo(StaleSize),
                "A write the tracker refused must never update the local copy.");

        private void ThenTheStoredForecastOfWasBroughtUpToDate(string referenceId)
            => Assert.That(TheStoredValueOf(referenceId, TheMappedFields().ForecastFieldId), Is.EqualTo(ForecastDateAfter(10)),
                "A field the tracker accepted becomes the stored copy, even when a sibling field in the same flush failed.");

        private void ThenTheStoredSizeOfWasBroughtUpToDate(string referenceId, string size)
            => Assert.That(TheStoredValueOf(referenceId, TheMappedFields().SizeFieldId), Is.EqualTo(size),
                "Without this the next assertion is vacuous: the tracker only gets the last word over a value write-back actually persisted (D-A7-R).");

        private void ThenTheStoredSizeOfIs(string referenceId, string size)
            => Assert.That(TheStoredValueOf(referenceId, TheMappedFields().SizeFieldId), Is.EqualTo(size));

        // --- Helpers ---

        /// <summary>
        /// The mapped field ids, read off the connection rather than navigated to from the item - a
        /// Feature reloaded outside the refresh does not carry its Portfolios, and a Then that depended
        /// on that navigation would fail on a null reference instead of on its own assertion.
        /// </summary>
        private (int SizeFieldId, int ForecastFieldId) TheMappedFields()
        {
            using var scope = Factory.Services.CreateScope();

            var fields = scope.ServiceProvider.GetRequiredService<IRepository<WorkTrackingSystemConnection>>()
                .GetAll()
                .SelectMany(connection => connection.AdditionalFieldDefinitions)
                .ToList();

            return (
                fields.First(f => f.Reference == FieldReference).Id,
                fields.First(f => f.Reference == ForecastFieldReference).Id);
        }

        private string ForecastDateAfter(int workingDays)
        {
            using var scope = Factory.Services.CreateScope();
            var clock = scope.ServiceProvider.GetRequiredService<ILighthouseClock>();

            return clock.TodayAsUtcMidnight.AddDays(workingDays).ToString("yyyy-MM-dd");
        }

        private string DescribeWrites()
            => string.Join(" | ", ConnectorWrites.Select(write =>
                string.Join(", ", write.Updates.Select(u => $"{u.WorkItemId}/{u.TargetFieldReference}={u.Value}"))));
    }
}
