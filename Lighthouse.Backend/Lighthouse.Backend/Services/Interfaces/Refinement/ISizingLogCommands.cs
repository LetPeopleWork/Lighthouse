using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Interfaces.Refinement
{
    /// <summary>Everything a reader adds to a Team's sizing log. Each call appends; nothing is ever changed.</summary>
    public interface ISizingLogCommands
    {
        SizingOutcome Vote(int teamId, string workItemReference, SizingVote vote, Voter voter);

        /// <summary>A comment never changes anybody's vote, so it can never make a Work Item Ready.</summary>
        SizingOutcome Comment(int teamId, string workItemReference, SizingComment comment, Voter voter);

        /// <summary>
        /// Takes back the vote the voter with this key currently holds, under the name that vote was cast with.
        /// Without a current vote there is nothing to take back, and nothing is written. Given an answer, only a
        /// current vote giving that answer is taken back: one session of an account may have changed the vote
        /// since another session showed it.
        /// </summary>
        SizingOutcome TakeBack(int teamId, string workItemReference, SizingChannel channel, string voterKey, SizingAnswer? answer);
    }

    public sealed record SizingVote(SizingAnswer Answer, SizingChannel Channel, string? Comment);

    public sealed record SizingComment(string Comment, SizingChannel Channel);

    /// <summary>The voter as the server established them; <paramref name="Key"/> is what makes a vote theirs.</summary>
    public sealed record Voter(string Key, string DisplayName, int? ProfileId);

    public enum SizingOutcome
    {
        Recorded,
        TeamNotFound,
        WorkItemNotInRefinement,
        RecordedAndMadeReady,
        CommentMissing,
        CommentTooLong,
        NothingTakenBack,
    }
}
