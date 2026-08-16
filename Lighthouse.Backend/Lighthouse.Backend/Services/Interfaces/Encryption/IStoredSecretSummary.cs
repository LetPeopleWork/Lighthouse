namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    // What the encryption settings need to know about the stored secrets without decrypting any of them:
    // how many are still readable with the key published with the product, and which keys the rest say
    // wrote them.
    public sealed record StoredSecretSummary(int UnderThePublishedKey, IReadOnlyCollection<string> KeyIdsSeen);

    // Asked as one question because it is one screen. The two answers read the same three columns for
    // the same payload, and separate ports would be two chances for a caller to ask for one, forget the
    // other, and draw a panel that is half right.
    public interface IStoredSecretSummary
    {
        Task<StoredSecretSummary> ReadAsync(CancellationToken cancellationToken = default);
    }
}
