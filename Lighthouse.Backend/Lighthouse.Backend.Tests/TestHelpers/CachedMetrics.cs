using Lighthouse.Backend.Cache;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    public static class CachedMetrics
    {
        /// <summary>
        /// Empties the host's metrics cache. A host that outlives its database must do this whenever it moves
        /// to a fresh one: every fresh database numbers its Teams and Portfolios from one again, so a figure
        /// cached for the previous database's Team would otherwise be served to the new Team with the same id.
        /// </summary>
        public static void ForgetTheCachedMetrics(this WebApplicationFactory<Program> host)
        {
            var metrics = host.Services.GetRequiredService<Cache<string, object>>();
            foreach (var key in metrics.Keys.ToList())
            {
                metrics.Remove(key);
            }
        }
    }
}
