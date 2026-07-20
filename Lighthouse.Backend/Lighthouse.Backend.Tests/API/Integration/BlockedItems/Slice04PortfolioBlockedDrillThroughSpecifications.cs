using System.Net;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 04 / US-04 — portfolio blocked drill-through.
    /// Backend-observable contract: blockedItemsAtDate reconstructs past membership from FEATURE-keyspace
    /// spell intervals (half-open overlap: EnteredAt &lt; startOfNextDate AND (LeftAt is null OR
    /// LeftAt &gt;= startOfDate)), keeps the live branch for today, and serves an honest partial set for
    /// pre-capture dates. The obsolete "reconstruction is impossible" comment removal (US-04 AC5) and
    /// the ADR-099 guard's log emission are DELIVER code-review / inner-loop verifications.
    /// </summary>
    public partial class Slice04PortfolioBlockedDrillThroughTest : PortfolioBlockedHistoryAcceptanceTest
    {
        // --- Given ---

        private SeededPortfolio GivenAPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, blockedState: "Blocked");

        private int GivenAFeatureWithASpellCovering(SeededPortfolio portfolio, string referenceId, DateTime enteredAt, DateTime? leftAt)
        {
            var featureId = SeedFeature(portfolio.PortfolioId, referenceId, state: "In Progress");
            SeedFeatureBlockedSpell(featureId, portfolio.PortfolioId, enteredAt, leftAt);
            return featureId;
        }

        private int GivenAFeatureInState(SeededPortfolio portfolio, string referenceId, string state)
            => SeedFeature(portfolio.PortfolioId, referenceId, state: state);

        private void GivenACapturedSnapshotOf(SeededPortfolio portfolio, DateOnly date, int blockedCount)
            => SeedBlockedCountSnapshot(portfolio.PortfolioId, Lighthouse.Backend.Models.OwnerType.Portfolio, date, blockedCount);

        private void GivenADepartedFeatureWithASpellCovering(SeededPortfolio portfolio, string referenceId, DateTime enteredAt, DateTime? leftAt)
        {
            var departedFeatureId = SeedStandaloneFeature(referenceId, state: "In Progress");
            SeedFeatureBlockedSpell(departedFeatureId, portfolio.PortfolioId, enteredAt, leftAt);
        }

        // --- When ---

        private async Task<(HttpStatusCode Status, string Body)> WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(
            SeededPortfolio portfolio, DateOnly date)
        {
            var (status, body) = await GetPortfolioBlockedItemsAtDate(portfolio.PortfolioId, date);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return (status, body);
        }

        private async Task<(HttpStatusCode Status, string Body)> WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(
            SeededPortfolio portfolio, DateTime date)
            => await WhenTheCoachDrillsIntoThePortfolioBlockedTrendAt(portfolio, DateOnly.FromDateTime(date));

        // --- Then ---

        private static void ThenTheDialogListsExactly((HttpStatusCode Status, string Body) response, params string[] referenceIds)
        {
            var references = ReferencesIn(response.Body);
            Assert.That(references, Is.EquivalentTo(referenceIds),
                $"blockedItemsAtDate must return exactly the features whose blocked interval covers the date. Body: {response.Body}");
        }

        private static void ThenTheDialogIsEmpty((HttpStatusCode Status, string Body) response)
        {
            var references = ReferencesIn(response.Body);
            Assert.That(references, Is.Empty,
                $"blockedItemsAtDate must return an honest empty set here — never a fabricated membership. Body: {response.Body}");
        }

        private static void ThenTheReconstructedCountIs((HttpStatusCode Status, string Body) response, int expectedCount)
        {
            var references = ReferencesIn(response.Body);
            Assert.That(references, Has.Count.EqualTo(expectedCount),
                $"The reconstructed membership count must equal the captured BlockedCountSnapshot for the same date, " +
                $"so the ADR-099 reconciliation guard stays quiet. Body: {response.Body}");
        }
    }
}
