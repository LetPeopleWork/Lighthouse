namespace Lighthouse.Backend.Services.Interfaces.Encryption
{
    // How many stored secrets an instance still keeps under the key that ships with every copy of
    // Lighthouse. An operator who upgrades and reads no release note learns it from this number, so it is
    // answered on the settings page rather than behind an action somebody has to know to press.
    public interface IPublishedKeySecretCount
    {
        Task<int> CountAsync(CancellationToken cancellationToken = default);
    }
}
