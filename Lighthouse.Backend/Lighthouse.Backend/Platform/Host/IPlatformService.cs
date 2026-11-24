namespace Lighthouse.Backend.Platform.Host
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
        
        bool IsStandalone { get; }
    }
}