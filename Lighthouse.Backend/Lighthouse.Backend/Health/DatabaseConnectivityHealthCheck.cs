using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lighthouse.Backend.Health
{
    public sealed class DatabaseConnectivityHealthCheck : IHealthCheck
    {
        private readonly LighthouseAppContext dbContext;

        public DatabaseConnectivityHealthCheck(LighthouseAppContext dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
                return canConnect
                    ? HealthCheckResult.Healthy("Database reachable.")
                    : HealthCheckResult.Unhealthy("Database unreachable.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy("Database unreachable.", ex);
            }
        }
    }
}
