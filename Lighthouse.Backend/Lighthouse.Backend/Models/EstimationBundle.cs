namespace Lighthouse.Backend.Models
{
    /// <summary>
    /// How an owner's items are estimated: the Additional Field read, the unit it is shown in, whether its values are
    /// categories rather than numbers, and those categories from smallest to largest. The four parts are followed or
    /// owned together, so a field can never be read with another owner's unit or categories.
    /// </summary>
    public sealed class EstimationBundle : IEquatable<EstimationBundle>
    {
        public static readonly EstimationBundle None = new(null, null, false, []);

        public EstimationBundle(int? additionalFieldDefinitionId, string? unit, bool useNonNumericEstimation, IEnumerable<string> categoryValues)
        {
            AdditionalFieldDefinitionId = additionalFieldDefinitionId;
            Unit = unit;
            UseNonNumericEstimation = useNonNumericEstimation;
            CategoryValues = [.. categoryValues];
        }

        public int? AdditionalFieldDefinitionId { get; }

        public string? Unit { get; }

        public bool UseNonNumericEstimation { get; }

        public IReadOnlyList<string> CategoryValues { get; }

        /// <summary>A bundle without a field estimates nothing, whatever its other parts say.</summary>
        public bool IsSet => AdditionalFieldDefinitionId is not null;

        /// <summary>The same unit and categories, read from another field.</summary>
        public EstimationBundle PointingAt(int? additionalFieldDefinitionId)
            => new(additionalFieldDefinitionId, Unit, UseNonNumericEstimation, CategoryValues);

        // The category order is their ranking, so the same categories in another order are another estimation.
        public bool Equals(EstimationBundle? other)
            => other is not null
               && AdditionalFieldDefinitionId == other.AdditionalFieldDefinitionId
               && string.Equals(Unit, other.Unit, StringComparison.Ordinal)
               && UseNonNumericEstimation == other.UseNonNumericEstimation
               && CategoryValues.SequenceEqual(other.CategoryValues, StringComparer.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as EstimationBundle);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(AdditionalFieldDefinitionId);
            hash.Add(Unit, StringComparer.Ordinal);
            hash.Add(UseNonNumericEstimation);
            foreach (var category in CategoryValues)
            {
                hash.Add(category, StringComparer.Ordinal);
            }

            return hash.ToHashCode();
        }
    }

    /// <summary>Where an owner's estimation comes from, and the bundle it holds as its own.</summary>
    public readonly record struct StoredEstimation(SettingSource Source, EstimationBundle OwnBundle)
    {
        public static readonly StoredEstimation FollowingTheDefault = new(SettingSource.ConnectionDefault, EstimationBundle.None);

        /// <param name="bundle">A bundle without a field is a deliberate None, whatever the connection's default says.</param>
        public static StoredEstimation Owning(EstimationBundle bundle) => new(SettingSource.Own, bundle);

        public bool FollowsTheDefault => Source == SettingSource.ConnectionDefault;

        public EstimationBundle ReadWith(EstimationBundle connectionDefault) => EffectiveSetting.Of(Source, OwnBundle, connectionDefault);

        // A sent bundle that names a field and differs in any part from what the owner read is a choice. The
        // compiler also wants an arm for an int cast to SettingSource outside the enum; writing one would be a
        // catch-all that hides a new source, so only that is silenced.
#pragma warning disable CS8524
        public StoredEstimation Taking(SettingSource? sentSource, EstimationBundle sentBundle, EstimationBundle connectionDefault)
            => IncomingSetting.SourceChosen(sentSource, sentBundle, sentBundle.IsSet, ReadWith(connectionDefault)) switch
            {
                SettingSource.Own => Owning(sentBundle),
                SettingSource.ConnectionDefault => FollowingTheDefault,
                null => this,
            };
#pragma warning restore CS8524
    }
}
