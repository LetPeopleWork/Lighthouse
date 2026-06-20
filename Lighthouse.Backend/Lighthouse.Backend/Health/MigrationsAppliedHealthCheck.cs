using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lighthouse.Backend.Health
{
    public sealed class MigrationsAppliedHealthCheck : IHealthCheck
    {
        private readonly LighthouseAppContext dbContext;

        public MigrationsAppliedHealthCheck(LighthouseAppContext dbContext)
        {
            this.dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var pending = (await dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                return pending.Count == 0
                    ? HealthCheckResult.Healthy("All migrations applied.")
                    : HealthCheckResult.Unhealthy($"{pending.Count} migration(s) pending.");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return HealthCheckResult.Unhealthy("Unable to determine migration state.", ex);
            }
        }
    }
}
