namespace Lighthouse.Backend.Platform.Host
{
    public interface IServiceConfig
    {
        string BaseUrl { get; }

        string OAuthStateSecret { get; }

        string TimeZone { get; }
    }
}
