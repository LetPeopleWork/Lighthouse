using Lighthouse.Backend.Services.Interfaces.Auth;

namespace Lighthouse.Backend.Services.Implementation.Auth
{
    // SCAFFOLD: true
    public sealed class OidcGroupSnapshotWriter : IOidcGroupSnapshotWriter
    {
        public Task WriteAsync(string stableSubject, IReadOnlyList<string> groupValues, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Not yet implemented — RED scaffold");
        }
    }
}
