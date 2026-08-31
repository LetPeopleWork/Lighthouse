using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models.OptionalFeatures;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.BehaviourSettings
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Story 5876 slice 01 - a refused toggle says so.
    /// Backend-observable contract: a behaviour setting the instance's licence does not cover is refused
    /// with 403 and nothing is written; one it does cover is taken; one the licence has no opinion about
    /// is taken either way.
    /// </summary>
    public partial class Slice01PremiumRefusalTest : BehaviourSettingsAcceptanceTest
    {
        /// <summary>
        /// The shipped non-premium row, seeded by the product itself. Slice 01 must not change it and the
        /// scenarios say so in both licence states.
        /// </summary>
        private const string ShippedNonPremiumKey = OptionalFeatureKeys.DeltaSyncKey;

        /// <summary>
        /// The refusal the other door onto this setting already gives, quoted from
        /// <c>LicenseGuardAttribute</c>. The two doors have to answer a client alike, so the wording is
        /// part of the contract rather than an implementation detail.
        /// </summary>
        private const string TheRefusalTheOtherDoorGives = "Access Denied: Premium Features Required";

        // --- Given ---

        private string GivenAPremiumBehaviourSetting()
        {
            SeedPremiumOptionalFeature(PremiumFixtureKey, "A setting only a licence covers", "Exists so the premium branch can be exercised before any shipped setting reaches it.");
            return PremiumFixtureKey;
        }

        private string GivenTheShippedNonPremiumBehaviourSetting()
        {
            Assert.That(ReadStoredOptionalFeature(ShippedNonPremiumKey).Found, Is.True,
                $"The product seeds '{ShippedNonPremiumKey}'. Without it this scenario asserts nothing.");

            return ShippedNonPremiumKey;
        }

        private void GivenTheInstanceHasNoPremiumLicence() => TheInstanceIsNotLicensedForPremium();

        private void GivenTheInstanceIsLicensedForPremium()
        {
            LicenseServiceMock.Setup(s => s.CanUsePremiumFeatures()).Returns(true);
        }

        private void GivenTheInstanceLicenceState(bool licensed)
        {
            if (!licensed)
            {
                TheInstanceIsNotLicensedForPremium();
            }
        }

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenTheAdminTurnsItOn(string settingKey)
            => ToggleOptionalFeature(settingKey, enabled: true);

        private Task<(HttpStatusCode Status, string Body)> WhenTheAdminTurnsOnASettingThatDoesNotExist()
            => ToggleASettingThatDoesNotExist();

        private Task<(HttpStatusCode Status, string Body)> WhenAnyoneReadsTheBehaviourSettings()
            => GetOptionalFeatures();

        /// <summary>
        /// The door the ordering setting has before this story moves it. Guarded by the licence attribute,
        /// so on an unlicensed instance it is the refusal the new door has to end up matching.
        /// </summary>
        private Task<(HttpStatusCode Status, string Body)> WhenTheAdminHandsTheOrderOverThroughTheDoorItHasToday()
            => SetOrderingPolicyThroughTheAlias(OrderOwnedByThisInstance);

        // --- Then ---

        private static void ThenTheRefusalIsForbidden((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                $"403 specifically. The setting about to move onto this endpoint already promises 403 on the door it has today, so anything else - including a success carrying the old value - regresses a shipped criterion. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheRefusalSaysPremiumIsRequired((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Body, Does.Contain(TheRefusalTheOtherDoorGives),
                $"Both doors onto this setting must answer a client alike. Body: {Excerpt(response.Body)}");
        }

        private static void ThenBothRefusalsAreIdentical((HttpStatusCode Status, string Body) oneDoor, (HttpStatusCode Status, string Body) theOther)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(theOther.Status, Is.EqualTo(oneDoor.Status),
                    "One setting, two doors. A client that learns to handle one refusal must not meet a different one at the other.");
                Assert.That(theOther.Body.Trim(), Is.EqualTo(oneDoor.Body.Trim()),
                    "The refusal text is copied by hand between an attribute and a controller. Nothing keeps the two in step except this comparison.");
            }
        }

        private static void ThenTheToggleWasTaken((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"The write is allowed here, so the caller is told it landed. Body: {Excerpt(response.Body)}");
        }

        private static void ThenItWasReportedAsNotFound((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.NotFound),
                $"A setting nobody can name is Not Found whatever the licence says. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheSettingsAreUnchanged((HttpStatusCode Status, string Body) before, (HttpStatusCode Status, string Body) after)
        {
            var was = ParseOptionalFeatureRows(before).Select(Summarise).ToArray();
            var now = ParseOptionalFeatureRows(after).Select(Summarise).ToArray();

            Assert.That(now, Is.EqualTo(was),
                "A refused write may change nothing a reader can see, including the row it was aimed at.");
        }

        private void ThenTheStoredSettingIsStillOff(string key)
        {
            Assert.That(ReadStoredOptionalFeature(key).Enabled, Is.False,
                $"'{key}' was refused, so the store may not carry the change either.");
        }

        private void ThenTheStoredSettingIsOn(string key)
        {
            Assert.That(ReadStoredOptionalFeature(key).Enabled, Is.True,
                $"'{key}' was accepted, so the change has to have reached the store.");
        }

        private void ThenTheStoredSettingIsNotPremium(string key)
        {
            Assert.That(ReadStoredOptionalFeature(key).IsPremium, Is.False,
                $"'{key}' is not premium and this story may not make it so.");
        }

        private static string Summarise(JsonElement row)
            => $"{row.GetProperty("key").GetString()}|{row.GetProperty("enabled").GetBoolean()}|{row.GetProperty("isPremium").GetBoolean()}";
    }
}
