using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ICryptoService
    {
        string Encrypt(string plainText);

        string Decrypt(string cipherText);

        SecretReadResult Read(string storedValue);
    }
}
