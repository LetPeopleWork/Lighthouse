using System.Diagnostics;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IProcessService
    {
        void Start(ProcessStartInfo startInfo);
 
        void Exit(int exitCode);
    }
}