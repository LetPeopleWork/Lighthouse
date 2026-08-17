namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    // How many stored credentials can be read and are not on the key in force - which is the only honest
    // answer to "would moving them achieve anything". It cannot be worked out from the keys an instance
    // holds, or from the ids written on the stored values: a credential written before the envelope format
    // names no key at all, and those are exactly the ones an upgraded install needs to move.
    public interface IReadableSecretsNotOnTheActiveKey
    {
        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
