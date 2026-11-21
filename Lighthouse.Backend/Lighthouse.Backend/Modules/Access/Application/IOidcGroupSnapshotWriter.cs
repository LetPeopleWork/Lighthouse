namespace Lighthouse.Backend.Modules.Access.Application
{
    public interface IOidcGroupSnapshotWriter
    {
        Task WriteAsync(string stableSubject, IReadOnlyList<string> groupValues, CancellationToken cancellationToken);
    }
}
