using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateCompletionNotifier
    {
        bool IsDistributed { get; }

        Task PublishCompletionAsync(UpdateKey key);

        IDisposable Subscribe(Action<UpdateKey> onCompleted);
    }
}
