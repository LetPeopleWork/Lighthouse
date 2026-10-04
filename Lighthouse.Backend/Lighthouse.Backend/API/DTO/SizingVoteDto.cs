using System.ComponentModel.DataAnnotations;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>A vote as the caller sends it. The answer and the channel are nullable so a missing one is refused rather than read as the first value.</summary>
    public sealed class SizingVoteDto
    {
        public SizingAnswer? Answer { get; set; }

        public SizingChannel? Channel { get; set; }

        [MaxLength(SizingLogEntry.LongestComment)]
        public string? Comment { get; set; }

        /// <summary>The name a voter declares on an instance without sign-in; ignored with sign-in.</summary>
        public string? VoterName { get; set; }
    }
}
