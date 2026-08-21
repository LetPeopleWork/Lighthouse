using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Lighthouse.Backend.Data;
using Lighthouse.Backend.Models.Dependencies;
using NUnit.Framework;
using Serilog.Events;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.API.Integration.Dependencies
{
    /// <summary>
    /// Step definitions for the two dependency settings a Portfolio owns. Backend-observable contract: a
    /// Portfolio that has set its dependencies aside hands back every one of them, unchanged, with the
    /// reason saying so - and the stored references are the same either side of the switch, because
    /// nothing about reading them depends on it.
    /// </summary>
    public partial class Slice04DependencySettingsTest : DependenciesAcceptanceTest
    {
        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private static TrackedFeature AFeatureTheTrackerHolds(string referenceId, string name)
            => new(referenceId, name, []);

        private static TrackedFeature AFeatureWaitingOn(string referenceId, string name, string[] waitsOn)
            => new(referenceId, name, waitsOn);

        private static TrackedFeature AFeatureWhoseWaitWasTypedIntoAField(
            string referenceId, string name, string[] waitsOn)
            => new(referenceId, name, waitsOn, DependencySource.PortfolioField);

        private Task GivenARefreshedPortfolio(int portfolioId, params TrackedFeature[] rowsFromTheTracker)
            => DriveAPortfolioRefresh(portfolioId, rowsFromTheTracker);

        private void GivenTheTeamBehindItHasNoMeasuredDelivery(string featureReferenceId)
            => GiveItWorkNobodyHasMeasured(featureReferenceId, remainingWorkItems: 3);

        private void GivenTheFeatureIsPlaced(string featureReferenceId, int place)
            => PlaceTheFeatureByHand(featureReferenceId, place);

        // --- When ---

        private void WhenThePortfolioSetsItsDependenciesAside(int portfolioId)
            => SetWhetherItActsOnItsDependencies(portfolioId, setThemAside: true);

        private void WhenThePortfolioActsOnThemAgain(int portfolioId)
            => SetWhetherItActsOnItsDependencies(portfolioId, setThemAside: false);

        // --- Then ---

        private async Task<List<JsonElement>> TheEntriesFor(string featureReferenceId)
        {
            var feature = await ReadTheFeatureThePayloadCarries(featureReferenceId)
                ?? throw new InvalidOperationException($"There is no {featureReferenceId} in the payload.");

            return [.. feature.GetProperty("dependsOn").EnumerateArray()];
        }

        private async Task<List<string>> TheNamesItWaitsOn(string featureReferenceId)
            => [.. (await TheEntriesFor(featureReferenceId))
                .Select(entry => entry.GetProperty("name").GetString() ?? string.Empty)
                .Order(StringComparer.Ordinal)];

        private async Task<List<string?>> TheReasonsAgainst(string featureReferenceId)
            => [.. (await TheEntriesFor(featureReferenceId)).Select(ReasonIn)];

        private static string? ReasonIn(JsonElement entry)
        {
            var reason = entry.GetProperty("notHonouredReason");

            return reason.ValueKind == JsonValueKind.Null ? null : reason.GetString();
        }

        private async Task<List<string>> EveryVerdictInThePayload()
        {
            using var response = await Client.GetAsync("/api/latest/features");
            response.EnsureSuccessStatusCode();

            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

            return [.. payload.RootElement.EnumerateArray()
                .SelectMany(feature => feature.GetProperty("dependsOn").EnumerateArray()
                    .Select(entry => string.Join(
                        "|",
                        feature.GetProperty("referenceId").GetString(),
                        entry.GetProperty("referenceId").GetString(),
                        ReasonIn(entry) ?? "nothing wrong",
                        entry.GetProperty("blockerPositionedBelow").GetBoolean())))
                .Order(StringComparer.Ordinal)];
        }

        private List<string> EveryStoredDependency()
            => [.. ReadStoredDependencies()
                .Select(stored => string.Join(
                    "|",
                    stored.FeatureReferenceId,
                    stored.KeyedToFeatureId,
                    stored.WaitsOnReferenceId,
                    stored.Source))
                .Order(StringComparer.Ordinal)];

        private bool ThePortfolioActsOnItsDependencies(int portfolioId)
        {
            using var scope = Factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<LighthouseAppContext>();

            return !context.Portfolios.AsNoTracking().Single(portfolio => portfolio.Id == portfolioId).IgnoreDependencies;
        }

        private List<string> TheWarningsTheRefreshRaised()
            => [.. CapturedLogs.AtOrAbove(LogEventLevel.Warning)];
    }
}
