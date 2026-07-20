using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 05 / US-05 — demo portfolio blocked history.
    /// Backend-observable contract: on a demo-gated portfolio refresh, backdated feature blocked spells
    /// are synthesized into the FEATURE keyspace (never the team keyspace), idempotently, with the
    /// StartedDate cap preserved, and the result is drillable through the slice-04 read port. Every
    /// assertion is on ROWS or read surfaces, never on absence-of-throw — the dispatcher swallows
    /// handler exceptions.
    /// </summary>
    public partial class Slice05DemoPortfolioBlockedHistoryTest : PortfolioBlockedHistoryAcceptanceTest
    {
        // --- Given ---

        private SeededPortfolio GivenADemoPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, demoConnection: true, blockedState: "Blocked");

        private SeededPortfolio GivenANonDemoPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, demoConnection: false, blockedState: "Blocked");

        private SeededTeamForPortfolio GivenATeam()
            => SeedTeam();

        private void GivenACollidingWorkItemPair(SeededTeamForPortfolio team)
        {
            // Seeded first into an empty WorkItems table: ids 1 and 2, colliding with the two features
            // seeded next — so the broken handler's Feature.Id writes actually land in the team table.
            SeedWorkItem(team.TeamId, "WI-1", state: "In Progress");
            SeedWorkItem(team.TeamId, "WI-2", state: "In Progress");
        }

        private void GivenTwoBlockedDemoFeatures(SeededPortfolio portfolio)
        {
            SeedFeature(portfolio.PortfolioId, "F-DEMO-1", state: "Blocked", startedDate: SyncDay.AddDays(-30));
            SeedFeature(portfolio.PortfolioId, "F-DEMO-2", state: "Blocked", startedDate: SyncDay.AddDays(-30));
        }

        private void GivenARecentlyStartedBlockedDemoFeature(SeededPortfolio portfolio)
        {
            // Started 3 days ago: the 14-day spread would claim an EnteredAt before the feature existed —
            // the StartedDate cap must clamp it.
            SeedFeature(portfolio.PortfolioId, "F-DEMO-YOUNG", state: "Blocked",
                startedDate: DateTime.UtcNow.Date.AddDays(-3));
        }

        private void GivenBackdatedSnapshotsButNoFeatureSpells(SeededPortfolio portfolio)
        {
            // The post-slice-01 state on a long-lived demo instance: snapshot backfill landed (slice 01
            // retained it) but no feature spells exist yet. The legacy snapshot-based idempotency guard
            // sees "already backfilled" — the reconciled guard must still synthesize the spells.
            var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
            for (var day = 14; day >= 1; day--)
            {
                SeedBlockedCountSnapshot(portfolio.PortfolioId, Lighthouse.Backend.Models.OwnerType.Portfolio, today.AddDays(-day), blockedCount: 2);
            }
        }

        // --- When ---

        private async Task WhenTheDemoPortfolioRefreshes(SeededPortfolio portfolio)
            => await DispatchPortfolioFeaturesRefreshed(portfolio.PortfolioId);

        // --- Then ---

        private void ThenTheFeatureKeyspaceHoldsBackdatedSpellsForBothFeatures(SeededPortfolio portfolio)
        {
            var spells = ReadFeatureSpells(portfolio.PortfolioId);
            var today = DateTime.UtcNow.Date;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spells, Has.Count.EqualTo(2),
                    "The demo backfill must synthesize one backdated spell per blocked demo feature into the FEATURE keyspace.");
                Assert.That(spells.TrueForAll(s => s.EnteredAt.Date <= today && s.EnteredAt.Date >= today.AddDays(-15)), Is.True,
                    $"Backdated spells must spread across the demo history window. Spells: {string.Join(", ", spells.Select(s => s.EnteredAt))}");
            }
        }

        private void ThenTheTeamKeyspaceHasNoSpells()
        {
            var teamSpells = ReadAllWorkItemSpells();
            Assert.That(teamSpells, Is.Empty,
                "The demo backfill must never write into the team keyspace (WorkItemBlockedTransition) — slice 01's invariant. " +
                $"Found {teamSpells.Count} row(s).");
        }

        private void ThenTheFeatureKeyspaceIsUnchangedAndNonEmpty(SeededPortfolio portfolio, List<Lighthouse.Backend.Models.FeatureBlockedTransition> spellsAfterFirstRefresh)
        {
            var spellsAfterSecondRefresh = ReadFeatureSpells(portfolio.PortfolioId);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spellsAfterFirstRefresh, Is.Not.Empty,
                    "Precondition: the first refresh must synthesize spells before idempotency can mean anything.");
                Assert.That(spellsAfterSecondRefresh, Has.Count.EqualTo(spellsAfterFirstRefresh.Count),
                    $"A second demo refresh must add no spells (idempotent). First: {spellsAfterFirstRefresh.Count}, second: {spellsAfterSecondRefresh.Count}.");
            }
        }

        private void ThenSpellsExistAndEverySpellStartsOnOrAfterItsFeatureStarted(SeededPortfolio portfolio)
        {
            var spells = ReadFeatureSpells(portfolio.PortfolioId);
            var featureStarted = DateTime.UtcNow.Date.AddDays(-3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(spells, Is.Not.Empty,
                    "The demo backfill must synthesize a spell for the recently-started blocked feature before the cap can mean anything.");
                Assert.That(spells.TrueForAll(s => s.EnteredAt.Date >= featureStarted), Is.True,
                    $"No backdated spell may predate its feature's start (StartedDate cap). Spells: {string.Join(", ", spells.Select(s => s.EnteredAt))}");
            }
        }

        private async Task ThenYesterdaysBarListsTheBackfilledFeatures(SeededPortfolio portfolio)
        {
            var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(-1));
            var (status, body) = await GetPortfolioBlockedItemsAtDate(portfolio.PortfolioId, yesterday);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);

            var references = ReferencesIn(body);
            Assert.That(references, Is.Not.Empty,
                $"A backdated bar on the demo portfolio chart must drill into the features blocked that day. Body: {body}");
        }

        private void ThenTheFeatureKeyspaceIsEmpty(SeededPortfolio portfolio)
        {
            var spells = ReadFeatureSpells(portfolio.PortfolioId);
            Assert.That(spells, Is.Empty,
                $"A non-demo portfolio must gain no synthesized history — the demo gate is the whole safety argument. Found {spells.Count} row(s).");
        }
    }
}
