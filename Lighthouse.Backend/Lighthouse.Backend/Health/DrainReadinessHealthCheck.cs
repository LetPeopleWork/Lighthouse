using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Lighthouse.Backend.Health
{
    public sealed class DrainReadinessHealthCheck : IHealthCheck
    {
        private readonly IReadinessState readinessState;

        public DrainReadinessHealthCheck(IReadinessState readinessState)
        {
            this.readinessState = readinessState ?? throw new ArgumentNullException(nameof(readinessState));
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(readinessState.IsDraining
                ? HealthCheckResult.Unhealthy("Shutting down — draining connections.")
                : HealthCheckResult.Healthy("Accepting traffic."));
        }
    }
}
