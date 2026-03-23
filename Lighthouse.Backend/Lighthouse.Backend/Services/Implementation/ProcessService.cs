using System.Diagnostics;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ProcessService : IProcessService
    {
        public void Start(ProcessStartInfo startInfo)
        {
            Process.Start(startInfo);
        }
 
        public void Exit(int exitCode)
        {
            Environment.Exit(exitCode);
        }
    }
}