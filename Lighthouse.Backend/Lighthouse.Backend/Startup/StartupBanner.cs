using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Implementation.Encryption;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Lighthouse.Backend.Startup
{
    // Everything the banner says about an instance, gathered by the caller and handed over in one piece, so
    // the lines can be built - and asserted on - without a running application behind them.
    public sealed record StartupBannerFacts(
        string Version,
        IReadOnlyList<string> Urls,
        string DatabaseProvider,
        string? LogFilePath,
        IConfiguration Configuration,
        EncryptionKeyRing KeyRing,
        KeyStoreLocation KeyStore,
        bool KeyCameFromTheRetiredSetting,
        bool AllowsStartWithUnreadableSecrets = false,
        KeySupply? KeySupply = null);

    // Said on every start for as long as the old name keeps working, because the only thing that makes it
    // safe to eventually stop reading that name is everyone having moved off it first.
    public static class RetiredKeySettingName
    {
        public const string Nudge =
            "The encryption key is being read from EncryptionSettings__EncryptionKey, which this release " +
            "retired. It still works today and will stop being read in a future release. Set the same value " +
            "as Encryption__Key and remove the old one.";
    }

    // Said once, however many places were named. An operator moving their key from a setting into a file
    // their secret store owns leaves the old setting behind more often than not, and every one of those
    // places is honoured on its own. Only the ordering decides between them, and until they are told which
    // one won, editing any of the others looks like it should change the key and does not.
    public static class AKeySuppliedInMoreThanOnePlace
    {
        public static string? Notice(KeySupply? keySupply)
        {
            if (keySupply is not { InMoreThanOnePlace: true })
            {
                return null;
            }

            return $"An encryption key was supplied in more than one place: {string.Join(", ", keySupply.Settings)}. " +
                $"Lighthouse is using the one from {keySupply.TheOneInForce} and reading nothing from the others. " +
                "Remove the ones you are not using, so that changing them cannot look like changing the key.";
        }
    }

    // Said on every start for as long as the setting is in force, and shaped like the emergency
    // administrator line for the same reason: whoever opens a hatch is rarely the person who pays for it
    // still being open a year later.
    public static class RunningWithCredentialsItCannotRead
    {
        public static string Notice =>
            "This instance was started with " +
            EncryptionKeyRingBootstrapper.StartAnywaySettingKey.Replace(":", "__", StringComparison.Ordinal) +
            " set, so it is running with stored credentials it cannot read. Every one of them has to be " +
            "entered again; the encryption settings name the Connection and the field each one sits in. " +
            "Remove the setting once they have been - and if this instance has nowhere durable to keep a " +
            "key, give it one with Encryption__KeyStorePath or Encryption__Key before entering anything, " +
            "because removing the setting means restarting and a restart would take the new key with it.";
    }

    // One sentence, rendered in two places. The startup line is read from a console by whoever runs the
    // process; the system information page is read by an administrator months later, and is the only one
    // a standalone operator ever sees. Two copies that agree today are two copies that disagree later.
    public static class WhoseKeyThisIs
    {
        // One word each, and they still have to tell the four apart on their own - an operator scanning a
        // console is reading this at a glance or not at all. A custody nobody named claims the least,
        // which is the same thing having nowhere to keep a key means: the instance is on the key that
        // ships inside every copy of the product.
        public static string InAWord(KeyCustody custody)
        {
            return custody switch
            {
                KeyCustody.GeneratedForThisInstance => "instance",
                KeyCustody.SuppliedByConfiguration => "configured",
                KeyCustody.SuppliedByExternalSecret => "mounted secret",
                _ => "published key",
            };
        }

        // Whose key it is, then where the key store is. Never the key id: it is worth having at the
        // moment a start stops, and the refusal that stops one names it there.
        public static string AndWhereItIsKept(KeyCustody custody, string keyStoreDirectory)
        {
            return $"{InAWord(custody)} · {keyStoreDirectory}";
        }
    }

    public static class StartupBanner
    {
        private const string Rule = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

        public static IReadOnlyList<string> BuildInfoLines(StartupBannerFacts facts)
        {
            ArgumentNullException.ThrowIfNull(facts);

            // Stryker disable all: blank rows and rule lines are where the banner breathes, not what it
            // says. Pinning them would freeze the layout against the next person who wants to move a gap.
            var info = new List<string>
            {
                "",
                "",
                "",
                Rule,
                $"        Lighthouse {facts.Version}",
                Rule,
                ""
            };
            // Stryker restore all

            info.AddRange(facts.Urls.Select(url => Line("🌐", "Url", url)));

            // Stryker disable once all: a gap between the addresses and the machine they answer on.
            info.Add("");

            info.Add(Line("🖥️", "OS", RuntimeInformation.OSDescription.Trim()));
            info.Add(Line("⚙️", "Runtime", RuntimeInformation.FrameworkDescription));
            info.Add(Line("🧩", "Architecture", RuntimeInformation.OSArchitecture.ToString()));
            info.Add(Line("🔢", "Process ID", Environment.ProcessId.ToString(CultureInfo.InvariantCulture)));
            info.Add(Line("💾", "Database", facts.DatabaseProvider));

            if (!string.IsNullOrEmpty(facts.LogFilePath))
            {
                info.Add(Line("📝", "Logs", facts.LogFilePath));
            }

            info.AddRange(AuthPostureBanner.BuildAuthPostureLines(facts.Configuration));
            info.AddRange(BuildEncryptionCustodyLines(
                facts.KeyRing,
                facts.KeyStore,
                facts.KeyCameFromTheRetiredSetting,
                facts.AllowsStartWithUnreadableSecrets,
                facts.KeySupply));

            // Stryker disable once all: the gap that separates the banner from whatever logs next.
            info.Add("");

            return info;
        }

        // What an operator has to know to keep their secrets readable: whose key this is, and the
        // directory that has to survive for it to still be there tomorrow. Every other line of this
        // banner is a label and a short value, and this one was a clause.
        //
        // The key id came off it deliberately. It is the most useful thing to have when diagnosing a
        // start that stopped - which is why the refusal that stops one now names both the key the
        // instance came up on and the key the stored credentials were written under. Carrying it on every
        // healthy start as well only lengthened the line nobody was reading.
        //
        // The key itself never appears; it is read off the ring rather than out of configuration, so the
        // sentence cannot disagree with the key in force.
        public static IReadOnlyList<string> BuildEncryptionCustodyLines(
            EncryptionKeyRing keyRing,
            KeyStoreLocation keyStore,
            bool keyCameFromTheRetiredSetting,
            bool allowsStartWithUnreadableSecrets = false,
            KeySupply? keySupply = null)
        {
            ArgumentNullException.ThrowIfNull(keyRing);
            ArgumentNullException.ThrowIfNull(keyStore);

            var lines = new List<string>
            {
                Line("🔑", "Encryption", WhoseKeyThisIs.AndWhereItIsKept(keyRing.Custody, keyStore.Directory))
            };

            if (keyRing.Custody == KeyCustody.NoDurableStore)
            {
                lines.Add(Line("⚠️", "Warning", NoDurableKeyStore.Warning));
            }

            if (keyCameFromTheRetiredSetting)
            {
                lines.Add(Line("⚠️", "Warning", RetiredKeySettingName.Nudge));
            }

            if (AKeySuppliedInMoreThanOnePlace.Notice(keySupply) is { } suppliedTwice)
            {
                lines.Add(Line("⚠️", "Warning", suppliedTwice));
            }

            if (allowsStartWithUnreadableSecrets)
            {
                lines.Add(Line("🚨", "Encryption", RunningWithCredentialsItCannotRead.Notice));
            }

            return lines;
        }

        private static string Line(string emoji, string label, string value)
        {
            return $"{emoji}  {label,-13} : {value}";
        }
    }
}
