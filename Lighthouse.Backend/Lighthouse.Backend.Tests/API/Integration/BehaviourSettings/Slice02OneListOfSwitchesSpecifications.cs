using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models.AppSettings;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Interfaces.Update;
using Moq;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Story 5876 slice 02 - one list of switches.
    /// Backend-observable contract: the ordering choice lives in the behaviour-settings table, an instance
    /// that already owned its order still owns it after the upgrade with every place intact, turning it on
    /// moves nobody, both transitions re-queue the forecasts, and the deprecated door writes the same store.
    /// </summary>
    public partial class Slice02OneListOfSwitchesTest : BehaviourSettingsAcceptanceTest
    {
        private const string ShippedNonPremiumKey = OptionalFeatureKeys.DeltaSyncKey;

        private readonly record struct ListedFeature(int Id, string Name, int Position);

        /// <summary>
        /// Four Features whose tracker rank runs the opposite way to the order the rows were created in.
        /// This is what makes the seeding scenarios discriminating: a seed that numbers an all-null set by
        /// row id produces a different list, and a fixture ranked in creation order would hide that.
        /// </summary>
        private void GivenFeaturesTheTrackerRankedBackwards(int portfolioId)
        {
            SeedFeature("Rebuild the search index", "FTR-1", "40", manualRank: null, portfolioId);
            SeedFeature("Retire the legacy importer", "FTR-2", "30", manualRank: null, portfolioId);
            SeedFeature("Publish the partner catalogue", "FTR-3", "20", manualRank: null, portfolioId);
            SeedFeature("Split the billing service", "FTR-4", "10", manualRank: null, portfolioId);
        }

        // --- Given ---

        private int GivenAPortfolio(string name) => SeedPortfolio(name);

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        private List<(string ReferenceId, int? ManualRank, string SourceOrder)> GivenTheOrderingPlacesAsTheyStandNow()
            => ReadStoredOrderingColumns();

        /// <summary>
        /// A Feature that shows up after the switch has already been flipped, so it carries no place. This
        /// is the only shape that can tell a seed which runs on both transitions from one which runs on the
        /// way out only — once every row holds a place, a second seed is a no-op and proves nothing.
        /// </summary>
        private void GivenAFeatureArrivesLater(string name, string referenceId, string sourceOrder, int portfolioId)
            => SeedFeature(name, referenceId, sourceOrder, manualRank: null, portfolioId);

        private void GivenTheLicenceLapses() => TheInstanceIsNotLicensedForPremium();

        private (bool Found, bool Enabled, bool IsPremium, bool IsPreview, string Name, string Description) GivenTheShippedNonPremiumSettingAsItReadsNow()
            => ReadStoredOptionalFeature(ShippedNonPremiumKey);

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheProductOwnerOpensTheFeaturesView() => GetAllFeatures();

        private async Task WhenTheAdminHandsTheOrderOverInBehaviourSettings()
        {
            var response = await ToggleOptionalFeature(FeatureOrderingOptionalFeatureKey, enabled: true);
            AssertTheToggleWasTaken(response, "on");
        }

        private async Task WhenTheAdminGivesTheOrderBackInBehaviourSettings()
        {
            var response = await ToggleOptionalFeature(FeatureOrderingOptionalFeatureKey, enabled: false);
            AssertTheToggleWasTaken(response, "off");
        }

        private async Task WhenTheAdminHandsTheOrderOverThroughTheDeprecatedAlias()
        {
            var response = await SetOrderingPolicyThroughTheAlias(OrderOwnedByThisInstance);
            AssertTheToggleWasTaken(response, "on through the deprecated door");
        }

        private void WhenTheInstanceUpgradesFrom(string? storedPolicyBeforeTheUpgrade)
            => SeedInstanceAsItWasBeforeTheUpgrade(storedPolicyBeforeTheUpgrade);

        // --- Then ---

        private static void ThenTheListIsUnchanged((HttpStatusCode Status, string Body) before, (HttpStatusCode Status, string Body) after)
        {
            // Name and place together, as one sequence. The places alone say nothing: the whole table is
            // numbered from one in the order it comes back, so the numbers read 1..N whatever moved.
            var wasListed = ParseListedFeatures(before).Select(f => $"{f.Name} at {f.Position}").ToArray();
            var isListed = ParseListedFeatures(after).Select(f => $"{f.Name} at {f.Position}").ToArray();

            Assert.That(isListed, Is.EqualTo(wasListed),
                "Nobody may move. A switch that reshuffles the list on the way in is indistinguishable from a bug, and is what happens when the places are seeded after the setting has already flipped.");
        }

        private void ThenThePlacesFollowTheTrackersOrderRatherThanTheRowIds()
        {
            var stored = ReadStoredOrderingColumns();

            var byPlace = stored
                .Where(row => row.ManualRank.HasValue)
                .OrderBy(row => row.ManualRank!.Value)
                .Select(row => row.ReferenceId)
                .ToArray();

            string[] theOrderTheTrackerRanked = ["FTR-4", "FTR-3", "FTR-2", "FTR-1"];

            Assert.That(byPlace, Is.EqualTo(theOrderTheTrackerRanked),
                "The places must be read off the sequence the admin was looking at. Row-id order here means the seed ran after the setting flipped, which is the one failure this design exists to prevent.");
        }

        private void ThenTheFeatureThatArrivedLaterStillHasNoPlace(string referenceId)
        {
            var stored = ReadStoredOrderingColumns().Single(row => row.ReferenceId == referenceId);

            Assert.That(stored.ManualRank, Is.Null,
                "Handing the order back is not a moment to hand out places. A null place is how this instance records that a Feature arrived while it was not choosing the order, and it is what makes taking the order over again append rather than renumber.");
        }

        private void ThenTheOrderingSettingReadsOn() => AssertTheOrderingSettingReads(true);

        private void ThenTheOrderingSettingReadsOff() => AssertTheOrderingSettingReads(false);

        private void AssertTheOrderingSettingReads(bool expectedToBeOn)
        {
            var stored = ReadStoredOptionalFeature(FeatureOrderingOptionalFeatureKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Found, Is.True, "The ordering setting has to be one of the rows in the behaviour-settings table.");
                Assert.That(stored.IsPremium, Is.True, "It is a premium setting, and the refusal slice 01 shipped is what makes that safe to say.");
                Assert.That(stored.Enabled, Is.EqualTo(expectedToBeOn));
            }
        }

        private void ThenThisInstanceStillOwnsTheOrder() => AssertTheOrderingPolicyReads(OrderOwnedByThisInstance);

        private void ThenTheTrackerStillOwnsTheOrder() => AssertTheOrderingPolicyReads(OrderOwnedByTheTracker);

        private void AssertTheOrderingPolicyReads(string expectedPolicy)
        {
            var response = GetOrderingPolicy().GetAwaiter().GetResult();

#pragma warning disable NUnit2045 // Guard-then-parse: the JSON read below would throw over the clear message under Assert.Multiple.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The ordering read port names the position column on every feature list and stays open to every caller. Body: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            using var document = JsonDocument.Parse(response.Body);
            var policy = document.RootElement.GetProperty("policy").GetString();

            Assert.That(policy, Is.EqualTo(expectedPolicy),
                "One store behind two doors. The read port has to report what the behaviour setting says, whichever door wrote it.");
        }

        private void ThenTheForecastsWereReQueuedFor(int portfolioId, int times)
        {
            ForecastUpdaterMock.Verify(updater => updater.TriggerImmediateUpdate(portfolioId), Times.Exactly(times));
        }

        private void ThenTheStoredPlacesAreUnchangedFrom(List<(string ReferenceId, int? ManualRank, string SourceOrder)> before)
        {
            Assert.That(ReadStoredOrderingColumns(), Is.EqualTo(before),
                "The places this instance chose are its own. Nothing in the move, and nothing in the upgrade, may renumber them.");
        }

        private void ThenTheShippedNonPremiumSettingStillReads((bool Found, bool Enabled, bool IsPremium, bool IsPreview, string Name, string Description) asShipped)
        {
            Assert.That(ReadStoredOptionalFeature(ShippedNonPremiumKey), Is.EqualTo(asShipped),
                "Faster Updates is not premium, and the only thing this story changes for it is the heading above the table.");
        }

        private void ThenTheSettingItMigratedFromIsStillStored(string expectedValue)
        {
            Assert.That(ReadStoredAppSetting(AppSettingKeys.FeatureOrderingPolicy), Is.EqualTo(expectedValue),
                "Additive only. The row it migrated from stops being read; it is not deleted, because a migration that turns out wrong has no way back once it is gone.");
        }

        // --- Parsing ---

        private static void AssertTheToggleWasTaken((HttpStatusCode Status, string Body) response, string direction)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"Turning the ordering setting {direction} must be accepted - every assertion after this rests on it having been taken. Body: {Excerpt(response.Body)}");
        }

        private static List<ListedFeature> ParseListedFeatures((HttpStatusCode Status, string Body) response)
        {
#pragma warning disable NUnit2045 // Guard-then-parse: the JSON read below would throw over the clear message under Assert.Multiple.
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The Features view read port must answer. Body: {Excerpt(response.Body)}");
            Assert.That(response.Body.TrimStart(), Does.StartWith("["),
                $"The read port must return a JSON array. Body starts: {Excerpt(response.Body)}");
#pragma warning restore NUnit2045

            using var document = JsonDocument.Parse(response.Body);

            return [.. document.RootElement
                .EnumerateArray()
                .Select(element => new ListedFeature(
                    element.GetProperty("id").GetInt32(),
                    element.GetProperty("name").GetString() ?? string.Empty,
                    element.GetProperty("position").GetInt32()))];
        }
    }
}
