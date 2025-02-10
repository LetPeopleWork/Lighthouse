using Lighthouse.Backend.Services.Interfaces;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Services.Implementation
{
    public class CryptoService : ICryptoService
    {
        private readonly byte[] encryptionKey;

        public CryptoService(IConfiguration configuration)
        {
            var base64Key = configuration["EncryptionSettings:EncryptionKey"] ?? throw new InvalidOperationException("EncryptionKey is not configured.");

            encryptionKey = Convert.FromBase64String(base64Key);

            if (encryptionKey.Length != 32)
            {
                throw new InvalidOperationException("Encryption key length is invalid. It must be 32 bytes for AES-256.");
            }
        }

        public string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.GenerateIV();

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    ms.Write(aes.IV, 0, aes.IV.Length);

                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                    }

                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            try
            {
                var fullCipher = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = encryptionKey;

                    var iv = new byte[16];
                    Array.Copy(fullCipher, 0, iv, 0, iv.Length);
                    aes.IV = iv;

                    byte[] cipher = new byte[fullCipher.Length - iv.Length];
                    Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream(cipher))
                    {
                        using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader sr = new StreamReader(cs))
                            {
                                return sr.ReadToEnd();
                            }
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is CryptographicException || ex is FormatException)
            {
                // Most likely the cipher was not (yet) encrypted. Returning plain text value for backward compatibility.
                return cipherText;
            }
        }
    }
}
