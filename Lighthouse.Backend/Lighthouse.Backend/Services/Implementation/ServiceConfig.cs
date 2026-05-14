using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ServiceConfig : IServiceConfig
    {
        public ServiceConfig(IConfiguration configuration)
        {
            BaseUrl = configuration["Lighthouse:BaseUrl"] ?? string.Empty;
            OAuthStateSecret = configuration["Lighthouse:OAuth:StateSecret"] ?? string.Empty;
        }

        public string BaseUrl { get; }

        public string OAuthStateSecret { get; }
    }
}
