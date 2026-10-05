using System.Globalization;
using Lighthouse.Backend.Models.Refinement;
using Lighthouse.Backend.Services.Interfaces.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    public sealed class SizingLogDto(SizingLog log)
    {
        public List<SizingLogEntryDto> Entries { get; } = [.. log.Entries.Select(line => new SizingLogEntryDto(line))];

        public SizingVotersDto Voters { get; } = new(log.Voters);
    }

    public sealed class SizingVotersDto(VoterNames voters)
    {
        public List<string> Yes { get; } = voters.Yes;

        public List<string> YesBut { get; } = voters.YesBut;

        public List<string> No { get; } = voters.No;
    }

    public sealed class SizingLogEntryDto(SizingLogLine line)
    {
        public SizingEntryKind Kind { get; } = line.Kind;

        public SizingAnswer? Answer { get; } = line.Answer;

        public string? Comment { get; } = line.Comment;

        public string VoterName { get; } = line.VoterName;

        public SizingChannel Channel { get; } = line.Channel;

        public string RecordedAt { get; } = line.RecordedAt.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

        public bool IsMine { get; } = line.IsMine;

        public bool IsOpenQuestion { get; } = line.IsOpenQuestion;
    }
}
