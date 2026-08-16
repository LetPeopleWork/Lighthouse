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
        bool AllowsStartWithUnreadableSecrets = false);

    // Said on every start for as long as the old name keeps working, because the only thing that makes it
    // safe to eventually stop reading that name is everyone having moved off it first.
    public static class RetiredKeySettingName
    {
        public const string Nudge =
            "The encryption key is being read from EncryptionSettings__EncryptionKey, which this release " +
            "retired. It still works today and will stop being read in a future release. Set the same value " +
            "as Encryption__Key and remove the old one.";
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
            "Remove the setting once they have been.";
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
                facts.AllowsStartWithUnreadableSecrets));

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
            bool allowsStartWithUnreadableSecrets = false)
        {
            ArgumentNullException.ThrowIfNull(keyRing);
            ArgumentNullException.ThrowIfNull(keyStore);

            var lines = new List<string>
            {
                Line("🔑", "Encryption", $"{WhereTheKeyCameFrom(keyRing.Custody)} · {keyStore.Directory}")
            };

            if (keyRing.Custody == KeyCustody.NoDurableStore)
            {
                lines.Add(Line("⚠️", "Warning", NoDurableKeyStore.Warning));
            }

            if (keyCameFromTheRetiredSetting)
            {
                lines.Add(Line("⚠️", "Warning", RetiredKeySettingName.Nudge));
            }

            if (allowsStartWithUnreadableSecrets)
            {
                lines.Add(Line("🚨", "Encryption", RunningWithCredentialsItCannotRead.Notice));
            }

            return lines;
        }

        // One word each, and they still have to tell the four apart on their own - an operator scanning a
        // console is reading this at a glance or not at all. A custody nobody named claims the least,
        // which is the same thing having nowhere to keep a key means: the instance is on the key that
        // ships inside every copy of the product.
        private static string WhereTheKeyCameFrom(KeyCustody custody)
        {
            return custody switch
            {
                KeyCustody.GeneratedForThisInstance => "instance",
                KeyCustody.SuppliedByConfiguration => "configured",
                KeyCustody.SuppliedByExternalSecret => "mounted secret",
                _ => "published key",
            };
        }

        private static string Line(string emoji, string label, string value)
        {
            return $"{emoji}  {label,-13} : {value}";
        }
    }
}
