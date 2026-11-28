using Lighthouse.Backend.Configuration;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Platform.Host
{
    /// <summary>
    /// A compressed answer that holds a secret next to text an attacker can put there lets the attacker guess the
    /// secret from the answer's size (BREACH). So only answers to safe methods are compressed, and never an answer of
    /// an endpoint marked with <see cref="NeverCompressResponseAttribute"/>, whose bodies can carry a secret or log text.
    /// A static file answers before routing and has no endpoint, so it stays compressible.
    /// </summary>
    public class BreachSafeResponseCompressionProvider : ResponseCompressionProvider
    {
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
                && !IsMarkedNeverCompress(context)
                && base.ShouldCompressResponse(context);
        }

        private static bool IsSafeMethod(string method) => HttpMethods.IsGet(method) || HttpMethods.IsHead(method);

        private static bool IsMarkedNeverCompress(HttpContext context) =>
            context.GetEndpoint()?.Metadata.GetMetadata<NeverCompressResponseAttribute>() is not null;
    }
}
