using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// An HttpMessageHandler that answers from a function instead of from a network. Substituting the
    /// transport and nothing else is what keeps the queries, the field lists, the paging and the mapping
    /// on the production path, so a test can say what was asked for as well as what came back.
    /// </summary>
    public static class StubTransport
    {
        public static HttpMessageHandler RespondingWith(Func<HttpRequestMessage, string> bodyFor)
            => RespondingWith((request, _) => bodyFor(request));

        /// <param name="bodyFor">
        /// Answers a request, given the request and its body. The body is handed over because a GraphQL
        /// endpoint puts the whole query there, so it is the only way to tell one call from another.
        /// </param>
        public static HttpMessageHandler RespondingWith(Func<HttpRequestMessage, string, string> bodyFor)
        {
            var mock = new Mock<HttpMessageHandler>();

            mock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>(async (request, cancellationToken) =>
                {
                    var requestBody = request.Content is null
                        ? string.Empty
                        : await request.Content.ReadAsStringAsync(cancellationToken);

                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(bodyFor(request, requestBody), Encoding.UTF8, "application/json"),
                    };
                });

            return mock.Object;
        }
    }

    /// <summary>
    /// Every request a stubbed transport was given, in order. Two runs of the same fetch have to agree
    /// on this exactly, which is what makes "and it costs no extra request" a testable claim rather than
    /// a reading of the code.
    /// </summary>
    public sealed class RecordedRequests
    {
        private readonly List<RecordedRequest> requests = [];

        public IReadOnlyList<string> Urls => [.. requests.Where(request => request.Url is not null).Select(request => request.Url!)];

        public IReadOnlyList<string> Bodies => [.. requests.Select(request => request.Body)];

        public IReadOnlyList<string> Paths => [.. Urls.Select(url => new Uri(url).AbsolutePath)];

        /// <summary>The field list the last search asked for, comma-joined, read out of that search's body.</summary>
        public string FieldsAskedForInTheLastSearch()
        {
            var lastSearch = requests.FindLast(request => request.Url?.Contains("/search", StringComparison.Ordinal) == true);

            return lastSearch is null ? string.Empty : SearchBody.ValueIn(lastSearch.Body, "fields");
        }

        public void Record(HttpRequestMessage request, string body)
            => requests.Add(new RecordedRequest(request.RequestUri?.AbsoluteUri, body));

        private sealed record RecordedRequest(string? Url, string Body);
    }

    /// <summary>
    /// Reads one parameter out of the json body a Jira search was sent with. A list reads back comma-joined
    /// and a number as its digits, and a body without the parameter reads back empty.
    /// </summary>
    public static class SearchBody
    {
        public static string ValueIn(string body, string name)
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return string.Empty;
            }

            using var json = JsonDocument.Parse(body);

            if (json.RootElement.ValueKind != JsonValueKind.Object || !json.RootElement.TryGetProperty(name, out var value))
            {
                return string.Empty;
            }

            return value.ValueKind switch
            {
                JsonValueKind.Array => string.Join(",", value.EnumerateArray().Select(entry => entry.ToString())),
                JsonValueKind.String => value.GetString() ?? string.Empty,
                _ => value.GetRawText(),
            };
        }
    }
}
