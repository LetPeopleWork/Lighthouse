using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IConnectionSaveService
    {
        Task<ConnectionSaveHandle?> Open(int connectionId, CancellationToken cancellationToken);

        Task<ConnectionSaveOutcome> Commit(ConnectionSaveHandle handle, Guid? concurrencyToken, bool startUpdates, CancellationToken cancellationToken);
    }

    // The caller edits Connection between Open and Commit.
    public sealed record ConnectionSaveHandle(WorkTrackingSystemConnection Connection)
    {
        public int ConnectionId => Connection.Id;

        // What each Team and each Portfolio on the connection asked the tracker for when the save was opened, by id.
        public IReadOnlyDictionary<int, string> TeamFetchesBeforeSave { get; init; } = new Dictionary<int, string>();

        public IReadOnlyDictionary<int, string> PortfolioFetchesBeforeSave { get; init; } = new Dictionary<int, string>();

        // Kept by name as well as id, because the save may rename or remove the field it named.
        public NamedField TeamsParentOverrideDefaultBeforeSave { get; init; } = NamedField.Nothing;

        public NamedField PortfoliosParentOverrideDefaultBeforeSave { get; init; } = NamedField.Nothing;

        // A field created by this same save has no id until it is stored, so the default is pointed at it afterwards.
        public AdditionalFieldDefinition? TeamsParentOverrideDefaultAddedInThisSave { get; set; }

        public AdditionalFieldDefinition? PortfoliosParentOverrideDefaultAddedInThisSave { get; set; }

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
