using Lighthouse.Backend.Modules.Licensing.Application;
using Lighthouse.Backend.Modules.Licensing.Contracts;

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
