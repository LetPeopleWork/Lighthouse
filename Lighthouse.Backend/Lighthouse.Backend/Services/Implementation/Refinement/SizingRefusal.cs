using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.Services.Implementation.Refinement
{
    /// <summary>
    /// Why a sizing entry was refused. Each reason is written to the log and, where the caller can act on it,
    /// handed back as the answer's code, so a refusal reads the same in both places.
    /// </summary>
    public static class SizingRefusal
    {
        public const string WorkItemNotInRefinement = "work-item-not-in-refinement";

        public const string VoterNameRequired = "voter-name-required";

        public const string VoterKeyRequired = "voter-key-required";

        public const string VoterNameTooLong = "voter-name-too-long";

        public const string VoteNeedsAPerson = "vote-needs-a-person";

        public const string CommentRequired = "comment-required";

        public const string CommentTooLong = "comment-too-long";

        /// <summary>
        /// Says why, for which Team and from where, and never who or on what: the declared name and the key
        /// speak for a person, and a Work Item's reference names the work.
        /// </summary>
        public static void Log(ILogger logger, LogLevel level, string reason, int teamId, SizingChannel channel)
            => logger.Log(level, "Sizing entry refused ({Reason}) for Team {TeamId} from {Channel}", reason, teamId, channel);
    }
}
