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
    // US-05 (D7): named cycle times at Portfolio scope — the twin of the Team build over the shared
    // WorkTrackingSystemOptionsOwner. The ADR-062 revision applies here too: no read-side premium gate
    // (the premium gate lives on definition create/update only), so a portfolio viewer reads the named
    // branch just like the Default.
    [TestFixture]
    [NonParallelizable]
    public class NamedCycleTimePortfolioIntegrationTest
    {
        private const string Backlog = "Backlog";
        private const string Analyzing = "Analyzing";
        private const string Building = "Building";
        private const string Validating = "Validating";
        private const string Done = "Done";

        private const int IdeaToLiveDefinitionId = 1;
        private const int BuildToDoneDefinitionId = 2;
        private const int InvalidDefinitionId = 3;

        private static readonly string[] DefaultDoingStates = [Analyzing, Building, Validating];
        private static readonly int[] ExpectedPercentiles = [50, 70, 85, 95];

        private static int testDateOffset;

        private TestWebApplicationFactory<Program> rootFactory = null!;
        private WebApplicationFactory<Program> factory = null!;
        private HttpClient client = null!;
        private DateTime windowStart;
        private DateTime windowEnd;
        private DateTime workStart;
        private int seededPortfolioId;

        [SetUp]
        public void Init()
        {
            var offsetDays = (System.Threading.Interlocked.Increment(ref testDateOffset) * 400) + 90000;
            windowEnd = new DateTime(2026, 5, 25, 0, 0, 0, DateTimeKind.Utc).AddDays(-offsetDays);
            windowStart = windowEnd.AddDays(-180);
            workStart = windowStart.AddDays(20);

            rootFactory = new TestWebApplicationFactory<Program>();
            factory = TestWebApplicationFactory<Program>.WithTestAuthentication(rootFactory);
            client = factory.CreateClient();

            using var setupScope = factory.Services.CreateScope();
            var dbContext = setupScope.ServiceProvider.GetRequiredService<Lighthouse.Backend.Data.LighthouseAppContext>();
            dbContext.Database.EnsureDeleted();
            dbContext.Database.EnsureCreated();

            foreach (var seeder in setupScope.ServiceProvider.GetServices<ISeeder>())
            {
                seeder.Seed().GetAwaiter().GetResult();
            }

            seededPortfolioId = SeedPortfolioWithDefinitionsAndFeature();
            client.AsPortfolioAdmin(seededPortfolioId);
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
        public async Task Portfolio_NamedDefinitions_SurviveReload_StampedWithValidity()
        {
            var settings = await (await client.GetAsync($"/api/latest/portfolios/{seededPortfolioId}/settings")).Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(settings);
            var definitions = document.RootElement.GetProperty("cycleTimeDefinitions").EnumerateArray().ToList();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(definitions, Has.Count.EqualTo(3),
                    $"US-05: the three saved definitions survive reload on the Portfolio owner. Body: {settings}");
                Assert.That(IsValidOf(definitions, IdeaToLiveDefinitionId), Is.True,
                    $"US-05: a fully-resolvable definition is stamped IsValid=true. Body: {settings}");
                Assert.That(IsValidOf(definitions, InvalidDefinitionId), Is.False,
                    $"US-05/D5: a definition whose boundary state is absent from AllStates is stamped IsValid=false. Body: {settings}");
            }
        }

        [Test]
        public async Task Portfolio_NamedDefinition_ScatterCarriesRecomputedNamedValues_InvalidOmitted()
        {
            var data = await (await client.GetAsync(CycleTimeDataUrl())).Content.ReadAsStringAsync();

            var named = NamedCycleTimesOf(data, "EPIC-1");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(named.ContainsKey(IdeaToLiveDefinitionId), Is.True,
                    $"US-05: the scatter feature carries a named value for the valid Backlog→Done definition. Body: {data}");
                Assert.That(named[IdeaToLiveDefinitionId], Is.GreaterThan(named[BuildToDoneDefinitionId]),
                    $"US-05: the wider Backlog→Done window is longer than the narrower Building→Done window. Body: {data}");
                Assert.That(named.ContainsKey(InvalidDefinitionId), Is.False,
                    $"US-05/D5: an invalid definition contributes no named value to the scatter. Body: {data}");
            }
        }

        [Test]
        public async Task Portfolio_NamedPercentiles_RecomputeOverTheWindow()
        {
            var percentiles = await (await client.GetAsync(PercentilesUrl(IdeaToLiveDefinitionId))).Content.ReadAsStringAsync();

            using var document = JsonDocument.Parse(percentiles);
            var values = document.RootElement.EnumerateArray()
                .Select(entry => entry.GetProperty("percentile").GetInt32())
                .ToList();
            Assert.That(values, Is.EquivalentTo(ExpectedPercentiles),
                $"US-05: cycleTimePercentiles?definitionId recomputes the 50/70/85/95 percentiles over the named window. Body: {percentiles}");
        }

        [Test]
        public async Task Portfolio_CumulativeScope_SpansTheNamedStates_EndStateExcluded_BacklogAppears()
        {
            var unscoped = await (await client.GetAsync(CumulativeUrl(null))).Content.ReadAsStringAsync();
            var scoped = await (await client.GetAsync(CumulativeUrl(IdeaToLiveDefinitionId))).Content.ReadAsStringAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(StateNames(unscoped), Does.Not.Contain(Backlog),
                    $"US-05: the default portfolio view bars only Doing states — Backlog never appears. Body: {unscoped}");
                Assert.That(StateNames(scoped), Does.Contain(Backlog),
                    $"US-05: scoping to a Backlog-anchored window adds Backlog to the displayed span. Body: {scoped}");
                Assert.That(StateNames(scoped), Does.Not.Contain(Done),
                    $"US-05/D10: the end state Done is excluded from the half-open span. Body: {scoped}");
            }
        }

        [Test]
        public async Task Portfolio_CumulativeScope_InvalidDefinition_StaysUnscoped()
        {
            var scopedInvalid = await (await client.GetAsync(CumulativeUrl(InvalidDefinitionId))).Content.ReadAsStringAsync();
            var unscoped = await (await client.GetAsync(CumulativeUrl(null))).Content.ReadAsStringAsync();

            Assert.That(scopedInvalid, Is.EqualTo(unscoped),
                "US-05/D5: an invalid definitionId is refused — the portfolio cumulative chart stays unscoped, never scoped against missing states.");
        }

        [Test]
        public async Task Portfolio_Viewer_CanReadTheNamedBranch_NoReadSidePremiumGate()
        {
            client.AsPortfolioViewer(seededPortfolioId);
            var response = await client.GetAsync(CycleTimeDataUrl());

            var body = await response.Content.ReadAsStringAsync();
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                $"ADR-062: there is no read-side premium gate — a portfolio viewer reads the named scatter branch like the Default. Body: {body}");
        }

        private string CycleTimeDataUrl()
            => $"/api/latest/portfolios/{seededPortfolioId}/metrics/cycleTimeData?startDate={windowStart:O}&endDate={windowEnd:O}";

        private string PercentilesUrl(int definitionId)
            => $"/api/latest/portfolios/{seededPortfolioId}/metrics/cycleTimePercentiles?startDate={windowStart:O}&endDate={windowEnd:O}&definitionId={definitionId}";

        private string CumulativeUrl(int? definitionId)
        {
            var url = $"/api/latest/portfolios/{seededPortfolioId}/metrics/cumulativeStateTime?startDate={windowStart:O}&endDate={windowEnd:O}";
            return definitionId.HasValue ? $"{url}&definitionId={definitionId.Value}" : url;
        }

        private static bool IsValidOf(IEnumerable<JsonElement> definitions, int id)
        {
            var match = definitions.Single(definition => definition.GetProperty("id").GetInt32() == id);
            return match.GetProperty("isValid").GetBoolean();
        }

        private static Dictionary<int, int> NamedCycleTimesOf(string body, string referenceId)
        {
            using var document = JsonDocument.Parse(body);
            foreach (var feature in document.RootElement.EnumerateArray())
            {
                if (feature.GetProperty("referenceId").GetString() != referenceId)
                {
                    continue;
                }

                return feature.GetProperty("namedCycleTimes").EnumerateArray()
                    .ToDictionary(
                        named => named.GetProperty("definitionId").GetInt32(),
                        named => named.GetProperty("days").GetInt32());
            }

            Assert.Fail($"Feature '{referenceId}' not found. Body: {body}");
            return [];
        }

        private static List<string> StateNames(string body)
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.GetProperty("states").EnumerateArray()
                .Select(row => row.GetProperty("state").GetString() ?? string.Empty)
                .ToList();
        }

        private int SeedPortfolioWithDefinitionsAndFeature()
        {
            using var scope = factory.Services.CreateScope();
            var sp = scope.ServiceProvider;

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
                DoingStates = [.. DefaultDoingStates],
                DoneStates = [Done],
                CycleTimeDefinitions =
                [
                    new CycleTimeDefinition { Id = IdeaToLiveDefinitionId, Name = "Idea to Live", StartState = Backlog, EndState = Done },
                    new CycleTimeDefinition { Id = BuildToDoneDefinitionId, Name = "Build to Done", StartState = Building, EndState = Done },
                    new CycleTimeDefinition { Id = InvalidDefinitionId, Name = "Phantom to Done", StartState = "Phantom", EndState = Done },
                ],
            };

            var portfolioRepository = sp.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            portfolioRepository.Save().GetAwaiter().GetResult();

            var featureRepository = sp.GetRequiredService<IRepository<Feature>>();
            var transitionRepository = sp.GetRequiredService<IFeatureStateTransitionRepository>();

            var feature = new Feature
            {
                ReferenceId = "EPIC-1",
                Name = "Epic EPIC-1",
                Type = "Epic",
                State = Done,
                StateCategory = StateCategories.Done,
                CreatedDate = workStart.AddDays(-1),
                StartedDate = workStart,
                ClosedDate = workStart.AddDays(18),
                Order = "EPIC-1",
            };
            feature.Portfolios.Add(portfolio);
            featureRepository.Add(feature);
            featureRepository.Save().GetAwaiter().GetResult();

            transitionRepository.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = Backlog, ToState = Analyzing, TransitionedAt = workStart.AddDays(2) });
            transitionRepository.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = Analyzing, ToState = Building, TransitionedAt = workStart.AddDays(5) });
            transitionRepository.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = Building, ToState = Validating, TransitionedAt = workStart.AddDays(12) });
            transitionRepository.Add(new FeatureStateTransition { FeatureId = feature.Id, FromState = Validating, ToState = Done, TransitionedAt = workStart.AddDays(18) });
            transitionRepository.Save().GetAwaiter().GetResult();

            return portfolio.Id;
        }
    }
}
