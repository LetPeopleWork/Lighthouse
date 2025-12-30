using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class PlatformService : IPlatformService
    {
        public PlatformService(IHostEnvironment hostEnvironment)
        {
            IsDevEnvironment = hostEnvironment.IsDevelopment();
            SetPlatform();
        }
        
        public SupportedPlatform Platform { get; private set; }
        public bool IsDevEnvironment { get; }

        private void SetPlatform()
        {
            if (IsDocker())
            {
                Platform = SupportedPlatform.Docker;
            }
            else if (OperatingSystem.IsWindows())
            {
                Platform = SupportedPlatform.Windows;
            }
            else if (OperatingSystem.IsLinux())
            {
                Platform = SupportedPlatform.Linux;
            }
            else if (OperatingSystem.IsMacOS())
            {
                Platform = SupportedPlatform.MacOS;
            }
            else
            {
                throw new PlatformNotSupportedException("The current operating system is not supported.");
            }
        }
        
        private static bool IsDocker()
        {
            // Check for common Docker environment indicators
            return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
                   File.Exists("/.dockerenv") ||
                   Environment.GetEnvironmentVariable("LIGHTHOUSE_DOCKER") == "true";
        }
    }
}