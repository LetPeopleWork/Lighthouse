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

        // Kept by name as well as id, because the save may rename or remove the field a default named.
        public IReadOnlyDictionary<ConnectionDefaultSetting, NamedField> DefaultsBeforeSave { get; init; } = new Dictionary<ConnectionDefaultSetting, NamedField>();

        // A field created by this same save has no id until it is stored, so the default is pointed at it afterwards.
        public Dictionary<ConnectionDefaultSetting, AdditionalFieldDefinition> DefaultsAddedInThisSave { get; } = [];

        public NamedField DefaultBeforeSave(ConnectionDefaultSetting setting) => DefaultsBeforeSave.GetValueOrDefault(setting, NamedField.Nothing);

        // Kept with their names, because once the edit removes a field the connection no longer knows what it was called.
        public IReadOnlyDictionary<int, string> StoredFieldsBeforeSave { get; init; } = new Dictionary<int, string>();

        public bool Removes(int fieldId)
            => StoredFieldsBeforeSave.ContainsKey(fieldId) && !Connection.HoldsStoredField(fieldId);

        public List<RemovedAdditionalField> RemovedFields()
            => [.. StoredFieldsBeforeSave.Where(field => Removes(field.Key)).Select(field => new RemovedAdditionalField(field.Key, field.Value))];
    }

    /// <summary>A connection default as it stands: the field it names, and for Estimation the whole bundle.</summary>
    public sealed record NamedField(int? FieldId, string? FieldName, EstimationBundle? Bundle = null)
    {
        public static readonly NamedField Nothing = new(null, null);

        // A rename keeps the field, so it is not a change of the default; for Estimation the unit, the
        // non-numeric switch and the categories are part of the default too.
        public bool IsTheSameDefaultAs(NamedField other)
            => FieldId == other.FieldId && Equals(Bundle, other.Bundle);

        /// <summary>How the log names the default, or null when it names no field.</summary>
        public string? Description => Bundle is null || FieldName is null ? FieldName : DescribeTheEstimation(FieldName, Bundle);

        private static string DescribeTheEstimation(string fieldName, EstimationBundle bundle)
            => bundle.UseNonNumericEstimation
                ? $"{bundle.NamedWith(fieldName)}, categories [{string.Join(", ", bundle.CategoryValues)}]"
                : bundle.NamedWith(fieldName);
    }

    public sealed record ConnectionSaveOutcome(IReadOnlyList<AdditionalFieldInUse> FieldsInUse)
    {
        public bool Refused => FieldsInUse.Count > 0;
    }
}
