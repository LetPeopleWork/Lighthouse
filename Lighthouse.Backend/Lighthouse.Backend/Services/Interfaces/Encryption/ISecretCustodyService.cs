using Lighthouse.Backend.Models.Encryption;

namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    // Handed to whatever only wants to know what is readable. There is no method on it that writes, so a
    // check cannot move a secret even by mistake - the guarantee is the shape of what the caller was given
    // rather than something a reviewer has to keep noticing.
    public interface ISecretCustodyReader
    {
        Task<SecretReadabilityReport> InspectAsync(CancellationToken cancellationToken = default);
    }

    public interface ISecretCustodyService : ISecretCustodyReader
    {
        Task<SecretReadabilityReport> ReEncryptAsync(CancellationToken cancellationToken = default);
    }
}
