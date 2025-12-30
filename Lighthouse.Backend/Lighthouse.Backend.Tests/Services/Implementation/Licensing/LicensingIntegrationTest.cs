using Lighthouse.Backend.Services.Interfaces.Licensing;
using Lighthouse.Backend.Tests.TestHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.Services.Implementation.Licensing
{
    public class LicensingIntegrationTest() : IntegrationTestBase(new TestWebApplicationFactory<Program>())
    {
        [Test]
        public async Task ValidLicenseLoaded_RemoveLicense_LoadNewLicense_IsValid()
        {
            var licenseService = ServiceProvider.GetService<ILicenseService>();

            // Load valid but expired license
            await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

            var (_, expiredLicenseIsValid) =  licenseService.GetLicenseData();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(expiredLicenseIsValid, Is.True);
                Assert.That(licenseService.CanUsePremiumFeatures(), Is.False);
            }

            // Remove License
            await licenseService.ClearLicense();

            var (noLicenseInfo, noLicenseInfoIsValid) =  licenseService.GetLicenseData();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(noLicenseInfoIsValid, Is.False);
                Assert.That(noLicenseInfo, Is.Null);
                Assert.That(licenseService.CanUsePremiumFeatures(), Is.False);
            }

            // Load valid not expired license
            await licenseService.ImportLicense(TestLicenseData.ValidLicense);
            
            var (_, validLicense) =  licenseService.GetLicenseData();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(validLicense, Is.True);
                Assert.That(licenseService.CanUsePremiumFeatures(), Is.True);
            }
        }
        [Test]
        public async Task ValidLicenseLoaded_LoadNewLicense_IsValid()
        {
            var licenseService = ServiceProvider.GetService<ILicenseService>();

            // Load valid but expired license
            await licenseService.ImportLicense(TestLicenseData.ValidExpiredLicense);

            var (_, expiredLicenseIsValid) =  licenseService.GetLicenseData();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(expiredLicenseIsValid, Is.True);
                Assert.That(licenseService.CanUsePremiumFeatures(), Is.False);
            }

            // Load valid not expired license
            await licenseService.ImportLicense(TestLicenseData.ValidLicense);
            
            var (_, validLicense) =  licenseService.GetLicenseData();
            using (Assert.EnterMultipleScope())
            {
                Assert.That(validLicense, Is.True);
                Assert.That(licenseService.CanUsePremiumFeatures(), Is.True);
            }
        }
    }
}