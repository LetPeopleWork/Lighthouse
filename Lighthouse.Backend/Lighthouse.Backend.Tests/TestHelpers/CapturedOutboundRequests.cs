using System.Net;
using Microsoft.Extensions.Http;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Every outbound HTTP request the running application makes through a client it asked the
    /// framework for - address and body both - recorded and then answered here rather than sent.
    ///
    /// This exists so that "nothing was sent" can be asserted directly instead of inferred. A test
    /// that only checks a fake publisher was not called proves nothing about a second, unnoticed
    /// path to the same address; this sees every request that leaves through the client factory,
    /// whatever asked for it.
    ///
    /// It is only as complete as the rule that nothing constructs its own client by hand. That rule
    /// is what keeps this from quietly becoming a test of nothing, and it is asserted separately.
    /// </summary>
    public sealed class CapturedOutboundRequests
    {
        private readonly List<OutboundRequest> requests = [];
        private readonly Lock gate = new();

        /// <summary>
        /// Positive control. An assertion that nothing reached a given address is worthless unless
        /// this harness can be shown to see anything at all.
        /// </summary>
        public bool SawAnything
        {
            get
            {
                lock (gate)
                {
                    return requests.Count > 0;
                }
            }
        }

        public List<OutboundRequest> All
        {
            get
            {
                lock (gate)
                {
                    return [.. requests];
                }
            }
        }

        public void Clear()
        {
            lock (gate)
            {
                requests.Clear();
            }
        }

        public List<OutboundRequest> ThatReached(string host)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(host);

            return [.. All.Where(request => request.Address.Host.EndsWith(host, StringComparison.OrdinalIgnoreCase))];
        }

        /// <summary>
        /// Everything sent to a given address, as one piece of text. What a payload rule forbids is
        /// forbidden across the whole conversation rather than one message at a time.
        /// </summary>
        public string EverythingSentTo(string host)
        {
            return string.Join(Environment.NewLine, ThatReached(host).Select(request => request.Body));
        }

        internal void Record(Uri? address, string body)
        {
            if (address is null)
            {
                return;
            }

            lock (gate)
            {
                requests.Add(new OutboundRequest(address, body));
            }
        }
    }

    public sealed record OutboundRequest(Uri Address, string Body);

    /// <summary>
    /// Puts the recorder in front of every client the framework builds, including ones registered
    /// long after this is installed.
    /// </summary>
    public sealed class OutboundRequestRecordingFilter(CapturedOutboundRequests captured) : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            ArgumentNullException.ThrowIfNull(next);

            return builder =>
            {
                next(builder);

                ArgumentNullException.ThrowIfNull(builder);
                builder.AdditionalHandlers.Insert(0, new OutboundRequestRecordingHandler(captured));
            };
        }
    }

    internal sealed class OutboundRequestRecordingHandler(CapturedOutboundRequests captured) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            captured.Record(request.RequestUri, body);

            // Answered here rather than forwarded. A test that reached a real address would be slow,
            // flaky and - for the address this was written for - would put invented events into a
            // live dataset.
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}"),
                RequestMessage = request,
            };
        }
    }
}
