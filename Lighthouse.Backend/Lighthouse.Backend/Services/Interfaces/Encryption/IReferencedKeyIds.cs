namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    // Which keys the stored secrets say they were written under. Asked so the panel can list the keys
    // that matter rather than every key held: every ring carries the key published with the product, so
    // a first install would otherwise appear to hold a key named after a legacy it never had, and a
    // rotated one fills with names that never encrypted anything.
    public interface IReferencedKeyIds
    {
        Task<IReadOnlyCollection<string>> ReadAsync(CancellationToken cancellationToken = default);
    }
}
