using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.API.DTO;
using Lighthouse.Backend.API.DTO.Archived;
using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Forecast;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration
{
    // Story #5587 (ADR-113), slice-01, through the real driving port:
    // GET /api/latest/deliveries/portfolio/{portfolioId}.
    //
    // Two features on two separate teams, each 90 % likely by the delivery date. The governing-feature
    // header answers 90 %; the joint across every (team, feature) row is .90 x .90 = .81. The unit
    // tests prove the maths; this proves the maths is WIRED - the delivery DTO, not just the collaborator.
    [TestFixture]
    public class DeliveryJointForecastIntegrationTest() : IntegrationTestBase
    {
        private const int TargetDays = 10;
        private const int TailDays = 20;

        // Every key the deliveries payload carries. The CLI and the MCP server read this payload by
        // name, so a key silently appearing or vanishing breaks them somewhere nobody is looking -
        // which is why the whole set is written out here and compared exactly. Growing it is a
        // deliberate act; a diff to this list is the moment to ask whether those clients were told.
        private static readonly string[] ExpectedDeliveryPayloadKeys =
        [
            "id", "name", "date", "portfolioId", "likelihoodPercentage", "teamsWithoutForecast",
            "completionDates", "progress", "remainingWork", "totalWork", "features",
            "featureLikelihoods", "hasSufficientData", "metricSnapshotCount", "selectionMode",
            "rules", "mode", "concurrencyToken",
            "sourceKey", "sourceReference", "sourceLastSyncedOn", "sourceUnavailableReason",
            "publishForecastToSource", "isOverdue",
        ];

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        // The server anchors on the instance zone, so the expectations must too - Bug #5567.
        private DateTime Today => ServiceProvider.GetRequiredService<ILighthouseClock>().TodayAsUtcMidnight;

        [Test]
        public async Task GetDelivery_TwoFeaturesOnSeparateTeams_LikelihoodIsTheJointAcrossEveryFeature()
        {
            var portfolio = await SeedPortfolioWithTwoSingleTeamFeatures();
            await CreateDelivery(portfolio, Today.AddDays(TargetDays));

            var delivery = await GetSingleDelivery(portfolio.Id);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    delivery.FeatureLikelihoods.Select(feature => feature.LikelihoodPercentage),
                    Has.All.EqualTo(90).Within(0.01),
                    "each feature on its own");
                Assert.That(delivery.LikelihoodPercentage, Is.EqualTo(81).Within(0.01), "joint across both features");
            }
        }

        [Test]
        public async Task GetDelivery_TwoFeaturesOnSeparateTeams_PercentileDatesComeFromTheJointHistogram()
        {
            // The percentile chips move outward too, not only the badge - the single most
            // under-communicated consequence of the change. The joint 85th percentile falls past the
            // target day onto the tail, while each feature's own 85th is still the target day.
            var portfolio = await SeedPortfolioWithTwoSingleTeamFeatures();
            await CreateDelivery(portfolio, Today.AddDays(TargetDays));

            var delivery = await GetSingleDelivery(portfolio.Id);

            var eightyFifth = delivery.CompletionDates.Single(forecast => forecast.Probability == 85);

            Assert.That(eightyFifth.ExpectedDate, Is.EqualTo(Today.AddDays(TailDays)));
        }

        [Test]
        public async Task GetDelivery_JointRollup_LeavesTheDeliveryPayloadShapeUnchanged()
        {
            // AC-01.12, a contract guard: LikelihoodPercentage and CompletionDates carry different
            // values on the SAME wire shape, which is what keeps the CLI/MCP clients out of this
            // release. Green today and it must stay green.
            var portfolio = await SeedPortfolioWithTwoSingleTeamFeatures();
            await CreateDelivery(portfolio, Today.AddDays(TargetDays));

            Client.AsPortfolioViewer(portfolio.Id);
            var response = await Client.GetAsync($"/api/latest/deliveries/portfolio/{portfolio.Id}");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var keys = payload.RootElement.GetProperty("active").EnumerateArray().Single().EnumerateObject().Select(property => property.Name).ToList();

            Assert.That(keys, Is.EquivalentTo(ExpectedDeliveryPayloadKeys));
        }

        [Test]
        public async Task FromDelivery_JointRollup_AttachesNothingToTheChangeTracker()
        {
            // ADR-113 enforcement: the delivery read path is read-only over the entity graph. The
            // per-team carriers deliberately leave Team/Feature unset, which is what structurally stops
            // EF fixing these transient read-path entities onto a tracked Feature or Team.
            var portfolio = await SeedPortfolioWithTwoSingleTeamFeatures();
            await CreateDelivery(portfolio, Today.AddDays(TargetDays));

            var delivery = ServiceProvider.GetRequiredService<IDeliveryRepository>().GetAll().Single();

            var trackedBefore = DatabaseContext.ChangeTracker.Entries<WhenForecast>().Count();
            var trackedResultsBefore = DatabaseContext.ChangeTracker.Entries<IndividualSimulationResult>().Count();

            DeliveryWithLikelihoodDto.FromDelivery(delivery, DateOnly.FromDateTime(Today), []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(DatabaseContext.ChangeTracker.Entries<WhenForecast>().Count(), Is.EqualTo(trackedBefore));
                Assert.That(DatabaseContext.ChangeTracker.Entries<IndividualSimulationResult>().Count(), Is.EqualTo(trackedResultsBefore));
            }
        }

        private static WhenForecast NinetyPercentByTargetDay()
        {
            var simulation = new SimulationResult();
            simulation.SimulationResults[TargetDays] = 9000;
            simulation.SimulationResults[TailDays] = 1000;

            return new WhenForecast(simulation) { HasSufficientData = true };
        }

        private async Task<Portfolio> SeedPortfolioWithTwoSingleTeamFeatures()
        {
            var connection = new WorkTrackingSystemConnection { Name = "Connection", WorkTrackingSystem = WorkTrackingSystems.Jira };
            var alpha = new Team { Name = "Team Alpha", WorkTrackingSystemConnection = connection };
            var beta = new Team { Name = "Team Beta", WorkTrackingSystemConnection = connection };

            var teamRepository = ServiceProvider.GetRequiredService<IRepository<Team>>();
            teamRepository.Add(alpha);
            teamRepository.Add(beta);
            await teamRepository.Save();

            var checkout = FeatureForecastedBy(alpha, "Checkout", "11");
            var reporting = FeatureForecastedBy(beta, "Reporting", "12");

            var portfolio = new Portfolio { Name = "Test Portfolio", WorkTrackingSystemConnection = connection };
            portfolio.UpdateFeatures([checkout, reporting]);

            var portfolioRepository = ServiceProvider.GetRequiredService<IRepository<Portfolio>>();
            portfolioRepository.Add(portfolio);
            await portfolioRepository.Save();

            return portfolioRepository.GetAll().Single();
        }

        private static Feature FeatureForecastedBy(Team team, string name, string order)
        {
            var feature = new Feature([(team, 3, 3)]) { Name = name, Order = order };

            var forecast = NinetyPercentByTargetDay();
            forecast.TeamId = team.Id;
            feature.SetFeatureForecasts([forecast]);

            return feature;
        }

        private async Task CreateDelivery(Portfolio portfolio, DateTime date)
        {
            var featureRepository = ServiceProvider.GetRequiredService<IRepository<Feature>>();
            var featureIds = featureRepository.GetAll().Select(feature => feature.Id).ToList();

            var request = new UpdateDeliveryRequest
            {
                Name = "Q3 Launch",
                Date = date,
                FeatureIds = featureIds,
                SelectionMode = DeliverySelectionMode.Manual,
            };

            Client.AsPortfolioViewer(portfolio.Id);
            var content = JsonContent.Create(request);
            var response = await Client.PostAsync($"/api/latest/deliveries/portfolio/{portfolio.Id}", content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<DeliveryWithLikelihoodDto> GetSingleDelivery(int portfolioId)
        {
            Client.AsPortfolioViewer(portfolioId);
            var response = await Client.GetAsync($"/api/latest/deliveries/portfolio/{portfolioId}");
            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync();
            var deliveries = JsonSerializer.Deserialize<PortfolioDeliveriesDto>(body, JsonOptions)!.Active;
            return deliveries.Single();
        }
    }
}
