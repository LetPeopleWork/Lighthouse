using System.Net;
using System.Text.Json;
using Lighthouse.Backend.Models.OptionalFeatures;
using Lighthouse.Backend.Services.Implementation.UsageData;
using Lighthouse.Backend.Tests.API.Integration.BehaviourSettings;
using NUnit.Framework;

namespace Lighthouse.Backend.Tests.API.Integration.UsageDataVeto
{
    /// <summary>
    /// DISTILL step definitions (Specifications) for Epic 5733 slice 03 - the administrator's veto as a
    /// setting. Backend-observable contract: the veto exists as a premium behaviour setting, ships
    /// disengaged, survives an upgrade in whatever state an administrator left it, and is refused out
    /// loud on an instance whose licence does not cover it.
    /// <para>
    /// The harness is story #5876's, reused rather than copied. It is not a behaviour-settings harness
    /// by anything except its name: it is the real host on real SQLite with the licence port faked, which
    /// is exactly what these scenarios need, and a second copy of it would be 250 lines that drift.
    /// </para>
    /// </summary>
    public partial class Slice03VetoSettingTest : BehaviourSettingsAcceptanceTest
    {

        /// <summary>
        /// The key the veto is stored under, read from the switch that governs it rather than written
        /// out again here. The two being renamed apart is the failure this const exists to prevent, and a
        /// test quoting the string would be one of the places that could drift.
        /// </summary>
        private const string VetoKey = UsageDataMasterSwitch.Key;

        /// <summary>
        /// A shipped setting the licence has no opinion about. Used to hold AC-07.3: the premium branch
        /// may not spread to a row that was never premium.
        /// </summary>
        private const string ShippedNonPremiumKey = OptionalFeatureKeys.DeltaSyncKey;

        // --- Given ---

        private void GivenTheCallerAdministersTheInstance() => TheCallerAdministersTheWholeInstance();

        private void GivenTheInstanceHasNoPremiumLicence() => TheInstanceIsNotLicensedForPremium();

        private void GivenTheInstanceIsLicensedForPremium()
        {
            LicenseServiceMock.Setup(licence => licence.CanUsePremiumFeatures()).Returns(true);
        }

        /// <summary>
        /// A premium row of this fixture's own making, for the one scenario that checks the inherited
        /// refusal itself. It deliberately does not use the veto row: that row is what the refusal is
        /// being checked on behalf of, so asserting the gate through it would assume what it proves.
        /// </summary>
        private string GivenAPremiumBehaviourSetting()
        {
            SeedPremiumOptionalFeature(
                PremiumFixtureKey,
                "A setting only a licence covers",
                "Exists so the premium refusal can be checked without the row that depends on it.");

            return PremiumFixtureKey;
        }

        private string GivenTheShippedSettingThatIsNotPremium()
        {
            Assert.That(ReadStoredOptionalFeature(ShippedNonPremiumKey).Found, Is.True,
                $"The product seeds '{ShippedNonPremiumKey}'. Without it this scenario asserts nothing.");

            return ShippedNonPremiumKey;
        }

        // --- When ---

        private Task<(HttpStatusCode Status, string Body)> WhenAnyoneReadsTheBehaviourSettings()
            => GetOptionalFeatures();

        private Task<(HttpStatusCode Status, string Body)> WhenTheAdminEngagesTheVeto()
            => ToggleOptionalFeature(VetoKey, enabled: true);

        private Task<(HttpStatusCode Status, string Body)> WhenTheAdminTurnsItOn(string settingKey)
            => ToggleOptionalFeature(settingKey, enabled: true);

        /// <summary>
        /// What an upgrade is, as far as a stored setting can tell: the seeders run again against a
        /// database that already has rows in it.
        /// </summary>
        private void WhenTheInstanceIsUpgraded() => RunEverySeeder();

        // --- Then ---

        private static void ThenTheVetoIsAmongThem((HttpStatusCode Status, string Body) response)
        {
            var keys = ParseOptionalFeatureRows(response)
                .Select(row => row.GetProperty("key").GetString())
                .ToArray();

            Assert.That(keys, Does.Contain(VetoKey),
                "no setting governs usage data, so the master switch finds no row, reads that as "
                + $"nothing-vetoed, and no administrator has a control to reach. Offered: {string.Join(", ", keys)}");
        }

        private static void ThenTheVetoReadsAsDisengagedAndPremium((HttpStatusCode Status, string Body) response)
        {
            var veto = TheVetoRowIn(response);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(veto.GetProperty("enabled").GetBoolean(), Is.False,
                    "an administrator who cannot change this still has to be able to see what it is "
                    + "doing, and what it is doing is nothing");
                Assert.That(veto.GetProperty("isPremium").GetBoolean(), Is.True,
                    "without the premium marking the control renders as one this administrator could "
                    + "use, and the refusal arrives only after they have tried");
            }
        }

