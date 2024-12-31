using Lighthouse.Backend.Services.Interfaces.Update;

namespace Lighthouse.Backend.Services.Implementation.Update
{
    public abstract class UpdateServiceBase : IUpdateService
    {
        private readonly IUpdateQueueService updateQueueService;
        private readonly UpdateType updateType;

        protected UpdateServiceBase(IUpdateQueueService updateQueueService, UpdateType updateType)
        {
            this.updateQueueService = updateQueueService;
            this.updateType = updateType;
        }

        public void TriggerUpdate(int id)
        {
            updateQueueService.EnqueueUpdate(updateType, id, async serviceProvider =>
            {
                await Update(id, serviceProvider);
            });
        }

        protected abstract Task Update(int id, IServiceProvider serviceProvider);
    }
}
