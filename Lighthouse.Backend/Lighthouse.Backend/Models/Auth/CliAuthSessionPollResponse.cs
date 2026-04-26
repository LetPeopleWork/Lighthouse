namespace Lighthouse.Backend.Models.Auth
{
    public record CliAuthSessionPollResponse
    {
        public required string Status { get; init; }

        public string? Token { get; init; }

        public string? UserName { get; init; }
    }
}
