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

        private const string TheSettingInForce = "Encryption__Key";

        private const string TheSettingBeingIgnored = "EncryptionSettings__EncryptionKey";

        private static readonly string[] EverySettingTheOperatorSet = [TheSettingInForce, TheSettingBeingIgnored];

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
        public void EncryptionLine_NamesWhoseKeyItIsAndWhereItIsKept()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance), KeptIn(), keyCameFromTheRetiredSetting: false);

            Assert.That(lines, Has.Some.Contains(EncryptionLabel)
                .And.Contains("instance")
                .And.Contains(KeyStoreDirectory));
        }

        // Every other line of this banner is a label and a short value; this one was a clause. The key id
        // came off it because the moment it is worth having is a start that stopped - and the refusal
        // that stops one now names both keys itself.
        [Test]
        public void EncryptionLine_CarriesNoKeyId()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance), KeptIn(), keyCameFromTheRetiredSetting: false);

            Assert.That(lines[0], Does.Not.Contain(ActiveKeyId));
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

        [TestCase(KeyCustody.GeneratedForThisInstance, "instance")]
        [TestCase(KeyCustody.SuppliedByConfiguration, "configured")]
        [TestCase(KeyCustody.SuppliedByExternalSecret, "mounted secret")]
        [TestCase(KeyCustody.NoDurableStore, "published key")]
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

        // The instance every pre-release install becomes after an in-app update: the settings file it kept
        // still holds the value Lighthouse used to ship, so the instance ignored it and made its own key.
        // It has to be told, or the dead setting sits there forever and the next operator to read it
        // copies it to the name that does refuse.
        [Test]
        public void ThePublishedKeyLeftInTheSettingsFile_IsSaidSoAndNamesTheFileAndTheBlockToDelete()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.GeneratedForThisInstance),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: null,
                thePublishedKeyWasLeftInTheSettingsFile: true);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(2));
                Assert.That(lines[1], Does.Contain(TheSettingBeingIgnored));
                Assert.That(lines[1], Does.Contain("appsettings.json"),
                    "an operator told to remove a setting and not told which file holds it has been sent looking");
                Assert.That(lines[1], Does.Contain("stays readable"),
                    "the first thing this reads like is that something was lost, and nothing was");
                Assert.That(lines[1], Does.Contain("Do not copy its value to Encryption__Key"),
                    "it is the one move that turns a started instance back into one that refuses to start");
            }
        }

        // The flag only says the shipped value was found, not that it decided anything. An instance whose
        // key came from somewhere else is still on that key, and telling it that it made one of its own
        // contradicts the custody line directly above and the more-than-one-place line below it.
        [TestCase(KeyCustody.SuppliedByConfiguration)]
        [TestCase(KeyCustody.SuppliedByExternalSecret)]
        [TestCase(KeyCustody.NoDurableStore)]
        public void ThePublishedKeyLeftInTheSettingsFile_OnAnInstanceThatIsNotOnAKeyOfItsOwn_IsNotClaimed(
            KeyCustody custody)
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(custody),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: null,
                thePublishedKeyWasLeftInTheSettingsFile: true);

            Assert.That(lines, Has.None.Contains("a key of its own"));
        }

        // An instance that ignored the shipped value is not on the retired name, so it must not also be
        // told to move off it - that nudge says to copy the value across, which is what would stop it.
        [Test]
        public void ThePublishedKeyLeftInTheSettingsFile_IsNotAlsoAskedToMoveOntoTheNewName()
        {
            Assert.That(
                ConfiguredKeyRingSource.AnsweredByTheRetiredName(null, null, ThePublishedKeyEncoded()),
                Is.False);
        }

        private static string ThePublishedKeyEncoded()
        {
            var scaffold = new EncryptionKeyRing(
                new EncryptionKey("k-not-the-published-one", new byte[EncryptionKey.MaterialLength]));

            return Convert.ToBase64String(scaffold.WithLegacyDefault().RetiredKeys[0].Material.Span);
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

        // An operator moving a key from a setting into a file their secret store owns leaves the old
        // setting behind more often than not, and the ordering quietly decides between them. Until they
        // are told which one won, editing the other looks like it should change the key and does not.
        [Test]
        public void AKeySuppliedInMoreThanOnePlace_IsSaidSoNamingEveryPlaceAndTheOneInForce()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: new KeySupply(EverySettingTheOperatorSet, TheSettingInForce));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(lines, Has.Count.EqualTo(2),
                    "The instance is being run on one of several supplied keys and says nothing about it.");
                Assert.That(lines[1], Does.Contain(LabelColumn("Warning")),
                    "The notice is not marked as a warning, so it reads as an ordinary fact about a healthy " +
                    "instance rather than as something to go and tidy up.");
                Assert.That(lines[1], Does.Contain(TheSettingInForce));
                Assert.That(lines[1], Does.Contain(TheSettingBeingIgnored),
                    "The place that is being ignored is not named, so an operator has no way to know which " +
                    "of the settings they wrote is the one doing nothing.");

                // The three things the sentence has to do, each pinned by the phrase that does it: say
                // there is more than one, say which one is winning, and say what to do about the rest.
                // Without all three it is a statement of fact an operator cannot act on.
                Assert.That(lines[1], Does.Contain("more than one place"));
                Assert.That(lines[1], Does.Contain("reading nothing from the others"));
                Assert.That(lines[1], Does.Contain("Remove the ones you are not using"));
            }
        }

        [Test]
        public void AKeySuppliedInMoreThanOnePlace_IsSaidOnceHoweverManyPlacesWereNamed()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: new KeySupply([.. EverySettingTheOperatorSet, "Encryption__KeysFile"], TheSettingInForce));

            Assert.That(
                lines.Count(line => line.Contains("more than one place", StringComparison.Ordinal)),
                Is.EqualTo(1),
                "The notice is emitted per named setting rather than per start, so an operator with three " +
                "of them set reads the same sentence three times.");
        }

        [TestCase(1, TestName = "AKeySuppliedInOnePlaceOnly_IsNotWorthSayingAnythingAbout")]
        [TestCase(0, TestName = "AKeySuppliedInNoSettingAtAll_IsNotWorthSayingAnythingAbout")]
        public void AKeySuppliedNoMoreThanOnce_SaysNothingAboutWhereItCameFrom(int placesCarryingAKey)
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: new KeySupply([.. EverySettingTheOperatorSet.Take(placesCarryingAKey)], TheSettingInForce));

            Assert.That(lines, Has.Count.EqualTo(1),
                "An instance with nothing ambiguous about where its key came from is being warned anyway.");
        }

        [Test]
        public void TheNoticeAboutSeveralPlaces_NamesSettingsAndNoKeyMaterial()
        {
            var lines = StartupBanner.BuildEncryptionCustodyLines(
                RingUnder(KeyCustody.SuppliedByConfiguration),
                KeptIn(),
                keyCameFromTheRetiredSetting: false,
                allowsStartWithUnreadableSecrets: false,
                keySupply: new KeySupply(EverySettingTheOperatorSet, TheSettingInForce));

            Assert.That(
                EveryWayTheKeyCouldBeWrittenDown()
                    .Where(written => lines.Any(line => line.Contains(written, StringComparison.OrdinalIgnoreCase)))
                    .ToList(),
                Is.Empty,
                "The notice names a key rather than only the settings that carry one.");
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
