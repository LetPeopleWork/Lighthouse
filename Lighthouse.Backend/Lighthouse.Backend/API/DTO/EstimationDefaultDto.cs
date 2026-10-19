using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>How the owners following a connection's Estimation default estimate their items, as one bundle.</summary>
    public class EstimationDefaultDto
    {
        public int? AdditionalFieldDefinitionId { get; set; }

        public string? Unit { get; set; }

        public bool? UseNonNumericEstimation { get; set; }

        public List<string>? EstimationCategoryValues { get; set; }

        public static EstimationDefaultDto? From(EstimationBundle bundle) => bundle.IsSet
            ? new EstimationDefaultDto
            {
                AdditionalFieldDefinitionId = bundle.AdditionalFieldDefinitionId,
                Unit = bundle.Unit,
                UseNonNumericEstimation = bundle.UseNonNumericEstimation,
                EstimationCategoryValues = [.. bundle.CategoryValues],
            }
            : null;

        // A bundle without a field estimates nothing, so its other parts are not kept either.
        public EstimationBundle ToBundle() => AdditionalFieldDefinitionId is null
            ? EstimationBundle.None
            : new EstimationBundle(AdditionalFieldDefinitionId, Unit, UseNonNumericEstimation ?? false, EstimationCategoryValues ?? []);
    }
}
