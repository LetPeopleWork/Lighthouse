using System.Net;

namespace Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders
{
    public sealed record OidcChallengeResponse(
        HttpStatusCode StatusCode,
        string Location,
        IReadOnlyList<string> SetCookies);
}
