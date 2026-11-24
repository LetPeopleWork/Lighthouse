using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Platform.UpdateQueue
{
    public interface IUpdateTaskNaming
    {
        string NameOf(UpdateStatus work);
    }
}
