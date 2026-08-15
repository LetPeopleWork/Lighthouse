using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Tests.Services.Implementation.Encryption
{
    public class KeyRingSerializerTests
    {
        private const string FirstKeyId = "k-2026-08-15-01";

        private const string SecondKeyId = "k-2026-08-14-01";

        private const string LegacyKeyId = "k-legacy-default";

        private const string LongestAllowedKeyId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        private const string OneCharacterTooLongKeyId = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        // Eight characters is short enough that a leaked fragment of a key would still be a leak, and long
        // enough that a random-looking run of that length cannot turn up in a sentence by coincidence.
        private const int FragmentLength = 8;

        [Test]
        public void Parse_OneEntry_YieldsAOneEntryRingWhoseEntryIsTheKeySecretsAreWrittenUnder()
        {
            var material = MaterialFor(FirstKeyId);

            var parsed = KeyRingSerializer.TryParse($"{FirstKeyId}:{Convert.ToBase64String(material)}", out var ring, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(defect, Is.Null);
                Assert.That(ring!.ActiveKey.Id, Is.EqualTo(FirstKeyId));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
                Assert.That(ring.RetiredKeys, Is.Empty);
            }
        }

        [Test]
        public void Parse_SeveralEntries_KeepsTheirOrderWithTheFirstActiveAndEveryLaterOneRetired()
        {
            var ringText = string.Join(',', EntryFor(FirstKeyId), EntryFor(SecondKeyId), EntryFor(LegacyKeyId));

            var parsed = KeyRingSerializer.TryParse(ringText, out var ring, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(ring!.ActiveKey.Id, Is.EqualTo(FirstKeyId));
                Assert.That(ring.RetiredKeys, Has.Count.EqualTo(2));
                Assert.That(ring.RetiredKeys[0].Id, Is.EqualTo(SecondKeyId));
                Assert.That(ring.RetiredKeys[1].Id, Is.EqualTo(LegacyKeyId));
            }
        }

        [Test]
        public void Parse_EntriesPaddedWithWhitespaceAndSpreadOverLines_ReadsThemAsThoughTrimmed()
        {
            var canonical = string.Join(',', EntryFor(FirstKeyId), EntryFor(SecondKeyId));
            var spreadOverLines = $"\n  {EntryFor(FirstKeyId)} ,\r\n\t{EntryFor(SecondKeyId)}  \n";

            var parsedCanonical = KeyRingSerializer.TryParse(canonical, out var fromCanonical, out _);
            var parsedLines = KeyRingSerializer.TryParse(spreadOverLines, out var fromLines, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsedCanonical, Is.True);
                Assert.That(parsedLines, Is.True);
                Assert.That(defect, Is.Null);
                Assert.That(fromLines, Is.EqualTo(fromCanonical));
            }
        }

        [Test]
        public void Format_ThenParse_YieldsAnEqualRingAndTheOneCanonicalSpelling()
        {
            var ring = new EncryptionKeyRing(KeyFor(FirstKeyId), KeyFor(SecondKeyId));

            var formatted = KeyRingSerializer.Format(ring);
            var reparsed = KeyRingSerializer.TryParse(formatted, out var roundTripped, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(formatted, Is.EqualTo($"{EntryFor(FirstKeyId)},{EntryFor(SecondKeyId)}"));
                Assert.That(reparsed, Is.True);
                Assert.That(roundTripped, Is.EqualTo(ring));
            }
        }

        [TestCase(31)]
        [TestCase(16)]
        [TestCase(33)]
        public void Parse_MaterialThatIsNot32Bytes_IsRefusedNamingTheEntryItsLengthAndTheLengthItMustHave(int materialLength)
        {
            var encoded = Convert.ToBase64String(RandomNumberGenerator.GetBytes(materialLength));

            var parsed = KeyRingSerializer.TryParse($"{FirstKeyId}:{encoded}", out _, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(defect, Does.Contain(FirstKeyId));
                Assert.That(defect, Does.Contain(materialLength.ToString()));
                Assert.That(defect, Does.Contain(EncryptionKey.MaterialLength.ToString()));
            }
        }

        [Test]
        public void Parse_MaterialThatIsNotBase64_IsRefusedSayingItCouldNotBeDecoded()
        {
            var parsed = KeyRingSerializer.TryParse($"{FirstKeyId}:{NotBase64Material()}", out _, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(defect, Does.Contain(FirstKeyId));
                Assert.That(defect, Does.Contain("decoded"));
            }
        }

        [Test]
        public void Parse_ARingNamingTheSameKeyTwice_IsRefusedSayingWhichNameIsRepeated()
        {
            var ringText = string.Join(',', EntryFor(FirstKeyId), EntryFor(SecondKeyId), EntryFor(FirstKeyId));

            var parsed = KeyRingSerializer.TryParse(ringText, out _, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(defect, Does.Contain(FirstKeyId));
                Assert.That(defect, Does.Not.Contain(SecondKeyId));
            }
        }

        [TestCase("", TestName = "Parse_ANameThatWasSetWithNothingUnderIt_IsRefused")]
        [TestCase("   ", TestName = "Parse_ANameThatWasSetWithOnlyWhitespaceUnderIt_IsRefused")]
        public void Parse_ANameThatWasSetWithNoKeyUnderIt_IsRefusedSayingNoKeyWasSupplied(string material)
        {
            var parsed = KeyRingSerializer.TryParse($"{FirstKeyId}:{material}", out _, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(defect, Does.Contain(FirstKeyId));
                Assert.That(defect, Does.Contain("no key material"));
            }
        }

        [TestCase("K-2026-08-15-01", TestName = "Parse_AKeyNameCarryingAnUppercaseLetter_IsRefused")]
        [TestCase("k.2026.08.15", TestName = "Parse_AKeyNameCarryingADot_IsRefused")]
        [TestCase(OneCharacterTooLongKeyId, TestName = "Parse_AKeyNameOneCharacterTooLong_IsRefused")]
        public void Parse_AKeyNameOutsideWhatAKeyMayBeCalled_IsRefusedSayingWhichNameIsNotAllowed(string keyId)
        {
            var parsed = KeyRingSerializer.TryParse($"{keyId}:{Convert.ToBase64String(MaterialFor(FirstKeyId))}", out _, out var defect);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.False);
                Assert.That(defect, Does.Contain(keyId));
                Assert.That(defect, Does.Contain("not allowed"));
            }
        }

        [TestCase("a")]
        [TestCase("0")]
        [TestCase(FirstKeyId)]
        [TestCase(LegacyKeyId)]
        [TestCase(LongestAllowedKeyId)]
        public void Parse_AKeyNameAtTheEdgeOfWhatIsAllowed_IsAccepted(string keyId)
        {
            var parsed = KeyRingSerializer.TryParse($"{keyId}:{Convert.ToBase64String(MaterialFor(keyId))}", out var ring, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(ring!.ActiveKey.Id, Is.EqualTo(keyId));
            }
        }

        [Test]
        public void Parse_ARingItRefuses_NeverQuotesTheSuppliedMaterialWholeOrInFragments()
        {
            var wrongLength = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));
            var usable = Convert.ToBase64String(MaterialFor(FirstKeyId));
            var undecodable = NotBase64Material();

            using (Assert.EnterMultipleScope())
            {
                AssertQuotesNothingOf($"{FirstKeyId}:{wrongLength}", wrongLength);
                AssertQuotesNothingOf($"{FirstKeyId}:{undecodable}", undecodable);
                AssertQuotesNothingOf($"{FirstKeyId}:{usable},{FirstKeyId}:{usable}", usable);
                AssertQuotesNothingOf($"NOT-A-NAME:{usable}", usable);
                AssertQuotesNothingOf(usable[..20], usable[..20]);
            }
        }

        [Test]
        public void Parse_AKeyWithNoNameOfItsOwn_TakesANameDerivedFromTheKeyItself()
        {
            var material = MaterialFor(FirstKeyId);
            var expectedId = "k-cfg-" + Convert.ToHexString(SHA256.HashData(material))[..8].ToLowerInvariant();

            var parsed = KeyRingSerializer.TryParse(Convert.ToBase64String(material), out var ring, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed, Is.True);
                Assert.That(ring!.ActiveKey.Id, Is.EqualTo(expectedId));
                Assert.That(ring.ActiveKey.Material.ToArray(), Is.EqualTo(material));
            }
        }

        // Two pods of one deployment, and the same pod after a restart, hold the same supplied key and must
        // label what they store identically, or a secret written by one is unattributable to the other.
        [Test]
        public void Parse_TheSameKeyWithNoNameReadTwice_DerivesTheSameNameBothTimes()
        {
            var supplied = Convert.ToBase64String(MaterialFor(FirstKeyId));
            var different = Convert.ToBase64String(MaterialFor(SecondKeyId));

            var parsedFirst = KeyRingSerializer.TryParse(supplied, out var first, out _);
            var parsedAgain = KeyRingSerializer.TryParse(supplied, out var second, out _);
            var parsedOther = KeyRingSerializer.TryParse(different, out var other, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsedFirst && parsedAgain && parsedOther, Is.True);
                Assert.That(second!.ActiveKey.Id, Is.EqualTo(first!.ActiveKey.Id));
                Assert.That(other!.ActiveKey.Id, Is.Not.EqualTo(first.ActiveKey.Id));
            }
        }

        [TestCase((string?)null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase(",")]
        [TestCase(":")]
        [TestCase("::::")]
        [TestCase(",,,,")]
        [TestCase("k-one:")]
        [TestCase(":AAAA")]
        [TestCase("k-one:a:b")]
        [TestCase("k-one")]
        [TestCase("=")]
        [TestCase("k-é:AAAA")]
        [TestCase("k-one:AAAA,")]
        [TestCase(",k-one:AAAA")]
        public void TryParse_AnyStringAtAll_ReturnsAnOutcomeRatherThanThrowing(string? value)
        {
            var parsed = false;
            EncryptionKeyRing? ring = null;
            string? defect = null;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => parsed = KeyRingSerializer.TryParse(value, out ring, out defect), Throws.Nothing);
                Assert.That(ring is not null, Is.EqualTo(parsed));
                Assert.That(defect is null, Is.EqualTo(parsed));
            }
        }

        private static void AssertQuotesNothingOf(string ringText, string material)
        {
            var accepted = KeyRingSerializer.TryParse(ringText, out _, out var defect);

            Assert.That(accepted ? "the ring was not refused at all" : WhatIsWrongWith(defect, material), Is.Null, ringText);
        }

        private static string? WhatIsWrongWith(string? defect, string material)
        {
            if (defect is null)
            {
                return "the ring was not refused at all";
            }

            var unpadded = material.TrimEnd('=');

            if (defect.Contains(material, StringComparison.Ordinal) || defect.Contains(unpadded, StringComparison.Ordinal))
            {
                return "the complaint quotes the supplied material";
            }

            for (var start = 0; start + FragmentLength <= unpadded.Length; start++)
            {
                if (defect.Contains(unpadded.AsSpan(start, FragmentLength), StringComparison.Ordinal))
                {
                    return "the complaint quotes a fragment of the supplied material";
                }
            }

            return null;
        }

        private static string NotBase64Material()
        {
            var usable = Convert.ToBase64String(MaterialFor(LegacyKeyId));

            return string.Concat(usable.AsSpan(0, 10), "!!", usable.AsSpan(12));
        }

        private static string EntryFor(string keyId)
        {
            return $"{keyId}:{Convert.ToBase64String(MaterialFor(keyId))}";
        }

        private static EncryptionKey KeyFor(string keyId)
        {
            return new EncryptionKey(keyId, MaterialFor(keyId));
        }

        private static byte[] MaterialFor(string seed)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        }
    }
}
