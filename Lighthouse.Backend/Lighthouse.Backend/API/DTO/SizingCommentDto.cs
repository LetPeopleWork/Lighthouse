using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>A comment without a vote, as the caller sends it.</summary>
    public sealed class SizingCommentDto
    {
        public string? Comment { get; set; }

        [JsonConverter(typeof(SizingVoteDto.NamesOnly<SizingChannel>))]
        public SizingChannel? Channel { get; set; }

        /// <summary>The name a voter declares on an instance without sign-in; ignored with sign-in.</summary>
        public string? VoterName { get; set; }
    }
}
