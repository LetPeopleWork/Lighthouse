using Lighthouse.Backend.Models.Encryption;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    internal class FakeCryptoService : ICryptoService
    {
        public string Decrypt(string cipherText)
        {
            return cipherText;
        }

        public string Encrypt(string plainText)
        {
            return plainText;
        }

        public SecretReadResult Read(string storedValue)
        {
            return new SecretReadResult(SecretState.LegacyPlaintext, storedValue, null);
        }
    }
}
