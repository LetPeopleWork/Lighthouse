using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class RefinementViewDto(RefinementView view)
    {
        public bool RefinementConfigured { get; } = view.RefinementConfigured;

        public List<RefinementRowDto> WorkItems { get; } = [.. view.WorkItems.Select(row => new RefinementRowDto(row))];

        public YardstickDto Yardstick { get; } = new(view.Yardstick);

        public VoterIdentityKind VoterIdentity { get; } = view.VoterIdentity;
    }

    public sealed class YardstickDto(Yardstick yardstick)
    {
        public YardstickSource Source { get; } = yardstick.Source;

        public int? Days { get; } = yardstick.Days;

        public int? Probability { get; } = yardstick.Probability;
    }

    public sealed class RefinementRowDto(RefinementRow row)
    {
        public string ReferenceId { get; } = row.WorkItem.ReferenceId;

        public string Name { get; } = row.WorkItem.Name;

        public string? Url { get; } = row.WorkItem.Url;

        public string State { get; } = row.WorkItem.State;

        public string ParentReferenceId { get; } = row.WorkItem.ParentReferenceId;

        public int VoteCount { get; } = row.Votes.VoteCount;

        public SizingAnswer? MyVote { get; } = row.Votes.MyVote;
    }
}
