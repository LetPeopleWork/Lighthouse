using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using Lighthouse.Backend.Startup;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Tests
{
    public class StartupBannerEncryptionKeyLineTest
    {
        private const string ActiveKeyId = "k-2026-08-14-01";

        private const string KeyStoreDirectory = "/app/Data/keys";

        private const string EncryptionLabel = "Encryption";

        private const int FragmentLength = 8;

        private static readonly byte[] ActiveMaterial = RandomNumberGenerator.GetBytes(EncryptionKey.MaterialLength);

        private static readonly string[] EveryOtherBannerLabel =
        [
            "Url",
            "OS",
            "Runtime",
            "Architecture",
            "Process ID",
            "Database",
            "Logs",
            "Authentication",
            "Authorization",
        ];

        [Test]
        public void EncryptionLine_NamesWhereTheKeyCameFromWhatItIsCalledAndWhereItIsKept()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance), KeptIn());

            Assert.That(lines, Has.Some.Contains(EncryptionLabel)
                .And.Contains("generated for this instance")
                .And.Contains(ActiveKeyId)
                .And.Contains(KeyStoreDirectory));
        }

        /// <summary>
        /// The banner is the one place the key ring is described in prose, so it is the one place a
        /// convenient "here is the key we ended up on" could be added without anybody noticing. Searching
        /// for short fragments as well as the whole value catches a truncated or partially masked rendering,
        /// which reads as safe and is not.
        /// </summary>
        [Test]
        public void Banner_CarriesNoPartOfTheKeyMaterialInAnyEncodingOrFragment()
        {
            var banner = string.Join(Environment.NewLine, WholeBannerUnder(KeyCustody.GeneratedForThisInstance));

            var disclosed = EveryWayTheKeyCouldBeWrittenDown()
                .Where(fragment => banner.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.That(disclosed, Is.Empty,
                "The startup banner carries the encryption key, or a piece of it, into whatever console or " +
                "log file the operator reads it from. Found: " + string.Join(", ", disclosed));
        }

        [TestCase(KeyCustody.GeneratedForThisInstance, "generated for this instance")]
        [TestCase(KeyCustody.SuppliedByConfiguration, "supplied by configuration")]
        [TestCase(KeyCustody.SuppliedByExternalSecret, "supplied by a mounted secret file")]
        [TestCase(KeyCustody.NoDurableStore, "the key published with the product")]
        public void EncryptionLine_TellsEachCustodyApartByItsWordingAlone(KeyCustody custody, string expectedSource)
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(RingUnder(custody), KeptIn());

            Assert.That(lines[0], Does.Contain(expectedSource));
        }

        [Test]
        public void NowhereDurableToKeepAKey_SaysSoInASecondLineThatNamesBothWaysOut()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(RingUnder(KeyCustody.NoDurableStore), KeptIn());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(2));
                Assert.That(lines[1], Does.Contain("Encryption__Key"));
                Assert.That(lines[1], Does.Contain("Encryption__KeyStorePath"));
            }
        }

        [Test]
        public void EveryOtherLineOfTheBanner_IsUnchangedAndExactlyOneLineIsAboutEncryption()
        {
            var lines = WholeBannerUnder(KeyCustody.GeneratedForThisInstance);

            var missing = EveryOtherBannerLabel
                .Where(label => !lines.Any(line => line.Contains(LabelColumn(label), StringComparison.Ordinal)))
                .ToList();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(missing, Is.Empty,
                    "A line the banner used to carry is gone. Found missing: " + string.Join(", ", missing));
                Assert.That(
                    lines.Count(line => line.Contains(LabelColumn(EncryptionLabel), StringComparison.Ordinal)),
                    Is.EqualTo(1));
            }
        }

        private static string LabelColumn(string label)
        {
            return $"{label,-13} :";
        }

        private static List<string> EveryWayTheKeyCouldBeWrittenDown()
        {
            var base64 = Convert.ToBase64String(ActiveMaterial);

            return
            [
                base64,
                base64.TrimEnd('='),
                Convert.ToHexString(ActiveMaterial),
                .. Enumerable
                    .Range(0, base64.Length - FragmentLength + 1)
                    .Select(start => base64.Substring(start, FragmentLength)),
            ];
        }

        private static IReadOnlyList<string> WholeBannerUnder(KeyCustody custody)
        {
            return StartupBanner.BuildInfoLines(new StartupBannerFacts(
                "1.2.3.4",
                ["http://localhost:8080"],
                "sqlite",
                "/app/logs",
                new ConfigurationBuilder().Build(),
                RingUnder(custody),
                KeptIn()));
        }

        private static EncryptionKeyRing RingUnder(KeyCustody custody)
        {
            return new EncryptionKeyRing(custody, new EncryptionKey(ActiveKeyId, ActiveMaterial));
        }

        private static KeyStoreLocation KeptIn()
        {
            return new KeyStoreLocation(KeyStoreDirectory, KeyStoreCase.ExplicitKeyStorePath);
        }
    }
}
