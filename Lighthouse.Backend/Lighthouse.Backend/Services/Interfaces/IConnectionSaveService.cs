namespace Lighthouse.Backend.Services.Interfaces
{
    // SCAFFOLD: not implemented or registered yet. The acceptance tests for connection defaults decorate this
    // service to let another save land between opening and committing a connection save, which is how they
    // prove that a field a Team started using in that gap still blocks its removal. DELIVER implements it,
    // registers it, and may rename it as long as the tests follow.
    public interface IConnectionSaveService
    {
        Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken);

        Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken);
    }

    public sealed record ConnectionSaveHandle(int ConnectionId);

    public sealed record ConnectionSaveOutcome(bool Refused);
}
