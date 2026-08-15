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
        KeyStoreLocation KeyStore);

    public static class StartupBanner
    {
        private const string Rule = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";

        public static IReadOnlyList<string> BuildInfoLines(StartupBannerFacts facts)
        {
            ArgumentNullException.ThrowIfNull(facts);

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

            info.AddRange(facts.Urls.Select(url => Line("🌐", "Url", url)));

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
            info.AddRange(BuildEncryptionCustodyLines(facts.KeyRing, facts.KeyStore));

            info.Add("");

            return info;
        }

        // What an operator has to know to keep their secrets readable: whose key this is, which name it
        // answers to, and the directory that has to survive for it to still be there tomorrow - which is
        // also the directory they would have to back up. The key itself never appears; it is read off the
        // ring rather than out of configuration, so the sentence cannot disagree with the key in force.
        public static IReadOnlyList<string> BuildEncryptionCustodyLines(EncryptionKeyRing keyRing, KeyStoreLocation keyStore)
        {
            ArgumentNullException.ThrowIfNull(keyRing);
            ArgumentNullException.ThrowIfNull(keyStore);

            var lines = new List<string>
            {
                Line("🔑", "Encryption", $"{WhereTheKeyCameFrom(keyRing.Custody)} ({keyRing.ActiveKey.Id}) · {keyStore.Directory}")
            };

            if (keyRing.Custody == KeyCustody.NoDurableStore)
            {
                lines.Add(Line("⚠️", "Warning", NoDurableKeyStore.Warning));
            }

            return lines;
        }

        // A custody nobody named claims the least, which is the same thing having nowhere to keep a key
        // means: the instance is on the key that ships inside every copy of the product.
        private static string WhereTheKeyCameFrom(KeyCustody custody)
        {
            return custody switch
            {
                KeyCustody.GeneratedForThisInstance => "generated for this instance",
                KeyCustody.SuppliedByConfiguration => "supplied by configuration",
                KeyCustody.SuppliedByExternalSecret => "supplied by a mounted secret file",
                _ => "the key published with the product",
            };
        }

        private static string Line(string emoji, string label, string value)
        {
            return $"{emoji}  {label,-13} : {value}";
        }
    }
}
