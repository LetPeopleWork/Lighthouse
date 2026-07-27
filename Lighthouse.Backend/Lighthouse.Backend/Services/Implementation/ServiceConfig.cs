using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class ServiceConfig : IServiceConfig
    {
        public ServiceConfig(IConfiguration configuration)
        {
            BaseUrl = configuration["Lighthouse:BaseUrl"] ?? string.Empty;
            OAuthStateSecret = configuration["Lighthouse:OAuth:StateSecret"] ?? string.Empty;
            TimeZone = configuration["Lighthouse:TimeZone"] ?? string.Empty;
        }

        public string BaseUrl { get; }

        public string OAuthStateSecret { get; }

        /// <summary>
        /// Bug #5567 - the instance time zone, deliberately NOT shipped in appsettings.json: a
        /// concrete default would move every containerised instance off UTC on upgrade, unannounced.
        /// Absent resolves to <see cref="TimeZoneInfo.Local"/>. Override with <c>Lighthouse__TimeZone</c>.
        /// </summary>
        public string TimeZone { get; }
    }
}
