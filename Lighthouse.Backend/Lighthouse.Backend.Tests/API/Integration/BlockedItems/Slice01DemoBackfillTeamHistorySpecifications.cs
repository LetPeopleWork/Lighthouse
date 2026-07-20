using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 01 / US-01 — demo backfill keyspace purity.
    /// Backend-observable contract: after a demo portfolio refresh, the team blocked-spell table holds
    /// no portfolio-owner rows, the team historic reads answer only from genuine team history, and the
    /// portfolio snapshot backfill still lands (on a real FK-enforcing database — see the FK-bite
    /// scenario; EF InMemory cannot distinguish broken from fixed here).
    /// </summary>
    public partial class Slice01DemoBackfillTeamHistoryTest : PortfolioBlockedHistoryAcceptanceTest
    {
        private static readonly DateTime PastRangeDate = DateTime.UtcNow.Date.AddDays(-1);

        // --- Given ---

        private SeededPortfolio GivenADemoPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, demoConnection: true, blockedState: "Blocked");

        private SeededTeamForPortfolio GivenATeam()
            => SeedTeam();

        private (int FeatureId, int WorkItemId) GivenABlockedDemoFeatureAndANeverBlockedWorkItemSharingAnId(
            SeededPortfolio portfolio, SeededTeamForPortfolio team)
        {
            // Seeded first into an empty WorkItems table and an empty Features table: both identity
            // sequences produce Id = 1, which is exactly the collision the demo backfill exploits.
            var workItemId = SeedWorkItem(team.TeamId, "WI-1", state: "In Progress");
            var featureId = SeedFeature(portfolio.PortfolioId, "F-1", state: "Blocked");

            Assert.That(featureId, Is.EqualTo(workItemId),
                $"Precondition broken: the scenario requires Feature.Id == WorkItem.Id (got feature {featureId}, work item {workItemId}). " +
                "If seeders now pre-populate these tables, re-derive the collision instead of assuming Id 1.");

            return (featureId, workItemId);
        }

        private void GivenABlockedDemoFeatureAndAnIdBeyondEveryWorkItem(SeededPortfolio portfolio)
        {
            // Work item Id = 1, feature Ids = 1 and 2: feature Id 2 has NO matching work item, so its
            // transition write violates the enforced FK on the broken code and the dispatcher swallows
            // the DbUpdateException — aborting BackfillAsync before the snapshot save.
            var team = SeedTeam();
            SeedWorkItem(team.TeamId, "WI-1", state: "In Progress");
            SeedFeature(portfolio.PortfolioId, "F-1", state: "Blocked");
            SeedFeature(portfolio.PortfolioId, "F-2", state: "Blocked");
        }

        // --- When ---

        private async Task WhenTheDemoPortfolioRefreshes(SeededPortfolio portfolio)
            => await DispatchPortfolioFeaturesRefreshed(portfolio.PortfolioId);

        // --- Then ---

        private async Task ThenTheWorkItemReadsNotBlockedOnAPastRange(SeededTeamForPortfolio team, int workItemId)
        {
            var (status, body) = await GetTeamWip(team.TeamId, PastRangeDate);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);

            var item = ItemByReference(body, "WI-1");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.TryGetProperty("isBlocked", out var isBlocked), Is.True, $"wip item must expose isBlocked. Item: {item}");
                Assert.That(isBlocked.GetBoolean(), Is.False,
                    $"A work item that was never blocked must not read blocked on a past range because a demo Feature shares its id. Item: {item}");
            }
        }

        private async Task ThenTheTeamDrillThroughExcludesTheWorkItem(SeededTeamForPortfolio team, int workItemId)
        {
            var (status, body) = await GetTeamBlockedItemsAtDate(team.TeamId, DateOnly.FromDateTime(PastRangeDate));
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);

            var references = ReferencesIn(body);
            Assert.That(references, Has.No.Member("WI-1"),
                $"blockedItemsAtDate must exclude the colliding id (the spell belongs to a demo Feature, not to this work item). Body: {body}");
        }

        private void ThenTheTeamKeyspaceHasNoSpells()
        {
            var teamSpells = ReadAllWorkItemSpells();
            Assert.That(teamSpells, Is.Empty,
                "A portfolio refresh must not write any blocked spell into the team keyspace (WorkItemBlockedTransition). " +
                $"Found {teamSpells.Count} row(s) — the demo backfill still writes Feature.Id values into the team table.");
        }

        private void ThenThePortfolioHasBackdatedSnapshots(SeededPortfolio portfolio)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var snapshots = ReadBlockedCountSnapshots(portfolio.PortfolioId, Lighthouse.Backend.Models.OwnerType.Portfolio);
            Assert.That(
                snapshots.Exists(s => s.RecordedAt < today), Is.True,
                "The demo snapshot backfill must land backdated portfolio BlockedCountSnapshot rows even when a demo Feature id " +
                "collides with no WorkItem. On the broken code the enforced FK aborts the backfill before the snapshot save — " +
                "this assertion is only meaningful on SQLite (EF InMemory does not enforce FKs).");
        }

        private async Task ThenThePortfolioTrendShowsBackdatedBlockedCounts(SeededPortfolio portfolio)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            var (status, body) = await GetPortfolioBlockedCountHistory(
                portfolio.PortfolioId, today.AddDays(-15), today);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);

            using var document = JsonDocument.Parse(body);
            var sawBackdatedPositiveCount = false;
            foreach (var point in document.RootElement.EnumerateArray())
            {
                if (point.TryGetProperty("blockedCount", out var count)
                    && point.TryGetProperty("recordedAt", out var recordedAt)
                    && count.GetInt32() > 0
                    && recordedAt.GetDateTime().Date < DateTime.UtcNow.Date)
                {
                    sawBackdatedPositiveCount = true;
                }
            }

            Assert.That(sawBackdatedPositiveCount, Is.True,
                $"The demo portfolio blocked trend must keep its backdated history (snapshot backfill retained). Body: {body}");
        }
    }
}
