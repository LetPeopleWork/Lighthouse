using Lighthouse.Backend.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;

namespace Lighthouse.Backend.Startup
{
    public static class TelemetryConfigurator
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration)
        {
            if (!IsEnabled(configuration))
            {
                return;
            }

            services.AddOpenTelemetry()
                .WithMetrics(metrics => metrics
                    .AddAspNetCoreInstrumentation()
                    .AddPrometheusExporter());
        }

        public static void MapEndpoints(WebApplication app)
        {
            if (!IsEnabled(app.Configuration))
            {
                return;
            }

            app.MapPrometheusScrapingEndpoint().AllowAnonymous();
        }

        private static bool IsEnabled(IConfiguration configuration)
        {
            var telemetryConfig = configuration
                .GetSection(TelemetryConfiguration.SectionName)
                .Get<TelemetryConfiguration>() ?? new TelemetryConfiguration();

            return telemetryConfig.Enabled;
        }
    }
}
