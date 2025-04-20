using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateQueueService
    {
        void EnqueueUpdate(UpdateType updateType, int id, Func<IServiceProvider, Task> updateTask);
    }
}
