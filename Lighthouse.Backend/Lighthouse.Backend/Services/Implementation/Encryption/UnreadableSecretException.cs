using Lighthouse.Backend.Models.Encryption;
using System.Security.Cryptography;

namespace Lighthouse.Backend.Services.Implementation.Encryption
{
    // The message names the state and the key the value claims and stops there. It travels into logs and
    // into connection-validation messages an operator reads, so anything more specific would put a
    // credential or a key into places neither belongs.
    public sealed class UnreadableSecretException : CryptographicException
    {
        public UnreadableSecretException(SecretState state, string? claimedKeyId)
            : base(BuildMessage(state, claimedKeyId))
        {
            State = state;
            ClaimedKeyId = claimedKeyId;
        }

        public SecretState State { get; }

        public string? ClaimedKeyId { get; }

        private static string BuildMessage(SecretState state, string? claimedKeyId)
        {
            return string.IsNullOrEmpty(claimedKeyId)
                ? $"A stored secret in state {state} cannot be read, and it names no encryption key."
                : $"A stored secret in state {state} cannot be read; it was written under the encryption key '{claimedKeyId}'.";
        }
    }
}
