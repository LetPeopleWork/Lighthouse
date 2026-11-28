using Lighthouse.Backend.Configuration;
using Lighthouse.Backend.Platform.UpdateQueue;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Platform.Host
{
    /// <summary>
    /// A compressed answer that holds a secret next to text an attacker can put there lets the attacker guess the
    /// secret from the answer's size (BREACH). So only answers to safe methods are compressed, and never the routes
    /// whose bodies can carry a secret: the update hub's handshake token, log text, and the embed session token.
    /// </summary>
    public class BreachSafeResponseCompressionProvider : ResponseCompressionProvider
    {
        public const string LogsV1Route = "/api/v1/logs";

        public const string LogsLatestRoute = "/api/latest/logs";

        public const string EmbedHandshakeV1Route = "/api/v1/embed/handshake";

        public const string EmbedHandshakeLatestRoute = "/api/latest/embed/handshake";

        private static readonly PathString[] ExcludedRoutes =
        [
            new("/" + UpdateNotificationHub.Route),
            new(LogsV1Route),
            new(LogsLatestRoute),
            new(EmbedHandshakeV1Route),
            new(EmbedHandshakeLatestRoute),
        ];

        private readonly ResponseCompressionConfiguration configuration;

        public BreachSafeResponseCompressionProvider(IServiceProvider services, IOptions<ResponseCompressionOptions> options, ResponseCompressionConfiguration configuration)
            : base(services, options)
        {
            this.configuration = configuration;
        }

        public override bool ShouldCompressResponse(HttpContext context)
        {
            return configuration.Enabled
                && IsSafeMethod(context.Request.Method)
                && !IsExcludedRoute(context.Request.Path)
                && base.ShouldCompressResponse(context);
        }

        private static bool IsSafeMethod(string method) => HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        private static bool IsExcludedRoute(PathString path) =>
            ExcludedRoutes.Any(route => path.StartsWithSegments(route, StringComparison.OrdinalIgnoreCase));
    }
}
