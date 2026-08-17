using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface ICryptoService
    {
        string Encrypt(string plainText);

        // A pass moving every stored secret onto one key has to write every row under the key it started
        // with, and asking for "the active one" per row means a key replaced halfway through splits the
        // pass across two keys without it noticing.
        string Encrypt(string plainText, EncryptionKey key);

        string Decrypt(string cipherText);

        SecretReadResult Read(string storedValue);
    }
}