        private void ThenTheStoredVetoIsDisengaged()
        {
            var stored = ReadStoredOptionalFeature(VetoKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Found, Is.True,
                    $"nothing is stored under '{VetoKey}', so there is no default to be right or wrong");
                Assert.That(stored.Enabled, Is.False,
                    "usage data is vetoed on a fresh instance, so nobody is ever asked and nothing is "
                    + "ever sent - and the seeder never overwrites a stored value, so no later release "
                    + "can put this right");
            }
        }

        private void ThenTheStoredVetoIsEngaged()
        {
            Assert.That(ReadStoredOptionalFeature(VetoKey).Enabled, Is.True,
                "the administrator's decision to stop usage data did not survive, so the instance is "
                + "sending again with nothing on screen saying so");
        }

        /// <summary>
        /// AC-06.6. Asserted on the seeded row rather than on a rendered page, because the row is
        /// where the words are written and the settings table renders whatever it holds.
        /// </summary>
        private void ThenTheVetoExplainsSuspendAndResume()
        {
            var description = ReadStoredOptionalFeature(VetoKey).Description ?? string.Empty;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(description, Does.Contain("sends no usage data").IgnoreCase,
                    "the description never says what switching it on actually does, so an "
                    + "administrator is left inferring it from the name");
                // "nobody is asked about it", not "nobody is asked" - the shorter phrase is also in
                // the promise at the end that nobody is asked again, so it held while this half of
                // the sentence was missing entirely.
                Assert.That(description, Does.Contain("nobody is asked about it").IgnoreCase,
                    "it stops the sending and the asking, and an administrator who only learns "
                    + "about the first will still see the dialog appear and think it failed");
                Assert.That(description, Does.Contain("suspended").IgnoreCase,
                    "an administrator reading this cannot tell whether flipping it throws away what "
                    + "everybody already answered, which is the fear that stops them using it at all");
                Assert.That(description, Does.Contain("resumes").IgnoreCase,
                    "nothing says the instance starts sending again when this is turned back off, so "
                    + "the switch reads as one-way");
                Assert.That(description, Does.Contain("asked again").IgnoreCase,
                    "nothing promises that turning it back off does not put the question to everybody "
                    + "a second time - which is the cost an administrator is weighing");
            }
        }

        /// <summary>
        /// The one row in that table whose "on" means stop. Every neighbour reads positively, so the
        /// name has to carry the negation without the description underneath it.
        /// </summary>
        private void ThenTheVetoIsNamedForWhatItStops()
        {
            var stored = ReadStoredOptionalFeature(VetoKey);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stored.Name, Does.Contain("never").IgnoreCase,
                    $"'{stored.Name}' does not say that switching it on stops anything, and it sits "
                    + "beside switches whose on means the feature runs");
                Assert.That(stored.IsPreview, Is.False,
                    "a preview chip on a privacy control invites an administrator to discount it");
            }
        }

        private void ThenTheVetoIsPremium()
        {
            Assert.That(ReadStoredOptionalFeature(VetoKey).IsPremium, Is.True,
                "the veto is the premium half of this feature; unmarked, every instance can engage it");
        }

        private static void ThenTheToggleWasTaken((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.OK),
                $"the write is allowed here, so the caller is told it landed. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheRefusalIsForbidden((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Status, Is.EqualTo(HttpStatusCode.Forbidden),
                "403 specifically. A 200 carrying the unchanged row tells an administrator they have "
                + $"stopped usage data when they have not. Body: {Excerpt(response.Body)}");
        }

        private static void ThenTheRefusalSaysPremiumIsRequired((HttpStatusCode Status, string Body) response)
        {
            Assert.That(response.Body, Does.Contain(TheRefusalPremiumSettingsGive),
                $"the refusal has to name its reason, or it reads as a fault. Body: {Excerpt(response.Body)}");
        }

        private void ThenTheStoredSettingIsStillOff(string key)
        {
            Assert.That(ReadStoredOptionalFeature(key).Enabled, Is.False,
                $"'{key}' was refused, so the store may not carry the change either");
        }

        private static void ThenTheOnlySettingThatChangedIsTheVeto(
            (HttpStatusCode Status, string Body) before, (HttpStatusCode Status, string Body) after)
        {
            var was = EverySettingExceptTheVeto(before);
            var now = EverySettingExceptTheVeto(after);

            Assert.That(now, Is.EqualTo(was),
                "writing the veto changed another setting. Nothing is registered to act on this key, so "
                + "storing the value is the whole of what writing it may do");
        }

        private static JsonElement TheVetoRowIn((HttpStatusCode Status, string Body) response)
        {
            var rows = ParseOptionalFeatureRows(response)
                .Where(row => row.GetProperty("key").GetString() == VetoKey)
                .ToArray();

            Assert.That(rows, Has.Length.EqualTo(1),
                $"exactly one row governs usage data. Found {rows.Length}, and a second one makes the "
                + "toggle answer for whichever the store happened to return first");

            return rows[0];
        }

        private static string[] EverySettingExceptTheVeto((HttpStatusCode Status, string Body) response)
        {
            return [.. ParseOptionalFeatureRows(response)
                .Where(row => row.GetProperty("key").GetString() != VetoKey)
                .Select(row => $"{row.GetProperty("key").GetString()}|{row.GetProperty("enabled").GetBoolean()}")
                .OrderBy(summary => summary, StringComparer.Ordinal)];
        }
    }
}
