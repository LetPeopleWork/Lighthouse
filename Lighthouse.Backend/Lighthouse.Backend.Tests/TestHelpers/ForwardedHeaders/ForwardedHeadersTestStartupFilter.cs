using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

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

                    var options = context.RequestServices.GetRequiredService<IOptions<ForwardedHeadersOptions>>();
                    var forwardedHeaders = new ForwardedHeadersMiddleware(
                        _ => Task.CompletedTask, NullLoggerFactory.Instance, options);
                    await forwardedHeaders.Invoke(context);

                    await context.Response.WriteAsJsonAsync(new ObservedRequest(
                        context.Request.Scheme,
                        context.Request.Host.Value ?? string.Empty));
                });

                next(app);
            };
        }
    }
}
