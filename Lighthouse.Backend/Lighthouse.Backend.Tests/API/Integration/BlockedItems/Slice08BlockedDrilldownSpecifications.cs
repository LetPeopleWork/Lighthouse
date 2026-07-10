using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BlockedItems
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Slice 08 / B1 — blocked-trend drill-through.
    /// Backend-observable contract: a NEW read endpoint reconstructs the set of items blocked at a given
    /// date from WorkItemBlockedTransition interval overlap (EnteredAt.Date &lt;= T AND (LeftAt is null OR
    /// LeftAt.Date &gt;= T)), joined to the team's work items (ADR-099). No persisted membership.
    /// </summary>
    public partial class Slice08BlockedDrilldownTest : BlockedItemsAcceptanceTest
    {
        // --- Given ---

        private SeededTeam GivenATeam()
            => SeedTeam(blockedStates: ["Blocked"]);

        /// <summary>
        /// Seed a work item plus a blocked spell [enteredAt, leftAt) on it. The item's live State is
        /// "Blocked" when the spell is still open (leftAt is null), else a non-blocked Doing state — so
        /// the latest-date reconstruction (live IsBlocked) and the historical reconstruction (intervals)
        /// are both exercisable from the same seed.
        /// </summary>
        private void GivenABlockedItemWithTransition(SeededTeam team, string referenceId, DateTime enteredAt, DateTime? leftAt)
        {
            SeedWorkItem(team.TeamId, referenceId, state: leftAt is null ? "Blocked" : "In Progress");

            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var item = workItemRepository.GetByPredicate(w => w.TeamId == team.TeamId && w.ReferenceId == referenceId)
                ?? throw new InvalidOperationException($"Seeded work item {referenceId} not found");

            var transitionRepository = sp.GetRequiredService<IWorkItemBlockedTransitionRepository>();
            transitionRepository.Add(new WorkItemBlockedTransition
            {
                WorkItemId = item.Id,
                EnteredAt = enteredAt,
                LeftAt = leftAt,
            });
            transitionRepository.Save().GetAwaiter().GetResult();
        }

        protected void GivenBlockedCountSnapshots(SeededTeam team, List<(DateOnly Date, int Count)> snapshots)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBlockedCountSnapshotRepository>();
            foreach (var (date, count) in snapshots)
            {
                repo.Add(new BlockedCountSnapshot
                {
                    OwnerId = team.TeamId,
                    OwnerType = OwnerType.Team,
                    RecordedAt = date,
                    BlockedCount = count,
                });
            }

            repo.Save().GetAwaiter().GetResult();
        }

        // --- When ---

        private async Task<(HttpStatusCode Status, string Body)> WhenTheCoachDrillsIntoTheBlockedTrendAt(SeededTeam team, DateOnly date)
        {
            Client.AsTeamAdmin(team.TeamId);
            var response = await Client.GetAsync(
                $"/api/latest/teams/{team.TeamId}/metrics/blockedItemsAtDate?date={date:yyyy-MM-dd}");
            return (response.StatusCode, await response.Content.ReadAsStringAsync());
        }

        // --- Then ---

        private static void ThenTheDialogListsExactly((HttpStatusCode Status, string Body) response, params string[] expectedReferenceIds)
        {
            var references = ParseReferenceIds(response);
            Assert.That(references, Is.EquivalentTo(expectedReferenceIds),
                $"blockedItemsAtDate must return exactly the items whose blocked interval covers the date. Body: {response.Body}");
        }

        private static void ThenTheDialogIsEmpty((HttpStatusCode Status, string Body) response)
        {
            var references = ParseReferenceIds(response);
            Assert.That(references, Is.Empty,
                $"A date with no covering blocked interval must return an empty set (empty dialog), never an error or fabricated history. Body: {response.Body}");
        }

        private static void ThenTheReconstructedCountMatchesTheSnapshot((HttpStatusCode Status, string Body) response, int expectedCount)
        {
            var references = ParseReferenceIds(response);
            Assert.That(references, Has.Count.EqualTo(expectedCount),
                $"The reconstructed membership count must reconcile with the BlockedCountSnapshot for that date. Body: {response.Body}");
        }

        /// <summary>
        /// Parse the response as a JSON array of work items and project the referenceId of each, failing
        /// with a clean RED assertion (not a raw parse exception) when the endpoint is missing and the
        /// request falls through to the SPA HTML fallback.
        /// </summary>
        private static List<string> ParseReferenceIds((HttpStatusCode Status, string Body) response)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                    $"The blockedItemsAtDate endpoint must serve the reconstructed item set. Body: {response.Body}");
                Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                    $"blockedItemsAtDate must return a JSON array of work items, not HTML/other — the endpoint appears unimplemented. Body starts: {response.Body[..Math.Min(60, response.Body.Length)]}");
            }

            using var document = JsonDocument.Parse(response.Body);
            var references = new List<string>();
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("referenceId", out var referenceId) && referenceId.ValueKind == JsonValueKind.String)
                {
                    references.Add(referenceId.GetString()!);
                }
            }

            return references;
        }

        // --- Reconciliation probe (SPIKE 08-01, ADR-099 de-risk gate) ---
        // Throwaway empirical probe: proves the interval-overlap reconstruction predicate yields a blocked
        // membership COUNT that reconciles (within +/-1) with the independently-seeded BlockedCountSnapshot
        // on a sampled past date, over real EF InMemory — BEFORE the blockedItemsAtDate endpoint is built.
        // GO  => reconstructed count within +/-1 of the snapshot count => scenario #24 reconcile helper is sound.
        // NO-GO => fall back to current-only click (only the latest bar interactive, historical bars marked
        //          non-interactive). No production code ships in this step.

        [Test]
        [Category("spike")]
        public void Reconciliation_probe_reconstructed_count_matches_snapshot_within_tolerance()
        {
            var team = GivenATeam();
            var sampledDate = DateOnly.FromDateTime(SyncDay.AddDays(-7));

            // PROBE-1 (open) and PROBE-2 (closed after the sampled date) both COVER the sampled date;
            // PROBE-CLOSED closed its spell before the sampled date and must NOT be reconstructed.
            GivenABlockedItemWithTransition(team, "PROBE-1", enteredAt: SyncDay.AddDays(-9), leftAt: null);
            GivenABlockedItemWithTransition(team, "PROBE-2", enteredAt: SyncDay.AddDays(-8), leftAt: SyncDay.AddDays(-5));
            GivenABlockedItemWithTransition(team, "PROBE-CLOSED", enteredAt: SyncDay.AddDays(-20), leftAt: SyncDay.AddDays(-15));

            // Independently-seeded snapshot for the sampled date (the reconciliation oracle).
            GivenBlockedCountSnapshots(team, [(sampledDate, 2)]);

            var reconstructedWorkItemIds = ReconstructBlockedWorkItemIdsAt(team, sampledDate);
            var snapshotCount = SnapshotCountAt(team, sampledDate);
            var reconciliationDelta = Math.Abs(reconstructedWorkItemIds.Count - snapshotCount);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reconstructedWorkItemIds, Has.Count.EqualTo(2),
                    "interval-overlap reconstruction (EnteredAt.Date <= T AND (LeftAt is null OR LeftAt.Date >= T)) must recover exactly the two spells covering the sampled date, excluding the spell closed before it");
                Assert.That(reconciliationDelta, Is.LessThanOrEqualTo(1),
                    $"GO gate: reconstructed count ({reconstructedWorkItemIds.Count}) must be within +/-1 of the BlockedCountSnapshot count ({snapshotCount}) for the sampled date; a wider gap is NO-GO -> current-only click (latest bar interactive, historical bars non-interactive)");
            }
        }

        /// <summary>
        /// Reconstruct the set of the team's work item ids that were blocked at <paramref name="date"/> by
        /// reading <see cref="WorkItemBlockedTransition"/> intervals directly from the repository and applying
        /// the ADR-099 interval-overlap predicate. This mirrors what the blockedItemsAtDate endpoint will do
        /// server-side; here it runs inside the probe to prove the predicate independently.
        /// </summary>
        private List<int> ReconstructBlockedWorkItemIdsAt(SeededTeam team, DateOnly date)
        {
            using var scope = Factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var teamWorkItemIds = workItemRepository
                .GetAllByPredicate(w => w.TeamId == team.TeamId)
                .Select(w => w.Id)
                .ToHashSet();

            var transitionRepository = sp.GetRequiredService<IWorkItemBlockedTransitionRepository>();
            return transitionRepository
                .GetAll()
                .Where(t => teamWorkItemIds.Contains(t.WorkItemId))
                .Where(t => DateOnly.FromDateTime(t.EnteredAt) <= date
                            && (t.LeftAt is null || DateOnly.FromDateTime(t.LeftAt.Value) >= date))
                .Select(t => t.WorkItemId)
                .Distinct()
                .ToList();
        }

        private int SnapshotCountAt(SeededTeam team, DateOnly date)
        {
            using var scope = Factory.Services.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IBlockedCountSnapshotRepository>();
            var snapshot = repo.GetByPredicate(s =>
                    s.OwnerId == team.TeamId && s.OwnerType == OwnerType.Team && s.RecordedAt == date)
                ?? throw new InvalidOperationException($"No BlockedCountSnapshot seeded for {date:yyyy-MM-dd}");
            return snapshot.BlockedCount;
        }
    }
}
