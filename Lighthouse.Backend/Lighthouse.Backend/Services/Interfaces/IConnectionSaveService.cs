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

        // A field created by this same save has no id until it is stored, so the default is pointed at it afterwards.
        public AdditionalFieldDefinition? TeamsParentOverrideDefaultAddedInThisSave { get; set; }

        // Kept with their names, because once the edit removes a field the connection no longer knows what it was called.
        public IReadOnlyDictionary<int, string> StoredFieldsBeforeSave { get; init; } = new Dictionary<int, string>();

        public bool Removes(int fieldId)
            => StoredFieldsBeforeSave.ContainsKey(fieldId) && !Connection.HoldsStoredField(fieldId);

        public List<RemovedAdditionalField> RemovedFields()
            => [.. StoredFieldsBeforeSave.Where(field => Removes(field.Key)).Select(field => new RemovedAdditionalField(field.Key, field.Value))];
    }

    public sealed record NamedField(int? FieldId, string? FieldName)
    {
        public static readonly NamedField Nothing = new(null, null);
    }

    public sealed record ConnectionSaveOutcome(IReadOnlyList<AdditionalFieldInUse> FieldsInUse)
    {
        public bool Refused => FieldsInUse.Count > 0;
    }
}
