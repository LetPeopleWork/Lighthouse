using System.Text.Json;
using System.Text.Json.Serialization;
using Lighthouse.Backend.Models.Refinement;

namespace Lighthouse.Backend.API.DTO
{
    /// <summary>A vote as the caller sends it. The answer and the channel are nullable so a missing one is refused rather than read as the first value.</summary>
    public sealed class SizingVoteDto
    {
        // The host reads enums leniently, so "0" would otherwise pass as Yes and "Yes, No" as No; a vote takes
        // only the names.
        [JsonConverter(typeof(NamesOnly<SizingAnswer>))]
        public SizingAnswer? Answer { get; set; }

        [JsonConverter(typeof(NamesOnly<SizingChannel>))]
        public SizingChannel? Channel { get; set; }

        public string? Comment { get; set; }

        /// <summary>The name a voter declares on an instance without sign-in; ignored with sign-in.</summary>
        public string? VoterName { get; set; }

        internal sealed class NamesOnly<TEnum> : JsonConverter<TEnum>
            where TEnum : struct, Enum
        {
            public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var name = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                if (Enum.TryParse(name, out TEnum value) && string.Equals(value.ToString(), name, StringComparison.Ordinal))
                {
                    return value;
                }

                throw new JsonException($"Expected one of {string.Join(", ", Enum.GetNames<TEnum>())}.");
            }

            public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
                => writer.WriteStringValue(value.ToString());
        }
    }
}
