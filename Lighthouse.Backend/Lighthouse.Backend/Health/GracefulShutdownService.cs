using Lighthouse.Backend.Services.Interfaces.Update;
using Microsoft.Extensions.Hosting;

namespace Lighthouse.Backend.Health
{
    public sealed class GracefulShutdownService : IHostedService
    {
        private readonly IUpdateQueueService updateQueueService;

        public GracefulShutdownService(IUpdateQueueService updateQueueService)
        {
            this.updateQueueService = updateQueueService ?? throw new ArgumentNullException(nameof(updateQueueService));
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => updateQueueService.DrainAsync(cancellationToken);
    }
}
