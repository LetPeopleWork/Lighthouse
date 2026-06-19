using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders
{
    public sealed class ForwardedHeadersTestStartupFilter : IStartupFilter
    {
        public const string EchoPath = "/__test/echo-scheme";

        private readonly IPAddress? simulatedRemoteIp;

        public ForwardedHeadersTestStartupFilter(IPAddress? simulatedRemoteIp)
        {
            this.simulatedRemoteIp = simulatedRemoteIp;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, proceed) =>
                {
                    if (simulatedRemoteIp is not null)
                    {
                        context.Connection.RemoteIpAddress = simulatedRemoteIp;
                    }

                    if (context.Request.Path != EchoPath)
                    {
                        await proceed();
                        return;
                    }

                    var originalBody = context.Response.Body;
                    using var discardedDownstreamBody = new MemoryStream();
                    context.Response.Body = discardedDownstreamBody;

                    await proceed();

                    context.Response.Body = originalBody;
                    context.Response.Clear();
                    await context.Response.WriteAsJsonAsync(new ObservedRequest(
                        context.Request.Scheme,
                        context.Request.Host.Value ?? string.Empty));
                });

                next(app);
            };
        }
    }
}
