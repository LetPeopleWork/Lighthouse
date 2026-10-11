using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Metrics;
using Lighthouse.Backend.Services.Implementation;

namespace Lighthouse.Backend.API.DTO
{
    public record WorkItemEstimateDto(double? Value, string? DisplayValue, string? Unit)
    {
        public static WorkItemEstimateDto? For(WorkTrackingSystemOptionsOwner owner, WorkItemBase item)
        {
            var estimate = EstimateNormalizer.EstimateOf(owner, item);
            if (estimate is null)
            {
                return null;
            }

            // Only a value the Estimation chart can place is shown, so a row never claims more than the chart does.
            return estimate.Status == EstimateNormalizationStatus.Mapped
                ? new WorkItemEstimateDto(estimate.NumericValue, estimate.DisplayValue, owner.EstimationUnit)
                : new WorkItemEstimateDto(null, null, owner.EstimationUnit);
        }
    }
}
