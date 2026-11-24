using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Platform.UpdateQueue
{
    public interface IUpdateCompletionNotifier
    {
        bool IsDistributed { get; }

        Task PublishCompletionAsync(UpdateKey key);

        IDisposable Subscribe(Action<UpdateKey> onCompleted);
    }
}
