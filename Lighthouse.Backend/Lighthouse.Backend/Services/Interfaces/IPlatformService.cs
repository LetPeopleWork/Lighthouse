namespace Lighthouse.Backend.Services.Interfaces
{
    public enum SupportedPlatform
    {
        Windows,
        Linux,
        MacOS,
        Docker,
    }
    
    public interface IPlatformService
    {
        SupportedPlatform Platform { get; }
        
        bool IsDevEnvironment { get; }
    }
}