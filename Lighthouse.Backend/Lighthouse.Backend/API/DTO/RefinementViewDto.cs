using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class RefinementViewDto(RefinementView view)
    {
        public bool RefinementConfigured { get; } = view.RefinementConfigured;

        public List<RefinementRowDto> WorkItems { get; } = [.. view.WorkItems.Select(item => new RefinementRowDto(item))];
    }

    public sealed class RefinementRowDto(WorkItem workItem)
    {
        public string ReferenceId { get; } = workItem.ReferenceId;

        public string Name { get; } = workItem.Name;

        public string? Url { get; } = workItem.Url;

        public string State { get; } = workItem.State;

        public string ParentReferenceId { get; } = workItem.ParentReferenceId;
    }
}
