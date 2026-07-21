using System.Net;
using System.Text.Json;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 03 / US-03 — honest historic blocked count.
    /// Backend-observable contract: with asOfDate in the past, the portfolio wip read answers from the
    /// captured FEATURE-keyspace spells (not from replaying today's rules), gated by the same
    /// isHistoricRange guard shape the team read uses; blockedSince reports the covering spell's
    /// EnteredAt. The docs-caveat removal (US-03 AC5, D6) is verified by grep at slice close, not by a
    /// test — see distill/wave-decisions.md.
    /// </summary>
    public partial class Slice03HistoricPortfolioBlockedCountTest : PortfolioBlockedHistoryAcceptanceTest
    {
        // --- Given ---

        private SeededPortfolio GivenAPortfolioWhoseRulesBlockAFeatureState()
            => SeedPortfolio(blockedOnState: true, blockedState: "Blocked");

        private SeededTeamForPortfolio GivenATeam()
            => SeedTeam();

        private int GivenAFeatureThatIsNotBlockedToday(SeededPortfolio portfolio, string referenceId)
            => SeedFeature(portfolio.PortfolioId, referenceId, state: "In Progress");

        private int GivenAFeatureBlockedToday(SeededPortfolio portfolio, string referenceId)
            => SeedFeature(portfolio.PortfolioId, referenceId, state: "Blocked");

        private int GivenAWorkItemThatIsNotBlockedToday(SeededTeamForPortfolio team, string referenceId)
            => SeedWorkItem(team.TeamId, referenceId, state: "In Progress");

        private void GivenAClosedBlockedSpellCovering(int featureId, SeededPortfolio portfolio, DateTime enteredAt, DateTime leftAt)
            => SeedFeatureBlockedSpell(featureId, portfolio.PortfolioId, enteredAt, leftAt);

        private void GivenAnOpenBlockedSpellThatBegan(int featureId, SeededPortfolio portfolio, DateTime enteredAt)
            => SeedFeatureBlockedSpell(featureId, portfolio.PortfolioId, enteredAt, leftAt: null);

        private void GivenAMirroredTeamSpellCovering(int workItemId, DateTime enteredAt, DateTime leftAt)
            => SeedWorkItemBlockedSpell(workItemId, enteredAt, leftAt);

        // --- When ---

        private async Task<JsonElement> WhenTheLeadReadsThePortfolioWipAt(SeededPortfolio portfolio, DateTime asOfDate)
        {
            var (status, body) = await GetPortfolioWip(portfolio.PortfolioId, asOfDate);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return FirstItem(body);
        }

        private async Task<JsonElement> WhenTheLeadReadsTheTeamWipAt(SeededTeamForPortfolio team, DateTime asOfDate)
        {
            var (status, body) = await GetTeamWip(team.TeamId, asOfDate);
            Assert.That(status, Is.EqualTo(HttpStatusCode.OK), body);
            return FirstItem(body);
        }

        // --- Then ---

        private void ThenTheFeatureReadsBlockedWithTheSpellStart(JsonElement item)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                    $"A feature whose blocked spell covers the selected range must read blocked even though it is clear today. Item: {item}");
                Assert.That(item.GetProperty("blockedSince").GetDateTime(),
                    Is.EqualTo(SyncDay.AddDays(-9)).Within(TimeSpan.FromSeconds(1)),
                    $"blockedSince on a historic read must be the covering spell's EnteredAt, not a live value. Item: {item}");
            }
        }

        private void ThenTheFeatureReadsBlocked(JsonElement item)
        {
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.True,
                $"The feature must read blocked for this range. Item: {item}");
        }

        private void ThenTheFeatureReadsNotBlocked(JsonElement item)
        {
            Assert.That(item.GetProperty("isBlocked").GetBoolean(), Is.False,
                $"The feature must read NOT blocked for this range — no spell covers it, and absence of a spell is evidence, not a gap. Item: {item}");
        }

        private void ThenBothReadsAgreeOnBlockedAndBlockedSince(JsonElement featureItem, JsonElement workItem, DateTime enteredAt)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(featureItem.GetProperty("isBlocked").GetBoolean(), Is.True,
                    $"Portfolio read must agree with the team read for the same spell shape. Feature: {featureItem}");
                Assert.That(workItem.GetProperty("isBlocked").GetBoolean(), Is.True,
                    $"Team read must report the mirrored spell blocked (shipped #5508 behaviour). WorkItem: {workItem}");
                Assert.That(featureItem.GetProperty("blockedSince").GetDateTime(),
                    Is.EqualTo(enteredAt).Within(TimeSpan.FromSeconds(1)),
                    $"Portfolio blockedSince must equal the spell EnteredAt, exactly as the team read reports. Feature: {featureItem}");
                Assert.That(workItem.GetProperty("blockedSince").GetDateTime(),
                    Is.EqualTo(enteredAt).Within(TimeSpan.FromSeconds(1)),
                    $"Team blockedSince must equal the spell EnteredAt (shipped #5508 behaviour). WorkItem: {workItem}");
            }
        }

        private static JsonElement FirstItem(string body)
        {
            using var document = JsonDocument.Parse(body);
            var clone = document.RootElement.Clone();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(clone.ValueKind, Is.EqualTo(JsonValueKind.Array), $"Expected an item array. Body: {body}");
                Assert.That(clone.GetArrayLength(), Is.EqualTo(1), $"Expected exactly one item in the read surface. Body: {body}");
            }

            return clone[0];
        }
    }
}
