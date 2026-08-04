using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    // wwwroot is a gitignored frontend build output, so the SPA default page is absent in a test host
    // and its middleware throws instead of answering. Any request that reaches it was unrouted; report
    // that as 404 so an assertion fails on the status rather than on a missing build artefact.
    public sealed class UnservedSpaPageStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                app.Use(async (context, proceed) =>
                {
                    try
                    {
                        await proceed();
                    }
                    catch (InvalidOperationException exception)
                        when (exception.Message.Contains("SPA default page", StringComparison.Ordinal))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                    }
                });

                next(app);
            };
        }
    }
}
