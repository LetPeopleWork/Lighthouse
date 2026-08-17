using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    /// <summary>
    /// Which place an operator put a key in is the one in force, and which others are sitting there being
    /// read by nothing. Two surfaces ask this - the startup line and the encryption settings - and they
    /// have to give the same answer, or an operator reading one and then the other is sent to two
    /// different settings.
    /// </summary>
    public class WhereTheKeyCameFromTests
    {
        private const string AKey = "a-key-value";

        private const string ARing = "k-one:a-key-value";

        private const string AKeysFile = "/etc/lighthouse/encryption/keys";

        private const string RingSetting = "Encryption__Keys";

        private const string SingleKeySetting = "Encryption__Key";

        private const string RetiredSetting = "EncryptionSettings__EncryptionKey";

        private const string KeysFileSetting = "Encryption__KeysFile";

        private static readonly string[] EveryPlaceAtOnce =
            [RingSetting, SingleKeySetting, RetiredSetting, KeysFileSetting];

        private static readonly string[] TheSingleKeyAndTheFile = [SingleKeySetting, KeysFileSetting];

        private static readonly string[] TheSingleKeyAlone = [SingleKeySetting];

        [TestCase(KeyCustody.GeneratedForThisInstance)]
        [TestCase(KeyCustody.NoDurableStore)]
        public void SettingThatAnswered_AKeyNobodySupplied_NamesNoSettingAtAll(KeyCustody custody)
        {
            var answered = WhereTheKeyCameFrom.SettingThatAnswered(custody, ARing, AKey, AKey, AKeysFile);

            Assert.That(answered, Is.Null,
                "A setting was named on an instance running on a key it keeps itself. A value can sit in a " +
                "setting without having won the resolution, and naming it sends an operator to edit " +
                "something that is not in force.");
        }

        // The order is the resolution's order, not the order somebody happened to check them in.
        [TestCase(ARing, AKey, AKey, RingSetting, TestName = "SettingThatAnswered_TheRingSettingBeatsBothOthers")]
        [TestCase(null, AKey, AKey, SingleKeySetting, TestName = "SettingThatAnswered_TheSingleKeySettingBeatsTheRetiredName")]
        [TestCase(null, null, AKey, RetiredSetting, TestName = "SettingThatAnswered_TheRetiredNameAnswersWhenItIsTheOnlyOneSet")]
        public void SettingThatAnswered_AKeyFromConfiguration_NamesTheSettingTheResolutionWouldHaveRead(
            string? ring, string? key, string? retired, string expected)
        {
            var answered = WhereTheKeyCameFrom.SettingThatAnswered(
                KeyCustody.SuppliedByConfiguration, ring, key, retired, AKeysFile);

            Assert.That(answered, Is.EqualTo(expected));
        }

        [Test]
        public void SettingThatAnswered_AKeyFromAMountedFile_NamesTheFileSettingRatherThanAnythingConfigured()
        {
            var answered = WhereTheKeyCameFrom.SettingThatAnswered(
                KeyCustody.SuppliedByExternalSecret, ARing, AKey, AKey, AKeysFile);

            Assert.That(answered, Is.EqualTo(KeysFileSetting),
                "Custody says the key came from a mounted file and a configured setting was named instead, " +
                "so the two surfaces that ask this would disagree with where the key actually came from.");
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void SettingThatAnswered_CustodyClaimingAFileThatIsNotNamed_NamesNothing(string? keysFilePath)
        {
            var answered = WhereTheKeyCameFrom.SettingThatAnswered(
                KeyCustody.SuppliedByExternalSecret, null, null, null, keysFilePath);

            Assert.That(answered, Is.Null);
        }

        [Test]
        public void EverySettingCarryingAKey_NothingSetAnywhere_IsEmpty()
        {
            Assert.That(WhereTheKeyCameFrom.EverySettingCarryingAKey(null, null, null, null), Is.Empty);
        }

        [TestCase("   ", "   ", "   ", "   ", TestName = "EverySettingCarryingAKey_SettingsHoldingOnlyBlankSpace_CarryNothing")]
        [TestCase("", "", "", "", TestName = "EverySettingCarryingAKey_SettingsHoldingAnEmptyValue_CarryNothing")]
        public void EverySettingCarryingAKey_ASettingWithNothingInIt_IsNotCounted(
            string? ring, string? key, string? retired, string? keysFile)
        {
            Assert.That(WhereTheKeyCameFrom.EverySettingCarryingAKey(ring, key, retired, keysFile), Is.Empty,
                "A setting that is present but empty is being counted as carrying a key, so an operator " +
                "who left a blank one behind is told they supplied a key twice.");
        }

        [Test]
        public void EverySettingCarryingAKey_EveryPlaceAtOnce_NamesThemAllInTheOrderTheResolutionReadsThem()
        {
            var carrying = WhereTheKeyCameFrom.EverySettingCarryingAKey(ARing, AKey, AKey, AKeysFile);

            Assert.That(carrying, Is.EqualTo(EveryPlaceAtOnce));
        }

        [Test]
        public void EverySettingCarryingAKey_OnlySomeOfThem_NamesOnlyThose()
        {
            var carrying = WhereTheKeyCameFrom.EverySettingCarryingAKey(null, AKey, null, AKeysFile);

            Assert.That(carrying, Is.EqualTo(TheSingleKeyAndTheFile));
        }

        [Test]
        public void EverySettingCarryingAKey_TheNamesItGivesBack_AreSpelledTheWayAnOperatorWritesThem()
        {
            var carrying = WhereTheKeyCameFrom.EverySettingCarryingAKey(null, AKey, null, null);

            Assert.That(carrying, Has.All.Not.Contains(":"),
                "A setting is being named in the spelling used inside the application rather than the one " +
                "an operator types, so the name they are told to go and edit does not exist for them.");
        }

        [Test]
        public void Resolve_AKeyInConfigurationWhileAFileIsAlsoNamed_ReportsBothPlacesAndTheOneInForce()
        {
            var supply = WhereTheKeyCameFrom.Resolve(
                KeyCustody.SuppliedByConfiguration, null, AKey, null, AKeysFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(supply.Settings, Is.EqualTo(TheSingleKeyAndTheFile));
                Assert.That(supply.TheOneInForce, Is.EqualTo(SingleKeySetting));
                Assert.That(supply.InMoreThanOnePlace, Is.True);
            }
        }

        [Test]
        public void Resolve_AKeyInOnePlaceOnly_IsNotSuppliedInMoreThanOnePlace()
        {
            var supply = WhereTheKeyCameFrom.Resolve(
                KeyCustody.SuppliedByConfiguration, null, AKey, null, null);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(supply.Settings, Is.EqualTo(TheSingleKeyAlone));
                Assert.That(supply.InMoreThanOnePlace, Is.False);
            }
        }

        // An instance that keeps its own key can still have settings sitting around it. Nothing won, so
        // there is nothing to tell an operator to remove in favour of anything.
        [Test]
        public void Resolve_SettingsSetOnAnInstanceRunningOnAKeyItKeepsItself_IsNotSuppliedInMoreThanOnePlace()
        {
            var supply = WhereTheKeyCameFrom.Resolve(
                KeyCustody.GeneratedForThisInstance, ARing, AKey, null, AKeysFile);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(supply.Settings, Is.Not.Empty);
                Assert.That(supply.TheOneInForce, Is.Null);
                Assert.That(supply.InMoreThanOnePlace, Is.False,
                    "An instance is being told to remove settings in favour of one that is not in force.");
            }
        }

        [Test]
        public void InMoreThanOnePlace_NothingSuppliedAnywhere_IsFalse()
        {
            Assert.That(new KeySupply([], null).InMoreThanOnePlace, Is.False);
        }
    }
}
