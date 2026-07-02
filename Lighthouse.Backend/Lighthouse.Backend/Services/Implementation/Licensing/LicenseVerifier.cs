using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Lighthouse.Backend.Services.Implementation.Licensing
{
    public class LicenseVerifier : ILicenseVerifier
    {
        private static readonly Lazy<string> publicKey = new(LoadEmbeddedPublicKey);

        public bool VerifyLicense(LicenseInformation license)
        {
            if (string.IsNullOrEmpty(license.Signature))
            {
                return false;
            }

            try
            {
                string canonicalLicense = CanonicalizeJson(license);

                byte[] licenseBytes = Encoding.UTF8.GetBytes(canonicalLicense);
                byte[] signatureBytes = Convert.FromBase64String(license.Signature);

                using RSA rsa = RSA.Create();
                rsa.ImportFromPem(publicKey.Value);

                bool valid = rsa.VerifyData(
                    licenseBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                return valid;
            }
            catch (FormatException)
            {
                // Signaure is not in a valid Base64 format --> tampering? Anyway, we cannot verify it --> invalid license
                return false;
            }
        }

        internal static string CanonicalizeJson(LicenseInformation license)
        {
            var dict = new SortedDictionary<string, object>
            {
                ["email"] = license.Email,
                ["expiry"] = license.ExpiryDate.ToString("yyyy-MM-dd"),
                ["license_number"] = license.LicenseNumber,
                ["name"] = license.Name,
                ["organization"] = license.Organization
            };

            if (license.ValidFrom.HasValue)
            {
                dict["valid_from"] = license.ValidFrom.Value.ToString("yyyy-MM-dd");
            }

            using var stream = new MemoryStream();

            // Emit raw UTF-8 with only the JSON-mandatory escapes so the canonical bytes are
            // byte-identical to the signer (Python json.dumps with ensure_ascii=False). The
            // default encoder escapes non-ASCII (e.g. "e-acute") and HTML-sensitive ASCII
            // ("&"), diverging from the signed bytes and rejecting valid licenses (Bug 5382).
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
            {
                Indented = false,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            });

            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);

                switch (kvp.Value)
                {
                    case string stringValue:
                        // Normalize to NFC so decomposed and precomposed spellings of the same
                        // character (e.g. "e" + combining acute vs "e-acute") produce identical bytes.
                        writer.WriteStringValue(stringValue.Normalize(NormalizationForm.FormC));
                        break;
                    case DateTime dateValue:
                        writer.WriteStringValue(dateValue.ToString("yyyy-MM-dd"));
                        break;
                    default:
                        writer.WriteStringValue(kvp.Value?.ToString() ?? "");
                        break;
                }
            }
            writer.WriteEndObject();
            writer.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private static string LoadEmbeddedPublicKey()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames()
                .First(x => x.EndsWith("public_key.pem"));

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
