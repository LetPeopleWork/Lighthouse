using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Licensing;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
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

        private static string CanonicalizeJson(LicenseInformation license)
        {
            var dict = new SortedDictionary<string, object>
            {
                ["email"] = license.Email,
                ["expiry"] = license.ExpiryDate.ToString("yyyy-MM-dd"),
                ["name"] = license.Name,
                ["organization"] = license.Organization
            };

            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

            writer.WriteStartObject();
            foreach (var kvp in dict)
            {
                writer.WritePropertyName(kvp.Key);

                switch (kvp.Value)
                {
                    case string stringValue:
                        writer.WriteStringValue(stringValue);
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
