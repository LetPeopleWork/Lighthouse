using Lighthouse.Backend.Services.Implementation.BackgroundServices.Update;

namespace Lighthouse.Backend.Services.Interfaces.Update
{
    public interface IUpdateTaskNaming
    {
        string NameOf(UpdateStatus work);
    }
}
