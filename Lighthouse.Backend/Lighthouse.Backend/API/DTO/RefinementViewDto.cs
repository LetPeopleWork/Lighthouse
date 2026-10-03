using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class RefinementViewDto(RefinementView view, ILighthouseClock clock)
    {
        public bool RefinementConfigured { get; } = view.RefinementConfigured;

        public List<RefinementRowDto> WorkItems { get; } = [.. view.WorkItems.Select(item => new RefinementRowDto(item, clock))];
    }

    public sealed class RefinementRowDto(WorkItem workItem, ILighthouseClock clock)
    {
        public string ReferenceId { get; } = workItem.ReferenceId;

        public string Name { get; } = workItem.Name;

        public string? Url { get; } = workItem.Url;

        public string State { get; } = workItem.State;

        public StateCategories StateCategory { get; } = workItem.StateCategory;

        // Age is only defined for started work, so a To Do row says nothing rather than a zero that reads as new.
        public int? WorkItemAge { get; } = workItem.StateCategory == StateCategories.Doing
            ? workItem.WorkItemAge(clock.Zone, clock.Today)
            : null;
    }
}
