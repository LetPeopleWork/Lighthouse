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
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The blockedItemsAtDate endpoint must serve the reconstructed item set. Body: {response.Body}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"blockedItemsAtDate must return a JSON array of work items, not HTML/other — the endpoint appears unimplemented. Body starts: {response.Body[..Math.Min(60, response.Body.Length)]}");

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
    }
}
