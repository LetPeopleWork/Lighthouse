using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateStatusStore
    {
        bool TryAdmit(UpdateKey key, UpdateStatus status);

        UpdateStatus? Advance(UpdateKey key, UpdateProgress to);

        bool TryGet(UpdateKey key, out UpdateStatus? status);

        void Remove(UpdateKey key);

        bool HasActiveWork();
    }
}
