using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Platform.Host
{
    public interface ISystemInfoService
    {
        SystemInfo GetSystemInfo();
    }
}
