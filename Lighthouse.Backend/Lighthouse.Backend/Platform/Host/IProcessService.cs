using System.Diagnostics;

namespace Lighthouse.Backend.Platform.Host
{
    public interface IProcessService
    {
        void Start(ProcessStartInfo startInfo);
 
        void Exit(int exitCode);
    }
}