using System.Globalization;
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

        public int ReadyByVotesCount { get; } = view.ReadyByVotesCount;

        public bool StagesConfigured { get; } = view.StagesConfigured;

        public int ReadyCount { get; } = view.ReadyCount;

        public ReadySource ReadySource { get; } = view.ReadySource;

        public string? NextRefinementDate { get; } = view.CalendarFacts.NextRefinementDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        public bool IsRefinementDay { get; } = view.CalendarFacts.IsRefinementDay;

        public int? DaysUntilNextRefinement { get; } = view.CalendarFacts.DaysUntilNextRefinement;

        public RefinementNeedDto Need { get; } = new(view.Need);
    }

    /// <summary>The need as facts only; the browser and the clients put them into words and place the line.</summary>
    public sealed class RefinementNeedDto(RefinementNeed need)
    {
        public RefinementVerdict? Verdict { get; } = need.Verdict;

        public NeedUnavailableReason? UnavailableReason { get; } = need.UnavailableReason;

        public int? Low { get; } = need.Range?.Low;

        public int? High { get; } = need.Range?.High;

        public int? LowPercentile { get; } = need.Range?.LowPercentile;

        public int? HighPercentile { get; } = need.Range?.HighPercentile;

        public int? HorizonWorkingDays { get; } = need.Range?.HorizonWorkingDays;
    }

    public sealed class YardstickDto(Yardstick yardstick)
    {
        public YardstickSource Source { get; } = yardstick.Source;

        public int? Days { get; } = yardstick.Days;

        public int? Probability { get; } = yardstick.Probability;
    }

    public class RefinementRowDto(RefinementRow row)
    {
        public string ReferenceId { get; } = row.WorkItem.ReferenceId;

        public string Name { get; } = row.WorkItem.Name;

        public string? Url { get; } = row.WorkItem.Url;

        public string State { get; } = row.WorkItem.State;

        public string ParentReferenceId { get; } = row.WorkItem.ParentReferenceId;

        public int VoteCount { get; } = row.Votes.VoteCount;

        public SizingAnswer? MyVote { get; } = row.Votes.MyVote;

        public VoteSplitDto Split { get; } = new(row.Votes.Split);

        public RowReadiness Readiness { get; } = row.Standing.Readiness;

        public int? MissingVotes { get; } = row.Standing.MissingVotes;

        public RefinementStage? Stage { get; } = row.Stage;

        public bool SignalsDisagree { get; } = row.SignalsDisagree;

        public bool HasComments { get; } = row.Conversation.HasComments;

        public bool HasOpenQuestion { get; } = row.Conversation.HasOpenQuestion;
    }

    /// <summary>The row as a vote left it, and whether that vote is the one that made it Ready.</summary>
    public sealed class VotedRowDto(RefinementRow row, bool madeReady) : RefinementRowDto(row)
    {
        public bool MadeReady { get; } = madeReady;
    }

    public sealed class VoteSplitDto(VoteSplit split)
    {
        public int Yes { get; } = split.Yes;

        public int YesBut { get; } = split.YesBut;

        public int No { get; } = split.No;
    }
}
