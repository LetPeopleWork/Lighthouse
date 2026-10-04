using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    /// <summary>Everything a reader adds to a Team's sizing log. Each call appends; nothing is ever changed.</summary>
    public interface ISizingLogCommands
    {
        VoteOutcome Vote(int teamId, string workItemReference, SizingVote vote, Voter voter);
    }

    public sealed record SizingVote(SizingAnswer Answer, SizingChannel Channel, string? Comment);

    /// <summary>The voter as the server established them; <paramref name="Key"/> is what makes a vote theirs.</summary>
    public sealed record Voter(string Key, string DisplayName, int? ProfileId);

    public enum VoteOutcome
    {
        Recorded,
        TeamNotFound,
        WorkItemNotInRefinement,
        RecordedAndMadeReady,
    }
}
