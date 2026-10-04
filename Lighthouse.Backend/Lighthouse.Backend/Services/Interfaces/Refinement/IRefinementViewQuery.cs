using Lighthouse.Backend.Models;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    public interface IRefinementViewQuery
    {
        /// <param name="presentedVoterKey">The key the reader's browser keeps, used only to mark the reader's own votes.</param>
        /// <returns>Null when there is no such Team.</returns>
        RefinementView? ForTeam(int teamId, string? presentedVoterKey);
    }

    public sealed record RefinementView(bool RefinementConfigured, List<RefinementRow> WorkItems, Yardstick Yardstick, VoterIdentityKind VoterIdentity);

    public sealed record RefinementRow(WorkItem WorkItem, RowVotes Votes);

    /// <summary>How many people currently have a vote on the row, and the reader's own vote, if any.</summary>
    public sealed record RowVotes(int VoteCount, SizingAnswer? MyVote)
    {
        public static RowVotes None { get; } = new(0, null);
    }

    /// <summary>The number every voter answers against: so many days, with so much probability.</summary>
    public sealed record Yardstick(YardstickSource Source, int? Days, int? Probability)
    {
        public static Yardstick None { get; } = new(YardstickSource.Unavailable, null, null);
    }

    /// <summary>How a voter is known on this instance: by their account, or by a name they declare.</summary>
    public enum VoterIdentityKind
    {
        Account = 0,
        SelfDeclared = 1,
    }
}
