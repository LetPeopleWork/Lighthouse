namespace Lighthouse.Backend.Services.Interfaces.Auth
{
    public interface IOidcGroupSnapshotWriter
    {
        Task WriteAsync(string stableSubject, IReadOnlyList<string> groupValues, CancellationToken cancellationToken);
    }
}
