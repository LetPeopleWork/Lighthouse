namespace Lighthouse.Middleware
{
    public class FeatureFlagMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IConfiguration configuration;

        public FeatureFlagMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            var useNewFrontend = configuration.GetValue<bool>("FeatureFlags:UseNewFrontend");

            if (!context.Request.Path.StartsWithSegments("/api") && useNewFrontend)
            {
                if (context.Request.Path == "/")
                {
                    context.Request.Path = "/NewFrontend/index.html";
                }
                else
                {
                    context.Request.Path = "/NewFrontend" + context.Request.Path;
                }
            }

            await next(context);
        }
    }

    public static class FeatureFlagMiddlewareExtensions
    {
        public static IApplicationBuilder UseFeatureFlagMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FeatureFlagMiddleware>();
        }
    }

}
