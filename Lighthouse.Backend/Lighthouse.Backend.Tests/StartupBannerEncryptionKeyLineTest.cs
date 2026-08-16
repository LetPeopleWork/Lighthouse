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
                RingUnder(KeyCustody.GeneratedForThisInstance), KeptIn(), keyCameFromTheRetiredSetting: false);

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
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(custody), KeptIn(), keyCameFromTheRetiredSetting: false);

            Assert.That(lines[0], Does.Contain(expectedSource));
        }

        [Test]
        public void NowhereDurableToKeepAKey_SaysSoInASecondLineThatNamesBothWaysOut()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.NoDurableStore), KeptIn(), keyCameFromTheRetiredSetting: false);

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

        [Test]
        public void AKeyStillSetUnderTheRetiredName_IsSaidSoAndAskedToMove()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration), KeptIn(), keyCameFromTheRetiredSetting: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(2));
                Assert.That(lines[1], Does.Contain("EncryptionSettings__EncryptionKey"));
                Assert.That(lines[1], Does.Contain("Encryption__Key"));
            }
        }

        [Test]
        public void AKeySetUnderTheNameInUse_IsNotAskedToMoveAnywhere()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration), KeptIn(), keyCameFromTheRetiredSetting: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(1));
                Assert.That(lines[0], Does.Not.Contain("EncryptionSettings"));
            }
        }

        // An instance running past the refusal looks entirely normal from every other angle, and the
        // operator who set the switch is rarely the one who finds it still set months later.
        [Test]
        public void AnInstanceStartedPastTheRefusal_SaysSoOnEveryStart()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(2));
                Assert.That(lines[1], Does.StartWith("🚨"),
                    "the banner is a wall of text and the emoji is how a line is found in it");
                Assert.That(lines[1], Does.Contain("This instance was started with"));
                Assert.That(lines[1], Does.Contain("Encryption__StartEvenIfNothingStoredCanBeRead"));
                Assert.That(lines[1], Does.Contain("has to be entered again"),
                    "the line is only useful if it says what the operator still owes");
                Assert.That(lines[1], Does.Contain("Remove the setting once they have been"),
                    "a notice that never says how to make itself go away is one people learn to scroll past");
            }
        }

        [Test]
        public void AnInstanceThatNeverNeededTheSwitch_IsNotToldAboutIt()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance),
                KeptIn(),
                keyCameFromTheRetiredSetting: false);

            Assert.That(lines, Has.Count.EqualTo(1),
                "a healthy install is not taught to worry about a hatch it never opened");
        }

        // Every line is found by its emoji before it is read by its label - the banner is a wall of text
        // in a console, and an operator scanning it for the encryption row is looking for the key.
        [TestCase("🌐", "Url")]
        [TestCase("🖥️", "OS")]
        [TestCase("⚙️", "Runtime")]
        [TestCase("🧩", "Architecture")]
        [TestCase("🔢", "Process ID")]
        [TestCase("💾", "Database")]
        [TestCase("📝", "Logs")]
        [TestCase("🔑", "Encryption")]
        public void EveryBannerRow_IsFoundByItsOwnMarkerAsWellAsItsLabel(string marker, string label)
        {
            var lines = WholeBannerUnder(KeyCustody.GeneratedForThisInstance);

            Assert.That(
                lines.Where(line => line.Contains(LabelColumn(label), StringComparison.Ordinal)),
                Has.Some.StartWith(marker),
                $"the {label} row lost the marker an operator scans for");
        }

        [Test]
        public void AWarningRow_IsMarkedApartFromTheRowItFollows()
        {
            var nowhereDurable = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.NoDurableStore), KeptIn(), keyCameFromTheRetiredSetting: false);
            var retiredName = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration), KeptIn(), keyCameFromTheRetiredSetting: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(nowhereDurable[1], Does.StartWith("⚠️"));
                Assert.That(nowhereDurable[1], Does.Contain(LabelColumn("Warning")));
                Assert.That(retiredName[1], Does.StartWith("⚠️"));
                Assert.That(retiredName[1], Does.Contain(LabelColumn("Warning")));
            }
        }

        [Test]
        public void TheCustodyLines_RefuseToBeBuiltWithoutARingOrAKeyStore()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => StartupBanner.BuildEncryptionCustodyLines(null!, KeptIn(), false),
                    Throws.ArgumentNullException);
                Assert.That(
                    () => StartupBanner.BuildEncryptionCustodyLines(RingUnder(KeyCustody.GeneratedForThisInstance), null!, false),
                    Throws.ArgumentNullException);
            }
        }

        // The version is the first thing anyone is asked for when they report a problem, and it is the one
        // line of the banner carried by no label, so nothing else here would notice it going missing.
        [Test]
        public void TheBanner_NamesTheVersionItIsRunning()
        {
            var lines = WholeBannerUnder(KeyCustody.GeneratedForThisInstance);

            Assert.That(lines, Has.Some.Contains("Lighthouse 1.2.3.4"));
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
                KeptIn(),
                false));
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
