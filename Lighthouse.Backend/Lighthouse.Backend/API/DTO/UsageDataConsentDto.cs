namespace Lighthouse.Backend.API.DTO
{
    public sealed record UsageDataConsentRequestDto(string Decision);

    /// <summary>
    /// The token is returned once, by the call that mints it, and never again - only its digest is
    /// stored, so the server cannot re-issue it. A browser that loses it cannot withdraw its
    /// consent, which is why the browser keeps it rather than the server remembering who asked.
    /// </summary>
    public sealed record UsageDataConsentResponseDto(string Token);
}
