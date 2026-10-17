using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IConnectionSaveService
    {
        Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken);

        Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken);
    }

    // The caller edits Connection between Open and Commit; FetchShapeBeforeSave is how it fetched when it was opened.
    public sealed record ConnectionSaveHandle(WorkTrackingSystemConnection Connection, string FetchShapeBeforeSave)
    {
        public int ConnectionId => Connection.Id;

        // Kept by name as well as id, because the save may rename or remove the field it named.
        public NamedField TeamsParentOverrideDefaultBeforeSave { get; init; } = NamedField.Nothing;
    }

    public sealed record NamedField(int? FieldId, string? FieldName)
    {
        public static readonly NamedField Nothing = new(null, null);
    }

    public sealed record ConnectionSaveOutcome(bool Refused);
}
