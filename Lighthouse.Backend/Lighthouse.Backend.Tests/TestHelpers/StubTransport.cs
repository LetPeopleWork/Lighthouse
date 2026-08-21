using Moq;
using Moq.Protected;
using System.Net;
using System.Text;

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
        private readonly List<string> urls = [];
        private readonly List<string> bodies = [];

        public IReadOnlyList<string> Urls => urls;

        public IReadOnlyList<string> Bodies => bodies;

        public IReadOnlyList<string> Paths => urls.ConvertAll(url => new Uri(url).AbsolutePath);

        /// <summary>The last request whose path names a search - the one carrying the field list.</summary>
        public string LastSearchUrl
        {
            get
            {
                var searches = urls.FindAll(url => url.Contains("/search", StringComparison.Ordinal));

                return searches[searches.Count - 1];
            }
        }

        public string FieldsAskedForInTheLastSearch()
            => System.Web.HttpUtility.ParseQueryString(new Uri(LastSearchUrl).Query)["fields"] ?? string.Empty;

        public void Record(HttpRequestMessage request, string body)
        {
            if (request.RequestUri is not null)
            {
                urls.Add(request.RequestUri.AbsoluteUri);
            }

            bodies.Add(body);
        }
    }
}
