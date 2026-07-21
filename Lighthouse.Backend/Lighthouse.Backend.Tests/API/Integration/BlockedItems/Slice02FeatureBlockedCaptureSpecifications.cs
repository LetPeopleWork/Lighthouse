using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 02 / US-02 — feature blocked-spell capture
    /// and blocked duration on the portfolio wip read. Backend-observable contract: a feature whose
    /// blocked verdict flips across two refreshes surfaces a non-null blockedSince (and clears it on
    /// the leave edge), first observation stays "—", and the per-portfolio verdict no longer leaks
    /// across portfolios (ADR-103). The "blocked Nd" badge copy, RAG chip and blocked->stale treatment
    /// are rendered by the shared frontend components (US-02 AC5/AC6 — no portfolio branch); they are
    /// FE concerns covered by the existing epic-5074 Vitest suite, not new backend behaviour.
    /// </summary>
    public partial class Slice02FeatureBlockedCaptureTest : PortfolioBlockedHistoryAcceptanceTest
    {
        // --- Given ---

        private SeededPortfolio GivenAPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, blockedState: "Blocked");

        private SeededPortfolio GivenAPortfolioWhoseRulesBlockNothing()
            => SeedPortfolio(blockedOnState: false);

        private SeededTeamForPortfolio GivenATeam()
            => SeedTeam();

        private int GivenASharedFeature(
            SeededPortfolio portfolioA, SeededPortfolio portfolioB, string referenceId, SeededTeamForPortfolio? team = null)
        {
            var featureId = SeedFeature(
                portfolioA.PortfolioId, referenceId, state: "In Progress",
                additionalPortfolioId: portfolioB.PortfolioId);

            if (team.HasValue)
            {
                SeedFeatureWork(featureId, team.Value.TeamId);

                // The scope-free team feature list is driven by the team's active work items (their
                // ParentReferenceId), not by the forecast FeatureWork link — seed a parented WIP item so
                // the shared feature actually surfaces on that list (the precondition the guard asserts on).
                SeedWorkItem(team.Value.TeamId, $"WI-{referenceId}", "In Progress", parentReferenceId: referenceId);
            }

            return featureId;
        }

        private async Task GivenTheFeatureWasObservedNotBlocked(SeededPortfolio portfolio, string referenceId)
            => await DrivePortfolioRefresh(portfolio.PortfolioId, [ConnectorFeature(referenceId, "In Progress")]);

        private async Task GivenTheFeatureWasObservedBlocked(SeededPortfolio portfolio, string referenceId)
        {
            await GivenTheFeatureWasObservedNotBlocked(portfolio, referenceId);
            await DrivePortfolioRefresh(portfolio.PortfolioId, [ConnectorFeature(referenceId, "Blocked")]);
        }

        // --- When ---

        private async Task WhenThePortfolioRefreshesAndTheFeatureNowMatchesTheBlockedRules(
            SeededPortfolio portfolio, string referenceId)
            => await DrivePortfolioRefresh(portfolio.PortfolioId, [ConnectorFeature(referenceId, "Blocked")]);

        private async Task WhenThePortfolioRefreshesAndTheFeatureNoLongerMatchesTheBlockedRules(
            SeededPortfolio portfolio, string referenceId)
            => await DrivePortfolioRefresh(portfolio.PortfolioId, [ConnectorFeature(referenceId, "In Progress")]);

        private async Task WhenBothPortfoliosRefresh(SeededPortfolio portfolioA, SeededPortfolio portfolioB)
        {
            await DrivePortfolioRefresh(portfolioA.PortfolioId, [ConnectorFeature("F-SHARED", "Blocked")]);
            await DrivePortfolioRefresh(portfolioB.PortfolioId, [ConnectorFeature("F-SHARED", "Blocked")]);
        }

        // --- Then ---

        private async Task ThenTheFeatureExposesABlockedDuration(SeededPortfolio portfolio, string referenceId)
        {
            var item = await GetFeatureFromWip(portfolio, referenceId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.TryGetProperty("isBlocked", out var isBlocked), Is.True, $"wip feature must expose isBlocked. Item: {item}");
                Assert.That(isBlocked.GetBoolean(), Is.True, $"A feature matching its portfolio's blocked rules must read blocked. Item: {item}");
                Assert.That(item.TryGetProperty("blockedSince", out var blockedSince), Is.True, $"wip feature must expose blockedSince. Item: {item}");
                Assert.That(blockedSince.ValueKind, Is.EqualTo(JsonValueKind.String),
                    $"A feature that became blocked must expose the captured spell's enter timestamp as blockedSince. Item: {item}");
            }
        }

        private async Task ThenTheFeatureExposesNoBlockedDuration(SeededPortfolio portfolio, string referenceId)
        {
            var item = await GetFeatureFromWip(portfolio, referenceId);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.TryGetProperty("blockedSince", out var blockedSince), Is.True, $"blockedSince must be present on the contract even when null. Item: {item}");
                Assert.That(blockedSince.ValueKind, Is.EqualTo(JsonValueKind.Null),
                    $"A feature with no open blocked spell must surface blockedSince as null, never a fabricated value. Item: {item}");
            }
        }

        private async Task ThenTheFeatureReadsBlockedOn(SeededPortfolio portfolio, string referenceId)
        {
            var item = await GetFeatureFromWip(portfolio, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                $"The feature must read blocked on the portfolio whose rules match it. Item: {item}");
        }

        private async Task ThenTheFeatureReadsNotBlockedOn(SeededPortfolio portfolio, string referenceId)
        {
            var item = await GetFeatureFromWip(portfolio, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.False,
                $"A feature blocked only by ANOTHER portfolio's rules must NOT render blocked here (ADR-103 per-portfolio semantics). Item: {item}");
        }

        private async Task ThenTheTeamFeatureListReportsTheFeatureBlocked(SeededTeamForPortfolio team, string referenceId)
        {
            var (status, body) = await GetTeamFeaturesInProgress(team.TeamId, DateTime.UtcNow);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);

            var item = ItemByReference(body, referenceId);
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                $"The scope-free team feature list must keep reporting a feature blocked when any portfolio blocks it (Any read projection). Item: {item}");
        }

        private async Task<JsonElement> GetFeatureFromWip(SeededPortfolio portfolio, string referenceId)
        {
            var (status, body) = await GetPortfolioWip(portfolio.PortfolioId, DateTime.UtcNow);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return ItemByReference(body, referenceId);
        }
    }
}
