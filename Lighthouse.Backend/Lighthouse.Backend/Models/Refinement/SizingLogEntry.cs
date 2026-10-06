using System.Security.Cryptography;
using System.Text;

namespace Lighthouse.Backend.Models.Refinement
{
    /// <summary>
    /// One entry of a Team's sizing log. Entries are only ever added: a changed mind is a later entry, and
    /// what a Work Item's votes add up to is worked out from the whole log when it is read.
    /// </summary>
    public class SizingLogEntry
    {
        public const int LongestComment = 2000;

        private const string SelfDeclaredKeyPrefix = "self:";

        private const string AccountKeyPrefix = "account:";

        /// <summary>
        /// How the log keys a voter who signed in nowhere: only a hash of the key their browser keeps, so the
        /// log never holds anything that could speak for them.
        /// </summary>
        public static string SelfDeclaredVoterKeyOf(string browserKey)
            => SelfDeclaredKeyPrefix + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(browserKey)));

        /// <summary>How the log keys a voter who signed in: the subject their account is known by.</summary>
        public static string AccountVoterKeyOf(string subject) => AccountKeyPrefix + subject;

        public int Id { get; init; }

        public required int TeamId { get; init; }

        /// <summary>The tracker's reference, not the Work Item row: a refresh may delete and re-create the row.</summary>
        public required string WorkItemReferenceId { get; init; }

        public required SizingEntryKind Kind { get; init; }

        public SizingAnswer? Answer { get; init; }

        public string? Comment { get; init; }

        /// <summary>Who cast it, derived by the server - never a value the caller fills in.</summary>
        public required string VoterKey { get; init; }

        public int? VoterProfileId { get; init; }

        /// <summary>The voter's name as it stood when the entry was written, so the log never re-labels itself.</summary>
        public required string VoterDisplayName { get; init; }

        public required DateTime RecordedAt { get; init; }

        public required SizingChannel Channel { get; init; }

        public int? YardstickDays { get; init; }

        public required YardstickSource YardstickSource { get; init; }

        public int? YardstickProbability { get; init; }
    }
}
