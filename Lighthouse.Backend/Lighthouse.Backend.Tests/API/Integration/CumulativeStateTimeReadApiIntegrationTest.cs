using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    [TestFixture]
    [NonParallelizable]
    public class CumulativeStateTimeReadApiIntegrationTest
    {
        private const string InProgress = "In Progress";
        private const string Review = "Review";
        private const string Test = "Test";
        private const string Done = "Done";

        private const double DaysTolerance = 0.1;

        private static readonly string[] WorkflowDoingStates = [InProgress, Review, Test];
        private static readonly double[] ReviewDrillDownDaysDescending = [30.0, 20.0, 10.0];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);

            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            var seeders = setupScope.ServiceProvider.GetServices<ISeeder>();
            foreach (var seeder in seeders)
            {
                seeder.Seed().GetAwaiter().GetResult();
            }
        }

        [TearDown]
        public void Cleanup()
        {
            using (var teardownScope = factory.Services.CreateScope())
            {
                var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
                dbContext.Database.EnsureDeleted();
            }

            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [Test]
        public async Task GetCumulativeStateTime_TeamWithKnownVisits_ReturnsOneBarPerWorkflowStateInWorkflowOrder()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var orderedStates = OrderedStateNames(body);
                Assert.That(orderedStates, Is.EqualTo(WorkflowDoingStates),
                    $"D19: bars must be returned one-per-Doing-state in workflow order; no Done-category state (e.g. {Done}) appears as a bar. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_ItemEnteredStateBeforeWindow_CountsFullUnclippedDuration()
        {
            // D5 full-duration attribution: an item that entered Review 30 days before the window
            // and exited 20 days into the window contributes its FULL 50 days to the Review bar — not 20.
            var teamId = SeedTeamWithSingleStraddlingItem();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var review = StateRow(body, Review);
                Assert.That(review.TotalDays, Is.EqualTo(50.0).Within(DaysTolerance),
                    $"D5: full unclipped Review duration (30 pre-window + 20 in-window = 50d) must be counted, not the 20d clipped portion. Body: {body}");
                Assert.That(review.CompletedContributionDays, Is.EqualTo(50.0).Within(DaysTolerance),
                    $"The item exited Review, so its full 50d lands in the completed segment. Body: {body}");
                Assert.That(review.OngoingContributionDays, Is.Zero.Within(DaysTolerance),
                    $"No item is currently in Review, so the ongoing segment is empty. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_KnownFixture_ReturnsExactTotalsAndCompletedOngoingSegmentHeightsPerState()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var inProgress = StateRow(body, InProgress);
                var review = StateRow(body, Review);
                var test = StateRow(body, Test);

                // In Progress: three completed items (5 + 5 + 10 = 20d) + one in-flight item (40d ongoing).
                Assert.That(inProgress.CompletedContributionDays, Is.EqualTo(20.0).Within(DaysTolerance), $"In Progress completed segment. Body: {body}");
                Assert.That(inProgress.OngoingContributionDays, Is.EqualTo(40.0).Within(DaysTolerance), $"In Progress ongoing segment (one item in flight 40d). Body: {body}");
                Assert.That(inProgress.TotalDays, Is.EqualTo(60.0).Within(DaysTolerance), $"In Progress total = completed + ongoing. Body: {body}");

                // Review: three completed visits (10 + 20 + 30 = 60d), no ongoing.
                Assert.That(review.CompletedContributionDays, Is.EqualTo(60.0).Within(DaysTolerance), $"Review completed segment. Body: {body}");
                Assert.That(review.OngoingContributionDays, Is.Zero.Within(DaysTolerance), $"Review ongoing segment empty. Body: {body}");
                Assert.That(review.TotalDays, Is.EqualTo(60.0).Within(DaysTolerance), $"Review total. Body: {body}");

                // Test: two completed visits (15 + 15 = 30d) + one item in flight in Test (25d ongoing).
                Assert.That(test.CompletedContributionDays, Is.EqualTo(30.0).Within(DaysTolerance), $"Test completed segment. Body: {body}");
                Assert.That(test.OngoingContributionDays, Is.EqualTo(25.0).Within(DaysTolerance), $"Test ongoing segment (one item in flight 25d). Body: {body}");
                Assert.That(test.TotalDays, Is.EqualTo(55.0).Within(DaysTolerance), $"Test total. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_KnownFixture_TooltipCountFieldsPresentPerState()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var inProgress = StateRow(body, InProgress);
                Assert.That(inProgress.CompletedItemCount, Is.EqualTo(3),
                    $"US-01 tooltip: completedItemCount for In Progress (three items exited it). Body: {body}");
                Assert.That(inProgress.OngoingItemCount, Is.EqualTo(1),
                    $"US-01 tooltip: ongoingItemCount for In Progress (one item currently there). Body: {body}");
                Assert.That(inProgress.ItemCount, Is.GreaterThan(0),
                    $"US-01 tooltip: distinct contributing item count present. Body: {body}");
                Assert.That(inProgress.MeanDays, Is.GreaterThan(0.0),
                    $"US-01 tooltip: meanDays present. Body: {body}");
                Assert.That(inProgress.HasMedianDays, Is.True,
                    $"US-01 tooltip: medianDays field present in the payload. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_NoItemsMatchFilter_ReturnsEmptyStatesArray()
        {
            var teamId = SeedTeamWithNoItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(StatesArrayLength(body), Is.Zero,
                    $"Empty filter (no items match) yields an empty states array (DDD-9). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_StateWithZeroContributingItems_RendersPlaceholderBarWithZeroHeight()
        {
            // Review has no visits in this fixture but is part of the workflow definition,
            // so it must still be emitted with totalDays 0 (placeholder bar, DDD-9).
            var teamId = SeedTeamWhereReviewHasNoContribution();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var orderedStates = OrderedStateNames(body);
                Assert.That(orderedStates, Does.Contain(Review),
                    $"A zero-contributing workflow state must still appear as a placeholder bar, not be dropped. Body: {body}");

                var review = StateRow(body, Review);
                Assert.That(review.TotalDays, Is.Zero.Within(DaysTolerance),
                    $"The zero-contributing Review bar has height 0 (DDD-9). Body: {body}");
                Assert.That(review.MedianDays, Is.Null,
                    $"A state with no completed visits has a null median (the empty-visit guard returns null). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_ItemEntirelyOutsideWindow_IsExcluded()
        {
            // D12: an item closed before windowStart with no state-time overlap is NOT included.
            var teamId = SeedTeamWhereOnlyItemIsEntirelyBeforeWindow();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(TotalDaysAcrossAllStates(body), Is.Zero.Within(DaysTolerance),
                    $"D12: an item entirely outside the window contributes nothing to any bar. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_ItemCurrentlyInToDoWithEarlierDoingVisit_IsExcludedWhileDoneItemRemains()
        {
            // D20: an item that bounced back into a To Do-category state is excluded entirely — even though
            // it had a Doing visit inside the window — and contributes to no bar. An item currently in a
            // Done-category state stays included and its historical Doing time still contributes.
            var teamId = SeedTeamWithBouncedBackToDoItemAndOneDoneItem(out var doneReferenceId, out var bouncedReferenceId);

            client.AsTeamAdmin(teamId);
            var barResponse = await client.GetAsync(BarUrl(teamId));
            var candidatesResponse = await client.GetAsync(CandidatesUrl(teamId));
            var itemsResponse = await client.GetAsync(ItemsUrl(teamId, Review));

            var barBody = await barResponse.Content.ReadAsStringAsync();
            var candidatesBody = await candidatesResponse.Content.ReadAsStringAsync();
            var itemsBody = await itemsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(barResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), barBody);
                Assert.That(candidatesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), candidatesBody);
                Assert.That(itemsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), itemsBody);

                var candidateReferenceIds = CandidateReferenceIds(candidatesBody);
                Assert.That(candidateReferenceIds, Does.Not.Contain(bouncedReferenceId),
                    $"D20: an item currently in a To Do-category state must be absent from the candidate set. Body: {candidatesBody}");
                Assert.That(candidateReferenceIds, Does.Contain(doneReferenceId),
                    $"D20: a Done-category item stays included. Body: {candidatesBody}");

                var itemReferenceIds = ItemRows(itemsBody).Select(row => row.ReferenceId).ToArray();
                Assert.That(itemReferenceIds, Does.Not.Contain(bouncedReferenceId),
                    $"D20: the bounced-back-to-To Do item contributes to no drill-down row. Body: {itemsBody}");
                Assert.That(itemReferenceIds, Does.Contain(doneReferenceId),
                    $"D20: the Done item's historical Review time still contributes. Body: {itemsBody}");

                var review = StateRow(barBody, Review);
                Assert.That(review.TotalDays, Is.EqualTo(10.0).Within(DaysTolerance),
                    $"D20: the Review bar counts only the Done item's 10d, never the excluded item's earlier Doing time. Body: {barBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_StartDateAfterEndDate_ReturnsBadRequest()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime?startDate={windowEnd:O}&endDate={windowStart:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"startDate after endDate must be rejected with 400, mirroring cycleTimePercentiles validation (DDD-16). Body: {body}");
        }

        [Test]
        public async Task GetCumulativeStateTime_AnonymousCaller_IsRejected()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsAnonymous();
            var response = await client.GetAsync(BarUrl(teamId));

            Assert.That(
                new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, HttpStatusCode.NotFound },
                Does.Contain(response.StatusCode),
                $"An unauthenticated caller must not read team cumulative state time (RbacGuard TeamRead, DDD-17). Status: {response.StatusCode}");
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_KnownState_ReturnsPerItemRowsWithSharedDialogColumns()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(ItemsUrl(teamId, Review));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var rows = ItemRows(body);
                Assert.That(rows, Is.Not.Empty,
                    $"US-04 drill-down lists the items that contributed to Review. Body: {body}");

                var done1 = rows.Single(row => row.ReferenceId == "DONE-1");
                Assert.That(done1.WorkItemId, Is.GreaterThan(0),
                    $"D22: the items payload carries workItemId so the frontend can build IWorkItem rows. Body: {body}");
                Assert.That(done1.Title, Is.EqualTo("Story DONE-1"),
                    $"US-04 column 'Title' must carry the work item Name. Body: {body}");
                Assert.That(done1.Type, Is.EqualTo("Story"),
                    $"US-04 column 'Work-Item Type' must carry the work item Type. Body: {body}");
                Assert.That(done1.State, Is.EqualTo(Done),
                    $"US-04 column 'Current State' must carry the work item's current State. Body: {body}");
                Assert.That(done1.StateCategory, Is.EqualTo(nameof(StateCategories.Done)),
                    $"D22: the items payload carries the stateCategory name ('Done') for the colored chip. Body: {body}");
                Assert.That(done1.Url, Is.EqualTo(UrlFor("DONE-1")),
                    $"D22: the items payload carries the work item Url for the clickable name link. Body: {body}");
                Assert.That(done1.DaysContributed, Is.GreaterThan(0.0),
                    $"US-04 column 'Days Contributed' must carry the contribution. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_PerItemDaysContributed_SumsToBarTotalForThatState()
        {
            // US-04 AC: Σ daysContributed across drill-down rows == the bar's totalDays within ±0.1d.
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var barResponse = await client.GetAsync(BarUrl(teamId));
            var itemsResponse = await client.GetAsync(ItemsUrl(teamId, Review));

            var barBody = await barResponse.Content.ReadAsStringAsync();
            var itemsBody = await itemsResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(barResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), barBody);
                Assert.That(itemsResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), itemsBody);

                var barTotal = StateRow(barBody, Review).TotalDays;
                var perItemSum = SumDaysContributed(itemsBody);

                Assert.That(perItemSum, Is.EqualTo(barTotal).Within(DaysTolerance),
                    $"US-04: per-item daysContributed must sum to the Review bar height ({barTotal}d) within ±{DaysTolerance}d. Sum was {perItemSum}d. Bar: {barBody} Items: {itemsBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_MissingState_ReturnsBadRequest()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var url = $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime/items?startDate={windowStart:O}&endDate={windowEnd:O}";
            var response = await client.GetAsync(url);

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest),
                $"The drill-down endpoint requires a non-empty state (DDD-16). Body: {body}");
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_Window_ReturnsExactlyTheIncludedItemsAndExcludesOutOfWindowItem()
        {
            // D17: candidates are exactly the D12-included items for the window. An item outside
            // the window must NOT be findable in the picker.
            var teamId = SeedTeamWithKnownVisitsPlusOneOutOfWindowItem(out var outOfWindowReferenceId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                var candidateReferenceIds = CandidateReferenceIds(body);
                Assert.That(candidateReferenceIds, Is.Not.Empty,
                    $"US-05: the picker candidate set is the D12-included items for the window. Body: {body}");
                Assert.That(candidateReferenceIds, Does.Not.Contain(outOfWindowReferenceId),
                    $"D17: an item not touched in the window must be absent from the candidate set. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_Window_DoesNotExposeParentReferenceId()
        {
            // D21: parent-expand was dropped; the picker selects raw work items only, so the candidate
            // row no longer carries a parentReferenceId field.
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateRowsHaveParentReferenceId(body), Is.False,
                    $"D21: no candidate row exposes a parentReferenceId field. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_WithItemIdsSubset_SumsOverOnlySelectedItemsWithFullDurations()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();
            var selected = TwoCompletedReviewItemDbIds(teamId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrlWithItemIds(teamId, selected));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                // The two selected items contributed 10d + 20d to Review (their FULL durations, D5).
                var review = StateRow(body, Review);
                Assert.That(review.TotalDays, Is.EqualTo(30.0).Within(DaysTolerance),
                    $"US-05/DDD-20: with itemIds=[a,b] the Review bar sums ONLY a+b full durations (10+20=30d), not the whole set's 60d. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_RagSourceUnaffectedByItemIds_SystemicTotalsIdenticalWithAndWithoutSelection()
        {
            // D18: RAG reflects the whole in-scope set. The systemic (no-itemIds) response is the RAG
            // source and must be byte-for-shape identical regardless of any selection the client makes.
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();
            var selected = TwoCompletedReviewItemDbIds(teamId);

            client.AsTeamAdmin(teamId);
            var systemicResponse = await client.GetAsync(BarUrl(teamId));
            var narrowedResponse = await client.GetAsync(BarUrlWithItemIds(teamId, selected));

            var systemicBody = await systemicResponse.Content.ReadAsStringAsync();
            var narrowedBody = await narrowedResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(systemicResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), systemicBody);
                Assert.That(narrowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), narrowedBody);

                var systemicReviewTotal = StateRow(systemicBody, Review).TotalDays;
                var narrowedReviewTotal = StateRow(narrowedBody, Review).TotalDays;

                Assert.That(systemicReviewTotal, Is.EqualTo(60.0).Within(DaysTolerance),
                    $"D18: the systemic Review total (the RAG source) stays the whole-set 60d. Systemic: {systemicBody}");
                Assert.That(narrowedReviewTotal, Is.LessThan(systemicReviewTotal),
                    $"D18: the narrowed bars are a strict subset of the systemic bars, confirming the selection narrows the VIEW only, never the systemic RAG source. Narrowed: {narrowedBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_ItemEnteredFirstStateExactlyAtWindowEnd_IsIncluded()
        {
            // Boundary: an item whose only visit-entry lands exactly on windowEnd still intersects the window.
            // The inclusivity is `entry <= endDate`; a strict `<` (or pairing the entry against the wrong exit)
            // would drop the item.
            var teamId = SeedTeamWithSingleItemEnteringAtWindowEnd(out var boundaryReferenceId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"An item entering its first state exactly at windowEnd must intersect the window (entry <= endDate is inclusive). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_ItemExitedFirstStateExactlyAtWindowStart_IsIncluded()
        {
            // Boundary: an item whose only visit-exit lands exactly on windowStart still intersects the window.
            // The inclusivity is `exit >= startDate`; a strict `>` would drop the item.
            var teamId = SeedTeamWithSingleItemExitingAtWindowStart(out var boundaryReferenceId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"An item exiting its first state exactly at windowStart must intersect the window (exit >= startDate is inclusive). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_InFlightItemEnteredCurrentStateExactlyAtWindowEnd_IsIncluded()
        {
            // Boundary: an in-flight item with no transitions is included via the in-flight-at-window-end gate
            // when it entered its current state exactly on windowEnd (`CurrentStateEnteredAt <= endDate` is inclusive).
            var teamId = SeedTeamWithInFlightItemEnteringCurrentStateAtWindowEnd(out var boundaryReferenceId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(CandidateReferenceIds(body), Does.Contain(boundaryReferenceId),
                    $"An in-flight item entering its current state exactly at windowEnd must be in-flight-at-window-end (inclusive boundary). Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeCandidates_DoneItemWithCurrentTimestampButNoWindowOverlap_IsExcluded()
        {
            // The in-flight-at-window-end gate must AND together (a) not-Done, (b) has a current timestamp,
            // (c) timestamp <= endDate. A Done item carrying a current timestamp but no window-overlapping
            // transitions must be excluded — flipping the first conjunct to OR, or defaulting the no-overlap
            // path to "included", would wrongly surface it.
            var teamId = SeedTeamWithExcludedDoneItemCarryingCurrentTimestamp(out var excludedReferenceId, out var includedReferenceId);

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(CandidatesUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            var candidateReferenceIds = CandidateReferenceIds(body);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(candidateReferenceIds, Does.Contain(includedReferenceId),
                    $"The normal in-window item stays a candidate. Body: {body}");
                Assert.That(candidateReferenceIds, Does.Not.Contain(excludedReferenceId),
                    $"A Done item with a current timestamp but no window-overlapping transitions is not in-flight-at-window-end and must be excluded. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_ItemsWithSyncedTransitionsButMissingTimestamps_DoNotCrashTheEndpoints()
        {
            // Robustness: malformed items (synced transitions but no StartedDate; in-flight but no
            // CurrentStateEnteredAt) must be handled without throwing. A short-circuit relaxed to a
            // non-short-circuiting operator would dereference a null timestamp and 500 the request.
            var teamId = SeedTeamWithMalformedButRecoverableItems(out var healthyReferenceId);

            client.AsTeamAdmin(teamId);
            var barResponse = await client.GetAsync(BarUrl(teamId));
            var candidatesResponse = await client.GetAsync(CandidatesUrl(teamId));

            var barBody = await barResponse.Content.ReadAsStringAsync();
            var candidatesBody = await candidatesResponse.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(barResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), barBody);
                Assert.That(candidatesResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK), candidatesBody);
                Assert.That(CandidateReferenceIds(candidatesBody), Does.Contain(healthyReferenceId),
                    $"The healthy item is still resolved alongside the malformed ones. Body: {candidatesBody}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTimeItems_ReviewDrillDown_OrdersRowsByDaysContributedDescendingAndOmitsZeroContributors()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(ItemsUrl(teamId, Review));

            var body = await response.Content.ReadAsStringAsync();
            var rows = ItemRows(body);
            var contributions = rows.Select(row => row.DaysContributed).ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(rows, Has.Count.EqualTo(3),
                    $"Only the three items that actually contributed to Review appear — zero-contribution items are filtered out (strict > 0). Body: {body}");
                Assert.That(contributions, Is.EqualTo(ReviewDrillDownDaysDescending).Within(DaysTolerance),
                    $"US-04 drill-down rows are ordered by daysContributed descending. Body: {body}");
            }
        }

        [Test]
        public async Task GetCumulativeStateTime_KnownFixture_MeanDaysIsArithmeticMeanNotAnotherAggregate()
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();

            client.AsTeamAdmin(teamId);
            var response = await client.GetAsync(BarUrl(teamId));

            var body = await response.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);

                // Review completed visits are 10, 20, 30 and a 0-day same-timestamp exit → arithmetic mean 15
                // (min would be 0, max 30, median 10).
                var review = StateRow(body, Review);
                Assert.That(review.MeanDays, Is.EqualTo(15.0).Within(DaysTolerance),
                    $"meanDays must be the arithmetic mean of completed-visit durations, not the min/max/median. Body: {body}");
            }
        }

        private string BarUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string BarUrlWithItemIds(int teamId, IReadOnlyList<int> itemIds)
        {
            var idParams = string.Join("&", itemIds.Select(id => $"itemIds={id}"));
            return $"{BarUrl(teamId)}&{idParams}";
        }

        private string ItemsUrl(int teamId, string state)
            => $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime/items?state={Uri.EscapeDataString(state)}&startDate={windowStart:O}&endDate={windowEnd:O}";

        private string CandidatesUrl(int teamId)
            => $"/api/latest/teams/{teamId}/metrics/cumulativeStateTime/candidates?startDate={windowStart:O}&endDate={windowEnd:O}";

        private int SeedTeamWithKnownVisitsAndInFlightItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var inWindow = windowStart.AddDays(20);

            // Three completed items walking In Progress → Review → Test → Done with known visit durations.
            // In Progress visits: 5, 5, 10 (sum 20). Review visits: 10, 20, 30 (sum 60). Test visits: 15, 15 (one item skips Test).
            AddCompletedItem(workItemRepository, transitionRepository, team, "DONE-1", inWindow,
                inProgressDays: 5, reviewDays: 10, testDays: 15);
            AddCompletedItem(workItemRepository, transitionRepository, team, "DONE-2", inWindow.AddDays(2),
                inProgressDays: 5, reviewDays: 20, testDays: 15);
            AddCompletedItem(workItemRepository, transitionRepository, team, "DONE-3", inWindow.AddDays(4),
                inProgressDays: 10, reviewDays: 30, testDays: 0);

            // One item in flight in In Progress for 40 full days (ongoing segment of In Progress).
            AddInFlightItem(workItemRepository, team, "WIP-IP", InProgress, currentStateEnteredAt: windowEnd.AddDays(-40));
            // One item in flight in Test for 25 full days (ongoing segment of Test).
            AddInFlightItem(workItemRepository, team, "WIP-TST", Test, currentStateEnteredAt: windowEnd.AddDays(-25));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithSingleStraddlingItem()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            // Entered Review 30 days BEFORE the window, exited 20 days INTO the window: full visit 50 days.
            var reviewEnter = windowStart.AddDays(-30);
            var reviewExit = windowStart.AddDays(20);

            var item = NewWorkItem(team, "STRADDLE-1", state: Done, category: StateCategories.Done,
                startedDate: reviewEnter, closedDate: reviewExit, currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Review, reviewEnter);
            AddTransition(transitionRepository, item, Review, Done, reviewExit);
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithNoItems()
        {
            using var scope = factory.Services.CreateScope();
            var team = AddTeam(scope.ServiceProvider);
            return team.Id;
        }

        private int SeedTeamWhereReviewHasNoContribution()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var inWindow = windowStart.AddDays(20);
            // Item walks In Progress → Test → Done, skipping Review entirely.
            var item = NewWorkItem(team, "NOREVIEW-1", state: Done, category: StateCategories.Done,
                startedDate: inWindow, closedDate: inWindow.AddDays(12), currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Test, inWindow.AddDays(4));
            AddTransition(transitionRepository, item, Test, Done, inWindow.AddDays(12));
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWhereOnlyItemIsEntirelyBeforeWindow()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var startedBeforeWindow = windowStart.AddDays(-60);
            var closedBeforeWindow = windowStart.AddDays(-40);
            var item = NewWorkItem(team, "OLD-1", state: Done, category: StateCategories.Done,
                startedDate: startedBeforeWindow, closedDate: closedBeforeWindow, currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Done, closedBeforeWindow);
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithKnownVisitsPlusOneOutOfWindowItem(out string outOfWindowReferenceId)
        {
            var teamId = SeedTeamWithKnownVisitsAndInFlightItems();
            outOfWindowReferenceId = "OUT-1";

            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = sp.GetRequiredService<IRepository<Team>>().GetById(teamId)!;
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            var startedBeforeWindow = windowStart.AddDays(-60);
            var closedBeforeWindow = windowStart.AddDays(-40);
            var item = NewWorkItem(team, outOfWindowReferenceId, state: Done, category: StateCategories.Done,
                startedDate: startedBeforeWindow, closedDate: closedBeforeWindow, currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Done, closedBeforeWindow);
            transitionRepository.Save().GetAwaiter().GetResult();

            return teamId;
        }

        private int SeedTeamWithBouncedBackToDoItemAndOneDoneItem(out string doneReferenceId, out string bouncedReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            doneReferenceId = "DONE-KEEP";
            bouncedReferenceId = "BOUNCED-TODO";

            var inWindow = windowStart.AddDays(20);

            // A Done item with a single 10-day Review visit (In Progress → Review → Done).
            var doneItem = NewWorkItem(team, doneReferenceId, state: Done, category: StateCategories.Done,
                startedDate: inWindow, closedDate: inWindow.AddDays(10), currentStateEnteredAt: null);
            workItemRepository.Add(doneItem);
            workItemRepository.Save().GetAwaiter().GetResult();
            AddTransition(transitionRepository, doneItem, InProgress, Review, inWindow);
            AddTransition(transitionRepository, doneItem, Review, Done, inWindow.AddDays(10));

            // An item that spent time in Review inside the window but is CURRENTLY back in a To Do-category state.
            var bouncedItem = NewWorkItem(team, bouncedReferenceId, state: "To Do", category: StateCategories.ToDo,
                startedDate: inWindow, closedDate: null, currentStateEnteredAt: inWindow.AddDays(15));
            workItemRepository.Add(bouncedItem);
            workItemRepository.Save().GetAwaiter().GetResult();
            AddTransition(transitionRepository, bouncedItem, InProgress, Review, inWindow.AddDays(5));
            AddTransition(transitionRepository, bouncedItem, Review, "To Do", inWindow.AddDays(15));

            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithSingleItemEnteringAtWindowEnd(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            boundaryReferenceId = "ENTER-AT-END";
            var item = NewWorkItem(team, boundaryReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowEnd, closedDate: windowEnd.AddDays(5), currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Done, windowEnd.AddDays(5));
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithSingleItemExitingAtWindowStart(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            boundaryReferenceId = "EXIT-AT-START";
            var item = NewWorkItem(team, boundaryReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowStart.AddDays(-10), closedDate: windowStart, currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Done, windowStart);
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithInFlightItemEnteringCurrentStateAtWindowEnd(out string boundaryReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();

            boundaryReferenceId = "INFLIGHT-AT-END";
            var item = NewWorkItem(team, boundaryReferenceId, state: InProgress, category: StateCategories.Doing,
                startedDate: windowEnd, closedDate: null, currentStateEnteredAt: windowEnd);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithExcludedDoneItemCarryingCurrentTimestamp(out string excludedReferenceId, out string includedReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            includedReferenceId = "DONE-INWINDOW";
            excludedReferenceId = "DONE-NO-OVERLAP";

            var inWindow = windowStart.AddDays(20);
            AddCompletedItem(workItemRepository, transitionRepository, team, includedReferenceId, inWindow,
                inProgressDays: 5, reviewDays: 10, testDays: 0);

            var noOverlap = NewWorkItem(team, excludedReferenceId, state: Done, category: StateCategories.Done,
                startedDate: windowStart.AddDays(-1), closedDate: null, currentStateEnteredAt: inWindow);
            noOverlap.StartedDate = null;
            workItemRepository.Add(noOverlap);

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithMalformedButRecoverableItems(out string healthyReferenceId)
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp);
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            healthyReferenceId = "HEALTHY-1";
            var inWindow = windowStart.AddDays(20);
            AddCompletedItem(workItemRepository, transitionRepository, team, healthyReferenceId, inWindow,
                inProgressDays: 5, reviewDays: 10, testDays: 0);

            var nullStartDone = NewWorkItem(team, "NULLSTART-DONE", state: Done, category: StateCategories.Done,
                startedDate: inWindow, closedDate: inWindow.AddDays(5), currentStateEnteredAt: null);
            nullStartDone.StartedDate = null;
            workItemRepository.Add(nullStartDone);

            var nullStartDoing = NewWorkItem(team, "NULLSTART-DOING", state: InProgress, category: StateCategories.Doing,
                startedDate: inWindow, closedDate: null, currentStateEnteredAt: inWindow);
            nullStartDoing.StartedDate = null;
            workItemRepository.Add(nullStartDoing);

            var noCurrentDoing = NewWorkItem(team, "NOCURRENT-DOING", state: InProgress, category: StateCategories.Doing,
                startedDate: inWindow, closedDate: null, currentStateEnteredAt: null);
            workItemRepository.Add(noCurrentDoing);

            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, nullStartDone, InProgress, Done, inWindow.AddDays(5));
            AddTransition(transitionRepository, nullStartDoing, "To Do", InProgress, inWindow);
            AddTransition(transitionRepository, noCurrentDoing, "To Do", InProgress, inWindow);
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private List<int> TwoCompletedReviewItemDbIds(int teamId)
        {
            using var scope = factory.Services.CreateScope();
            var workItemRepository = scope.ServiceProvider.GetRequiredService<IWorkItemRepository>();
            return workItemRepository.GetAllByPredicate(wi => wi.TeamId == teamId
                    && (wi.ReferenceId == "DONE-1" || wi.ReferenceId == "DONE-2"))
                .Select(wi => wi.Id)
                .ToList();
        }

        private void AddCompletedItem(
            IWorkItemRepository workItemRepository,
            IWorkItemStateTransitionRepository transitionRepository,
            Team team,
            string referenceId,
            DateTime startedDate,
            int inProgressDays,
            int reviewDays,
            int testDays)
        {
            var inProgressExit = startedDate.AddDays(inProgressDays);
            var reviewExit = inProgressExit.AddDays(reviewDays);
            var testExit = reviewExit.AddDays(testDays);

            var item = NewWorkItem(team, referenceId, state: Done, category: StateCategories.Done,
                startedDate: startedDate, closedDate: testExit, currentStateEnteredAt: null);
            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            AddTransition(transitionRepository, item, InProgress, Review, inProgressExit);
            AddTransition(transitionRepository, item, Review, Test, reviewExit);
            if (testDays > 0)
            {
                AddTransition(transitionRepository, item, Test, Done, testExit);
            }
            else
            {
                AddTransition(transitionRepository, item, Review, Done, reviewExit);
            }
        }

        private void AddInFlightItem(IWorkItemRepository workItemRepository, Team team, string referenceId, string state, DateTime currentStateEnteredAt)
        {
            var item = NewWorkItem(team, referenceId, state: state, category: StateCategories.Doing,
                startedDate: currentStateEnteredAt, closedDate: null, currentStateEnteredAt: currentStateEnteredAt);
            workItemRepository.Add(item);
        }

        private static WorkItem NewWorkItem(Team team, string referenceId, string state, StateCategories category, DateTime startedDate, DateTime? closedDate, DateTime? currentStateEnteredAt)
        {
            return new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = state,
                StateCategory = category,
                Url = UrlFor(referenceId),
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                CurrentStateEnteredAt = currentStateEnteredAt,
                Order = referenceId,
            };
        }

        private static string UrlFor(string referenceId) => $"https://example.test/items/{referenceId}";

        private static void AddTransition(IWorkItemStateTransitionRepository repository, WorkItem item, string fromState, string toState, DateTime transitionedAt)
        {
            repository.Add(new WorkItemStateTransition
            {
                WorkItemId = item.Id,
                FromState = fromState,
                ToState = toState,
                TransitionedAt = transitionedAt,
            });
        }

        private static Team AddTeam(IServiceProvider sp)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var team = new Team
            {
                Name = $"Team {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                DoneItemsCutoffDays = 0,
                ToDoStates = ["To Do"],
                DoingStates = [.. WorkflowDoingStates],
                DoneStates = [Done],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static string[] OrderedStateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            var states = new List<string>();
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                states.Add(entry.GetProperty("state").GetString() ?? string.Empty);
            }
            return states.ToArray();
        }

        private static int StatesArrayLength(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("states").GetArrayLength();
        }

        private static double TotalDaysAcrossAllStates(string body)
        {
            using var document = JsonDocument.Parse(body);
            var total = 0.0;
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                total += entry.GetProperty("totalDays").GetDouble();
            }
            return total;
        }

        private static double SumDaysContributed(string itemsBody)
        {
            using var document = JsonDocument.Parse(itemsBody);
            var total = 0.0;
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                total += item.GetProperty("daysContributed").GetDouble();
            }
            return total;
        }

        private static List<ItemRowView> ItemRows(string itemsBody)
        {
            using var document = JsonDocument.Parse(itemsBody);
            var rows = new List<ItemRowView>();
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                rows.Add(new ItemRowView(
                    WorkItemId: item.GetProperty("workItemId").GetInt32(),
                    ReferenceId: item.GetProperty("referenceId").GetString() ?? string.Empty,
                    Title: item.GetProperty("title").GetString() ?? string.Empty,
                    Type: item.GetProperty("type").GetString() ?? string.Empty,
                    State: item.GetProperty("state").GetString() ?? string.Empty,
                    StateCategory: item.GetProperty("stateCategory").GetString() ?? string.Empty,
                    Url: item.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String ? url.GetString() : null,
                    DaysContributed: item.GetProperty("daysContributed").GetDouble()));
            }
            return rows;
        }

        private static string[] CandidateReferenceIds(string body)
        {
            using var document = JsonDocument.Parse(body);
            var referenceIds = new List<string>();
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                referenceIds.Add(item.GetProperty("referenceId").GetString() ?? string.Empty);
            }
            return referenceIds.ToArray();
        }

        private static bool CandidateRowsHaveParentReferenceId(string body)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var item in document.RootElement.GetProperty("items").EnumerateArray())
            {
                if (item.TryGetProperty("parentReferenceId", out _))
                {
                    return true;
                }
            }
            return false;
        }

        private static StateRowView StateRow(string body, string state)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var entry in document.RootElement.GetProperty("states").EnumerateArray())
            {
                if (entry.GetProperty("state").GetString() != state)
                {
                    continue;
                }

                var hasMedian = entry.TryGetProperty("medianDays", out var medianElement);
                return new StateRowView(
                    TotalDays: entry.GetProperty("totalDays").GetDouble(),
                    CompletedContributionDays: entry.GetProperty("completedContributionDays").GetDouble(),
                    OngoingContributionDays: entry.GetProperty("ongoingContributionDays").GetDouble(),
                    ItemCount: entry.GetProperty("itemCount").GetInt32(),
                    CompletedItemCount: entry.GetProperty("completedItemCount").GetInt32(),
                    OngoingItemCount: entry.GetProperty("ongoingItemCount").GetInt32(),
                    MeanDays: entry.GetProperty("meanDays").GetDouble(),
                    HasMedianDays: hasMedian,
                    MedianDays: hasMedian && medianElement.ValueKind == JsonValueKind.Number ? medianElement.GetDouble() : null);
            }

            Assert.Fail($"State '{state}' was expected in the response but was absent. Body: {body}");
            return default;
        }

        private readonly record struct StateRowView(
            double TotalDays,
            double CompletedContributionDays,
            double OngoingContributionDays,
            int ItemCount,
            int CompletedItemCount,
            int OngoingItemCount,
            double MeanDays,
            bool HasMedianDays,
            double? MedianDays);

        private readonly record struct ItemRowView(
            int WorkItemId,
            string ReferenceId,
            string Title,
            string Type,
            string State,
            string StateCategory,
            string? Url,
            double DaysContributed);
    }
}
