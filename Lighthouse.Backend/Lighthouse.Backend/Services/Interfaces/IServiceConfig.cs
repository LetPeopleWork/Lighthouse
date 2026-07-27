namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IServiceConfig
    {
        string BaseUrl { get; }

        string OAuthStateSecret { get; }

        string TimeZone { get; }
    }
}
