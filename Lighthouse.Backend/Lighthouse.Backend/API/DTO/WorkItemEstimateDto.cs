using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public record WorkItemEstimateDto(double? Value, string? DisplayValue, string? Unit)
    {
        public static WorkItemEstimateDto? For(WorkTrackingSystemOptionsOwner owner, WorkItemBase item)
        {
            throw new InvalidOperationException(
                $"Not yet implemented -- RED scaffold written by DISTILL: the estimate of {item.ReferenceId} for {owner.Name}");
        }
    }
}
