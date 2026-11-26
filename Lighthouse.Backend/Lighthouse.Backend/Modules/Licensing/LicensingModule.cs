using Lighthouse.Backend.Modules.Licensing.Application;

namespace Lighthouse.Backend.Modules.Licensing
{
    public static class LicensingModule
    {
        public static IServiceCollection AddLicensing(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<ILicenseService, LicenseService>();
            serviceCollection.AddScoped<ILicenseVerifier, LicenseVerifier>();

            return serviceCollection;
        }
    }
}
