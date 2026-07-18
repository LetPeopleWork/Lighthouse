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
    /// <summary>
    /// DISTILL RED scaffold - feature flow-overview-named-cycle-time, Slice 02 / US-03
    /// (Trend follows the named cycle-time selection). Every test is [Ignore]-marked so the
    /// suite stays green until DELIVER wires D12/ADR-101. When un-ignored against today's code
    /// each test fails for the RIGHT reason - cycleTimePercentilesInfo neither accepts
    /// definitionId nor segments its cache key by definition - NOT for setup/compile error.
    ///
    /// The named-vs-default seed is the shipped Epic 5251 shape: Implementation->Done (def 1)
    /// produces a named P85 that differs from the default started->finished P85 over the same
    /// window, so cache collisions are observable as an equality that should be an inequality.
    /// </summary>
    [TestFixture]
    public class NamedCycleTimeTrendInfoApiIntegrationTest
    {
        private const string Backlog = "Backlog";
        private const string Implementation = "Implementation";
        private const string Done = "Done";
        private const int ImplementationToDoneDefinitionId = 1;
        private const int NonExistentDefinitionId = 987654;

        // Hoisted rather than inlined into the assertion: CA1861 flags constant array
        // arguments passed to repeatedly-called methods.
        private static readonly string[] ExpectedDetailRowLabels = ["50th", "70th", "85th", "95th"];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime conceptStart;

        [OneTimeSetUp]
        public void OneTimeInit()
        {
            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();
        }

        [OneTimeTearDown]
        public void OneTimeCleanup()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        [SetUp]
        public void Init()
        {
            var offsetDays = System.Threading.Interlocked.Increment(ref testDateOffset) * 400;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            conceptStart = windowStart.AddDays(20);

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
            using var teardownScope = factory.Services.CreateScope();
            var dbContext = teardownScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
        }

        // US-03 AC1 (regression guard): definitionId absent => the trend footer is byte-identical to today.
        [Test]
        public async Task Info_DefinitionIdAbsent_ReturnsTheDefaultTrend_Unchanged()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var withoutParam = await client.GetAsync(InfoUrl(teamId, definitionId: null));
            var withZeroParam = await client.GetAsync(InfoUrl(teamId, definitionId: 0));

            var withoutBody = await withoutParam.Content.ReadAsStringAsync();
            var withZeroBody = await withZeroParam.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutParam.StatusCode, Is.EqualTo(HttpStatusCode.OK), withoutBody);
                Assert.That(P85(withZeroBody), Is.EqualTo(P85(withoutBody)),
                    $"definitionId omitted or 0 is not a named request (IsNamedRequest => definitionId > 0); the default trend is returned unchanged. WithZero: {withZeroBody} Without: {withoutBody}");
            }
        }

        // US-03 AC2: named selection => the trend compares the definition's current period against its OWN previous period.
        [Test]
        public async Task Info_NamedDefinition_TrendComparesTheNamedSeriesCurrentPeriodValue()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var namedInfo = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var namedPercentiles = await client.GetAsync(PercentilesUrl(teamId, ImplementationToDoneDefinitionId));

            var namedInfoBody = await namedInfo.Content.ReadAsStringAsync();
            var namedPercentilesBody = await namedPercentiles.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namedInfo.StatusCode, Is.EqualTo(HttpStatusCode.OK), namedInfoBody);
                Assert.That(P85(namedInfoBody), Is.EqualTo(PercentileFromList(namedPercentilesBody, 85)),
                    $"The named trend's current-period P85 is the named series P85 (Implementation->Done), matching the sibling cycleTimePercentiles named read. Info: {namedInfoBody} Percentiles: {namedPercentilesBody}");
            }
        }

        // US-03 AC3 (the cache-collision trap): a named trend and a default trend for the SAME range must not collide.
        [Test]
        public async Task Info_NamedAndDefaultTrend_SameRange_DoNotCollideInCache()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var namedFirst = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var defaultSecond = await client.GetAsync(InfoUrl(teamId, definitionId: null));

            var namedBody = await namedFirst.Content.ReadAsStringAsync();
            var defaultBody = await defaultSecond.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namedFirst.StatusCode, Is.EqualTo(HttpStatusCode.OK), namedBody);
                Assert.That(defaultSecond.StatusCode, Is.EqualTo(HttpStatusCode.OK), defaultBody);

                // The default started->finished P85 differs from the named Implementation->Done P85 over this
                // window. If the cache key lacks a _Def_ segment the second (default) caller receives the first
                // (named) caller's cached numbers, and these two values collapse to equal - the bug this asserts against.
                Assert.That(P85(defaultBody), Is.Not.EqualTo(P85(namedBody)),
                    $"Named-first then default-second must each return their own answer; equal values mean a shared cache key (CycleTimePercentilesInfo_{{start}}_{{end}} with no _Def_ segment). Named: {namedBody} Default: {defaultBody}");
            }
        }

        // US-03 AC3 (definition-vs-definition): the key segments by definition, not merely named-vs-default.
        [Test]
        public async Task Info_TwoDifferentNamedDefinitions_SameRange_EachReturnsItsOwnComparison()
        {
            var teamId = SeedTeamWithTwoDefinitions(out var backlogToDoneDefinitionId);

            client.AsTeamAdmin(teamId);
            var narrow = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var wide = await client.GetAsync(InfoUrl(teamId, backlogToDoneDefinitionId));

            var narrowBody = await narrow.Content.ReadAsStringAsync();
            var wideBody = await wide.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(narrow.StatusCode, Is.EqualTo(HttpStatusCode.OK), narrowBody);
                Assert.That(wide.StatusCode, Is.EqualTo(HttpStatusCode.OK), wideBody);
                Assert.That(P85(wideBody), Is.Not.EqualTo(P85(narrowBody)),
                    $"Backlog->Done is strictly wider than Implementation->Done, so its P85 is larger; equal values mean the key segments only by named-vs-default, not by definitionId. Wide: {wideBody} Narrow: {narrowBody}");
            }
        }

        // US-03 AC4: an invalid / non-existent definitionId behaves exactly as the sibling cycleTimePercentiles named path.
        [Test]
        public async Task Info_NonExistentDefinitionId_MirrorsTheSiblingCycleTimePercentilesNamedPath()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var siblingPercentiles = await client.GetAsync(PercentilesUrl(teamId, NonExistentDefinitionId));
            var info = await client.GetAsync(InfoUrl(teamId, NonExistentDefinitionId));

            var infoBody = await info.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                // Asserted absolutely, not merely "same as the sibling" - two matching
                // 500s would satisfy a relative assertion while both endpoints are broken.
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    $"An unknown definitionId is an empty result, never a server error. Body: {infoBody}");
                Assert.That(siblingPercentiles.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "The sibling cycleTimePercentiles named read holds the same contract.");
                Assert.That(PercentileCount(infoBody), Is.Zero,
                    $"An empty named series yields no percentile lines (sibling parity, not a 500). Body: {infoBody}");
            }
        }

        // The comparison window is the equally long span ending the day before the current
        // one starts. Nothing else in the suite pins those boundaries, so an off-by-one or a
        // sign flip on the offsets would silently compare against the wrong - even future - period.
        [Test]
        public async Task Info_ComparisonPeriod_IsTheEquallyLongWindowImmediatelyBeforeTheCurrentOne()
        {
            var teamId = SeedTeamWithClosedItems();
            var periodDays = (windowEnd.Date - windowStart.Date).Days + 1;
            var expectedPreviousStart = windowStart.AddDays(-periodDays);
            var expectedPreviousEnd = windowStart.AddDays(-1);

            client.AsTeamAdmin(teamId);
            var info = await client.GetAsync(InfoUrl(teamId, definitionId: null));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(ComparisonField(body, "currentLabel"),
                    Is.EqualTo($"{windowStart:yyyy-MM-dd} – {windowEnd:yyyy-MM-dd}"),
                    $"The current label spans exactly the requested range. Body: {body}");
                Assert.That(ComparisonField(body, "previousLabel"),
                    Is.EqualTo($"{expectedPreviousStart:yyyy-MM-dd} – {expectedPreviousEnd:yyyy-MM-dd}"),
                    $"The previous window is {periodDays} days long and ends the day before the current one opens. Body: {body}");
            }
        }

        // The trend's detail rows pair each percentile with ITS OWN previous value. Pairing them
        // by position, or matching the wrong percentile, would misreport every row.
        [Test]
        public async Task Info_DetailRows_PairEachPercentileWithItsOwnPreviousValue()
        {
            var teamId = SeedTeamWithClosedItems();

            client.AsTeamAdmin(teamId);
            var info = await client.GetAsync(InfoUrl(teamId, definitionId: null));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(DetailRowLabels(body), Is.EqualTo(ExpectedDetailRowLabels),
                    $"One row per percentile, labelled '<percentile>th'. Body: {body}");
                Assert.That(MetricLabel(body), Is.EqualTo("Cycle Time Percentiles"),
                    $"The populated comparison names its metric too, not only the empty one. Body: {body}");
                Assert.That(DetailRowPreviousValue(body, "85th"), Is.EqualTo("0"),
                    $"The previous window holds no closed items, so the default path reports a real 0 - not the em-dash reserved for 'no previous series at all'. Body: {body}");

                foreach (var percentile in new[] { 50, 70, 85, 95 })
                {
                    Assert.That(DetailRowCurrentValue(body, $"{percentile}th"),
                        Is.EqualTo(PercentileFromArray(JsonDocument.Parse(body).RootElement.GetProperty("percentiles"), percentile, body).ToString()),
                        $"Row {percentile}th carries the {percentile}th percentile's own current value. Body: {body}");
                }
            }
        }

        // A named definition with data this period but none in the comparison period has no
        // previous value to show - the row must say so rather than fabricate a zero.
        [Test]
        public async Task Info_NamedDefinition_NoPreviousPeriodData_ShowsAnAbsentPreviousValue()
        {
            var teamId = SeedTeamWithItemsClosedOnlyInTheCurrentWindow();

            client.AsTeamAdmin(teamId);
            var info = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PercentileCount(body), Is.GreaterThan(0),
                    $"The current period has closed items for this definition. Body: {body}");
                Assert.That(DetailRowPreviousValue(body, "85th"), Is.EqualTo("–"),
                    $"No previous-period data for this named definition, so the previous column is an em-dash, not a 0 that would read as an infinitely fast prior period. Body: {body}");
            }
        }

        // An INVALID definition (it exists, but a boundary state is no longer in the workflow)
        // is not a computable window. Guarding only on "definition == null" would compute one anyway.
        [Test]
        public async Task Info_InvalidDefinition_YieldsNoPercentiles()
        {
            var teamId = SeedTeamWithInvalidDefinition();

            client.AsTeamAdmin(teamId);
            var info = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PercentileCount(body), Is.Zero,
                    $"A definition whose boundary state left the workflow computes nothing. Body: {body}");
                Assert.That(Direction(body), Is.EqualTo("none"),
                    $"No data means no trend verdict. Body: {body}");
                Assert.That(MetricLabel(body), Is.EqualTo("Cycle Time Percentiles"),
                    $"The empty comparison still names the metric it belongs to. Body: {body}");
            }
        }

        // Regression (adversarial review): an empty CURRENT named period must not read as an
        // improvement just because the PREVIOUS period had data. The median falls back to 0 when
        // the current percentile list is empty, and 0 < previous median compares as "faster".
        [Test]
        public async Task Info_NamedDefinition_CurrentPeriodEmpty_PreviousHadData_DoesNotClaimAnImprovement()
        {
            var teamId = SeedTeamWithItemsClosedOnlyInThePreviousWindow();

            client.AsTeamAdmin(teamId);
            var info = await client.GetAsync(InfoUrl(teamId, ImplementationToDoneDefinitionId));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PercentileCount(body), Is.Zero,
                    $"Nothing closed in the current window for this definition, so there are no percentile lines. Body: {body}");
                Assert.That(Direction(body), Is.EqualTo("none"),
                    $"An absent current period is not an improvement - reporting 'down' (faster) from a 0 median would be a fabricated green. Body: {body}");
            }
        }

        // US-03 AC5: the Portfolio twin behaves identically to Team scope.
        [Test]
        public async Task PortfolioInfo_NamedAndDefaultTrend_SameRange_DoNotCollideInCache()
        {
            var portfolioId = SeedPortfolioWithClosedItems();

            client.AsPortfolioAdmin(portfolioId);
            var namedFirst = await client.GetAsync(PortfolioInfoUrl(portfolioId, ImplementationToDoneDefinitionId));
            var defaultSecond = await client.GetAsync(PortfolioInfoUrl(portfolioId, definitionId: null));

            var namedBody = await namedFirst.Content.ReadAsStringAsync();
            var defaultBody = await defaultSecond.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(namedFirst.StatusCode, Is.EqualTo(HttpStatusCode.OK), namedBody);
                Assert.That(P85(defaultBody), Is.Not.EqualTo(P85(namedBody)),
                    $"Portfolio twin must segment its cache key by definition exactly as Team scope does. Named: {namedBody} Default: {defaultBody}");
            }
        }

        // The cache key must segment by DATE RANGE as well as by definition. A key that
        // collapsed the range would serve the first window's answer for every later window.
        [Test]
        public async Task Info_DifferentDateRanges_DoNotShareACacheEntry()
        {
            var teamId = SeedTeamWithClosedItems();
            // conceptStart+29 closes inside the early window; conceptStart+46 only inside the late one.
            var earlyEnd = conceptStart.AddDays(35);
            var lateStart = conceptStart.AddDays(36);

            client.AsTeamAdmin(teamId);
            var early = await client.GetAsync(InfoUrlForRange(teamId, definitionId: null, windowStart, earlyEnd));
            var late = await client.GetAsync(InfoUrlForRange(teamId, definitionId: null, lateStart, windowEnd));
            var namedEarly = await client.GetAsync(InfoUrlForRange(teamId, ImplementationToDoneDefinitionId, windowStart, earlyEnd));
            var namedLate = await client.GetAsync(InfoUrlForRange(teamId, ImplementationToDoneDefinitionId, lateStart, windowEnd));

            var earlyBody = await early.Content.ReadAsStringAsync();
            var lateBody = await late.Content.ReadAsStringAsync();
            var namedEarlyBody = await namedEarly.Content.ReadAsStringAsync();
            var namedLateBody = await namedLate.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(early.StatusCode, Is.EqualTo(HttpStatusCode.OK), earlyBody);
                Assert.That(P85(earlyBody), Is.Not.EqualTo(P85(lateBody)),
                    $"Two windows holding different closed items must not share a default cache entry. Early: {earlyBody} Late: {lateBody}");
                Assert.That(P85(namedEarlyBody), Is.Not.EqualTo(P85(namedLateBody)),
                    $"The same holds for the named key - the range is part of it, not just the definition. Early: {namedEarlyBody} Late: {namedLateBody}");
            }
        }

        // The Portfolio twin carries its own copy of the window arithmetic and the named
        // guards, so it needs its own coverage - a Team-only test leaves that copy unpinned.
        [Test]
        public async Task PortfolioInfo_ComparisonPeriod_IsTheEquallyLongWindowImmediatelyBeforeTheCurrentOne()
        {
            var portfolioId = SeedPortfolioWithClosedItems();
            var periodDays = (windowEnd.Date - windowStart.Date).Days + 1;
            var expectedPreviousStart = windowStart.AddDays(-periodDays);
            var expectedPreviousEnd = windowStart.AddDays(-1);

            client.AsPortfolioAdmin(portfolioId);
            var info = await client.GetAsync(PortfolioInfoUrl(portfolioId, definitionId: null));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(ComparisonField(body, "currentLabel"),
                    Is.EqualTo($"{windowStart:yyyy-MM-dd} – {windowEnd:yyyy-MM-dd}"),
                    $"The current label spans exactly the requested range. Body: {body}");
                Assert.That(ComparisonField(body, "previousLabel"),
                    Is.EqualTo($"{expectedPreviousStart:yyyy-MM-dd} – {expectedPreviousEnd:yyyy-MM-dd}"),
                    $"The previous window is {periodDays} days long and ends the day before the current one opens. Body: {body}");
            }
        }

        [Test]
        public async Task PortfolioInfo_DefinitionIdAbsentOrZero_ReturnsTheDefaultTrend()
        {
            var portfolioId = SeedPortfolioWithClosedItems();

            client.AsPortfolioAdmin(portfolioId);
            var withoutParam = await client.GetAsync(PortfolioInfoUrl(portfolioId, definitionId: null));
            var withZeroParam = await client.GetAsync(PortfolioInfoUrl(portfolioId, definitionId: 0));

            var withoutBody = await withoutParam.Content.ReadAsStringAsync();
            var withZeroBody = await withZeroParam.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(withoutParam.StatusCode, Is.EqualTo(HttpStatusCode.OK), withoutBody);
                Assert.That(P85(withZeroBody), Is.EqualTo(P85(withoutBody)),
                    $"definitionId 0 is not a named request (IsNamedRequest => definitionId > 0). WithZero: {withZeroBody} Without: {withoutBody}");
            }
        }

        [Test]
        public async Task PortfolioInfo_DifferentDateRanges_DoNotShareACacheEntry()
        {
            var portfolioId = SeedPortfolioWithClosedItems();
            var earlyEnd = conceptStart.AddDays(35);
            var lateStart = conceptStart.AddDays(36);

            client.AsPortfolioAdmin(portfolioId);
            var early = await client.GetAsync(PortfolioInfoUrlForRange(portfolioId, definitionId: null, windowStart, earlyEnd));
            var late = await client.GetAsync(PortfolioInfoUrlForRange(portfolioId, definitionId: null, lateStart, windowEnd));

            var earlyBody = await early.Content.ReadAsStringAsync();
            var lateBody = await late.Content.ReadAsStringAsync();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(early.StatusCode, Is.EqualTo(HttpStatusCode.OK), earlyBody);
                Assert.That(P85(earlyBody), Is.Not.EqualTo(P85(lateBody)),
                    $"The Portfolio cache key carries the range too. Early: {earlyBody} Late: {lateBody}");
            }
        }

        [Test]
        public async Task PortfolioInfo_InvalidDefinition_YieldsNoPercentiles()
        {
            var portfolioId = SeedPortfolioWithInvalidDefinition();

            client.AsPortfolioAdmin(portfolioId);
            var info = await client.GetAsync(PortfolioInfoUrl(portfolioId, ImplementationToDoneDefinitionId));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK), body);
                Assert.That(PercentileCount(body), Is.Zero,
                    $"A definition whose boundary state left the workflow computes nothing. Body: {body}");
                Assert.That(Direction(body), Is.EqualTo("none"),
                    $"No data means no trend verdict. Body: {body}");
            }
        }

        [Test]
        public async Task PortfolioInfo_NonExistentDefinitionId_IsAnEmptyResultNotAnError()
        {
            var portfolioId = SeedPortfolioWithClosedItems();

            client.AsPortfolioAdmin(portfolioId);
            var info = await client.GetAsync(PortfolioInfoUrl(portfolioId, NonExistentDefinitionId));
            var body = await info.Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(info.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    $"An unknown definitionId is an empty result, never a server error. Body: {body}");
                Assert.That(PercentileCount(body), Is.Zero, body);
            }
        }

        private string InfoUrl(int teamId, int? definitionId)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimePercentilesInfo?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private static string InfoUrlForRange(int teamId, int? definitionId, DateTime start, DateTime end)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimePercentilesInfo?startDate={start:O}&endDate={end:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private static string PortfolioInfoUrlForRange(int portfolioId, int? definitionId, DateTime start, DateTime end)
        {
            var baseUrl = $"/api/latest/portfolios/{portfolioId}/metrics/cycleTimePercentilesInfo?startDate={start:O}&endDate={end:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private string PercentilesUrl(int teamId, int? definitionId)
        {
            var baseUrl = $"/api/latest/teams/{teamId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private string PortfolioInfoUrl(int portfolioId, int? definitionId)
        {
            var baseUrl = $"/api/latest/portfolios/{portfolioId}/metrics/cycleTimePercentilesInfo?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{baseUrl}&definitionId={definitionId.Value}" : baseUrl;
        }

        private int SeedTeamWithClosedItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, ImplementationToDoneDefinition());
            SeedClosedItems(sp, team);
            return team.Id;
        }

        private int SeedTeamWithTwoDefinitions(out int backlogToDoneDefinitionId)
        {
            backlogToDoneDefinitionId = ImplementationToDoneDefinitionId + 1;
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(
                sp,
                ImplementationToDoneDefinition(),
                new CycleTimeDefinition
                {
                    Id = backlogToDoneDefinitionId,
                    Name = "Backlog to Done",
                    StartState = Backlog,
                    EndState = Done,
                });
            SeedClosedItems(sp, team);
            return team.Id;
        }

        private int SeedTeamWithItemsClosedOnlyInTheCurrentWindow()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, ImplementationToDoneDefinition());

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            // conceptStart sits 20 days into the current window, so both land inside it and
            // the equally long preceding window holds nothing for this definition.
            AddItem(workItemRepository, transitionRepository, team, "PHX-CUR-1", conceptStart, conceptStart.AddDays(12),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Done, conceptStart.AddDays(12)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-CUR-2", conceptStart.AddDays(3), conceptStart.AddDays(21),
                Transition(Backlog, Implementation, conceptStart.AddDays(3)),
                Transition(Implementation, Done, conceptStart.AddDays(21)));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedTeamWithInvalidDefinition()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            // The definition's start boundary names a state the team's workflow does not
            // contain, which is exactly what happens when an admin removes a state later.
            var team = AddTeam(sp, new CycleTimeDefinition
            {
                Id = ImplementationToDoneDefinitionId,
                Name = "Concept to Done",
                StartState = "AStateThatLeftTheWorkflow",
                EndState = Done,
            });

            SeedClosedItems(sp, team);
            return team.Id;
        }

        private int SeedTeamWithItemsClosedOnlyInThePreviousWindow()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var team = AddTeam(sp, ImplementationToDoneDefinition());

            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            // The comparison window is the equally long span immediately before windowStart, so
            // these two land in the PREVIOUS period and nothing at all lands in the current one.
            var previousClose = windowStart.AddDays(-30);
            AddItem(workItemRepository, transitionRepository, team, "PHX-PREV-1", previousClose.AddDays(-15), previousClose,
                Transition(Backlog, Implementation, previousClose.AddDays(-15)),
                Transition(Implementation, Done, previousClose));

            AddItem(workItemRepository, transitionRepository, team, "PHX-PREV-2", previousClose.AddDays(-9), previousClose.AddDays(-2),
                Transition(Backlog, Implementation, previousClose.AddDays(-9)),
                Transition(Implementation, Done, previousClose.AddDays(-2)));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();

            return team.Id;
        }

        private int SeedPortfolioWithClosedItems()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;
            var portfolio = AddPortfolio(sp, ImplementationToDoneDefinition());
            SeedClosedFeatures(sp, portfolio);
            return portfolio.Id;
        }

        private int SeedPortfolioWithInvalidDefinition()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

            var portfolio = AddPortfolio(sp, new CycleTimeDefinition
            {
                Id = ImplementationToDoneDefinitionId,
                Name = "Concept to Done",
                StartState = "AStateThatLeftTheWorkflow",
                EndState = Done,
            });

            SeedClosedFeatures(sp, portfolio);
            return portfolio.Id;
        }

        private void SeedClosedFeatures(IServiceProvider sp, Portfolio portfolio)
        {
            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            AddFeature(featureRepository, transitionRepository, portfolio, "EPIC-204", conceptStart, conceptStart.AddDays(46),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Done, conceptStart.AddDays(46)));

            AddFeature(featureRepository, transitionRepository, portfolio, "EPIC-211", conceptStart, conceptStart.AddDays(29),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Backlog, conceptStart.AddDays(12)),
                Transition(Backlog, Implementation, conceptStart.AddDays(20)),
                Transition(Implementation, Done, conceptStart.AddDays(29)));

            AddFeature(featureRepository, transitionRepository, portfolio, "EPIC-NEVERDONE", conceptStart, conceptStart.AddDays(40),
                Transition(Backlog, Implementation, conceptStart.AddDays(6)));

            featureRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();
        }

        private static Portfolio AddPortfolio(IServiceProvider sp, params CycleTimeDefinition[] definitions)
        {
            var connection = new WorkTrackingSystemConnection
            {
                Name = $"Connection {Guid.NewGuid():N}",
                WorkTrackingSystem = WorkTrackingSystems.AzureDevOps,
            };

            var portfolio = new Portfolio
            {
                Name = $"Portfolio {Guid.NewGuid():N}",
                WorkTrackingSystemConnection = connection,
                ToDoStates = [Backlog],
                DoingStates = [Implementation],
                DoneStates = [Done],
                CycleTimeDefinitions = [.. definitions],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            return portfolio;
        }

        private static void AddFeature(
            IRepository<Feature> featureRepository,
            IFeatureStateTransitionRepository transitionRepository,
            Portfolio portfolio,
            string referenceId,
            DateTime startedDate,
            DateTime closedDate,
            params WorkItemStateTransition[] transitions)
        {
            var feature = new Feature
            {
                ReferenceId = referenceId,
                Name = $"Epic {referenceId}",
                Type = "Epic",
                State = Done,
                StateCategory = StateCategories.Done,
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Order = referenceId,
            };
            feature.Portfolios.Add(portfolio);

            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            foreach (var transition in transitions)
            {
                transitionRepository.Add(new FeatureStateTransition
                {
                    FeatureId = feature.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                });
            }
        }

        private void SeedClosedItems(IServiceProvider sp, Team team)
        {
            var workItemRepository = sp.GetRequiredService<IWorkItemRepository>();
            var transitionRepository = sp.GetRequiredService<IWorkItemStateTransitionRepository>();

            AddItem(workItemRepository, transitionRepository, team, "PHX-204", conceptStart, conceptStart.AddDays(46),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Done, conceptStart.AddDays(46)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-211", conceptStart, conceptStart.AddDays(29),
                Transition(Backlog, Implementation, conceptStart),
                Transition(Implementation, Backlog, conceptStart.AddDays(12)),
                Transition(Backlog, Implementation, conceptStart.AddDays(20)),
                Transition(Implementation, Done, conceptStart.AddDays(29)));

            AddItem(workItemRepository, transitionRepository, team, "PHX-NEVERDONE", conceptStart, conceptStart.AddDays(40),
                Transition(Backlog, Implementation, conceptStart.AddDays(6)));

            workItemRepository.Save().GetAwaiter().GetResult();
            transitionRepository.Save().GetAwaiter().GetResult();
        }

        private static CycleTimeDefinition ImplementationToDoneDefinition()
        {
            return new CycleTimeDefinition
            {
                Id = ImplementationToDoneDefinitionId,
                Name = "Implementation to Done",
                StartState = Implementation,
                EndState = Done,
            };
        }

        private static void AddItem(
            IWorkItemRepository workItemRepository,
            IWorkItemStateTransitionRepository transitionRepository,
            Team team,
            string referenceId,
            DateTime startedDate,
            DateTime closedDate,
            params WorkItemStateTransition[] transitions)
        {
            var item = new WorkItem
            {
                Team = team,
                TeamId = team.Id,
                ReferenceId = referenceId,
                Name = $"Story {referenceId}",
                Type = "Story",
                State = Done,
                StateCategory = StateCategories.Done,
                Url = $"https://example.test/items/{referenceId}",
                CreatedDate = startedDate.AddDays(-1),
                StartedDate = startedDate,
                ClosedDate = closedDate,
                Order = referenceId,
            };

            workItemRepository.Add(item);
            workItemRepository.Save().GetAwaiter().GetResult();

            foreach (var transition in transitions)
            {
                transitionRepository.Add(new WorkItemStateTransition
                {
                    WorkItemId = item.Id,
                    FromState = transition.FromState,
                    ToState = transition.ToState,
                    TransitionedAt = transition.TransitionedAt,
                });
            }
        }

        private static WorkItemStateTransition Transition(string fromState, string toState, DateTime transitionedAt)
        {
            return new WorkItemStateTransition { FromState = fromState, ToState = toState, TransitionedAt = transitionedAt };
        }

        private static Team AddTeam(IServiceProvider sp, params CycleTimeDefinition[] definitions)
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
                ToDoStates = [Backlog],
                DoingStates = [Implementation],
                DoneStates = [Done],
                CycleTimeDefinitions = [.. definitions],
            };

            var teamRepository = sp.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(team);
            teamRepository.Save().GetAwaiter().GetResult();

            return team;
        }

        private static int P85(string body)
        {
            using var document = JsonDocument.Parse(body);
            return PercentileFromArray(document.RootElement.GetProperty("percentiles"), 85, body);
        }

        private static string ComparisonField(string body, string field)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("comparison").GetProperty(field).GetString() ?? string.Empty;
        }

        private static string MetricLabel(string body)
        {
            return ComparisonField(body, "metricLabel");
        }

        private static string[] DetailRowLabels(string body)
        {
            using var document = JsonDocument.Parse(body);
            return [.. document.RootElement.GetProperty("comparison").GetProperty("detailRows")
                .EnumerateArray()
                .Select(row => row.GetProperty("label").GetString() ?? string.Empty)];
        }

        private static string DetailRowCurrentValue(string body, string label)
        {
            return DetailRowField(body, label, "currentValue");
        }

        private static string DetailRowPreviousValue(string body, string label)
        {
            return DetailRowField(body, label, "previousValue");
        }

        private static string DetailRowField(string body, string label, string field)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var row in document.RootElement.GetProperty("comparison").GetProperty("detailRows").EnumerateArray())
            {
                if (row.GetProperty("label").GetString() == label)
                {
                    return row.GetProperty(field).GetString() ?? string.Empty;
                }
            }

            Assert.Fail($"Detail row '{label}' was expected in the response but was absent. Body: {body}");
            return string.Empty;
        }

        private static string Direction(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("comparison").GetProperty("direction").GetString() ?? string.Empty;
        }

        private static int PercentileCount(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("percentiles").GetArrayLength();
        }

        private static int PercentileFromList(string body, int percentile)
        {
            using var document = JsonDocument.Parse(body);
            return PercentileFromArray(document.RootElement, percentile, body);
        }

        private static int PercentileFromArray(JsonElement array, int percentile, string body)
        {
            foreach (var entry in array.EnumerateArray())
            {
                if (entry.GetProperty("percentile").GetInt32() == percentile)
                {
                    return entry.GetProperty("value").GetInt32();
                }
            }

            Assert.Fail($"Percentile {percentile} was expected in the response but was absent. Body: {body}");
            return 0;
        }
    }
}
