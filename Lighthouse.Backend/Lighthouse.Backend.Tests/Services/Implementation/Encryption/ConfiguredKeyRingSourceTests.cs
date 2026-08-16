using Lighthouse.Backend.Services.Implementation.Encryption;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class ConfiguredKeyRingSourceTests
    {
        // Never decoded: this question is only ever about which settings were filled in, so the values are
        // written as words rather than as anything key-shaped.
        private const string AValue = "a value under one name";

        private const string AnotherValue = "a value under another name";

        // The banner only says "you are on the retired setting" when that setting is what actually answered,
        // so an operator who has already moved is never told to move again.
        [TestCase(null, null, AValue, ExpectedResult = true, TestName = "OnlyTheRetiredNameIsSet")]
        [TestCase("", "  ", AValue, ExpectedResult = true, TestName = "TheOtherTwoAreSetToNothing")]
        [TestCase(null, AnotherValue, AValue, ExpectedResult = false, TestName = "TheSingleKeyNameIsAlsoSet")]
        [TestCase(AnotherValue, null, AValue, ExpectedResult = false, TestName = "TheRingNameIsAlsoSet")]
        [TestCase(null, null, null, ExpectedResult = false, TestName = "NothingIsSetAnywhere")]
        [TestCase(null, null, "   ", ExpectedResult = false, TestName = "TheRetiredNameIsSetToWhitespace")]
        [TestCase(AnotherValue, null, null, ExpectedResult = false, TestName = "OnlyTheRingNameIsSet")]
        public bool AnsweredByTheRetiredName(string? suppliedRing, string? suppliedKey, string? retired)
        {
            return ConfiguredKeyRingSource.AnsweredByTheRetiredName(suppliedRing, suppliedKey, retired);
        }

        // Which setting an operator would have to go and edit. A setting holding nothing but spaces has
        // not answered anything - the resolution passes over it - so naming it would send somebody to
        // edit a line that is doing nothing.
        [TestCase(AnotherValue, null, null, ExpectedResult = "Encryption__Keys", TestName = "TheRingName")]
        [TestCase(null, AnotherValue, null, ExpectedResult = "Encryption__Key", TestName = "TheSingleKeyName")]
        [TestCase(null, null, AValue, ExpectedResult = "EncryptionSettings__EncryptionKey", TestName = "TheRetiredName")]
        [TestCase(AnotherValue, AValue, AValue, ExpectedResult = "Encryption__Keys", TestName = "TheRingNameWinsWhenSeveralAreSet")]
        [TestCase("   ", AnotherValue, null, ExpectedResult = "Encryption__Key", TestName = "ASettingHoldingOnlySpacesIsNotTheAnswer")]
        [TestCase(null, null, null, ExpectedResult = null, TestName = "NothingIsSetAnywhere")]
        [TestCase("  ", "  ", "  ", ExpectedResult = null, TestName = "EverySettingHoldsOnlySpaces")]
        public string? SettingThatAnswered(string? suppliedRing, string? suppliedKey, string? retired)
        {
            return ConfiguredKeyRingSource.SettingThatAnswered(suppliedRing, suppliedKey, retired);
        }

        [Test]
        public void AsAnOperatorWouldWriteIt_NoSettingAtAll_Refuses()
        {
            Assert.That(
                () => ConfiguredKeyRingSource.AsAnOperatorWouldWriteIt(null!),
                Throws.ArgumentNullException,
                "a refusal or a panel quoting an empty setting name teaches an operator nothing and looks like a bug in the product");
        }

        [Test]
        public void Resolve_NothingSuppliedUnderAnyName_HasNoAnswer()
        {
            Assert.That(new ConfiguredKeyRingSource(null, null, null).Resolve(), Is.Null);
        }

        [Test]
        public void Resolve_EverySettingSetToWhitespace_HasNoAnswer()
        {
            Assert.That(new ConfiguredKeyRingSource("  ", "\t", " ").Resolve(), Is.Null);
        }

        [Test]
        public void Resolve_ADefectiveKeyUnderTheRetiredName_SaysWhichSettingItCameFrom()
        {
            var refusal = Assert.Throws<InvalidOperationException>(
                () => new ConfiguredKeyRingSource(null, null, "not base64 at all").Resolve());

            Assert.That(refusal.Message, Does.Contain(ConfiguredKeyRingSource.RetiredSingleKeySettingKey));
        }
    }
}
