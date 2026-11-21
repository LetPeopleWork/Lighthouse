using Lighthouse.Backend.Models;
using Mono.Cecil;

namespace Lighthouse.Backend.Tests.Architecture
{
    /// <summary>
    /// Licensing has its own module folder. The licence service, the licence verifier and their interfaces live in the
    /// module's Application, the licence guard attribute lives in its Http adapters, and none is left under the old
    /// Services folders. The licence public key travels with its verifier: exactly one embedded resource ends with
    /// public_key.pem, and it is named after the Application namespace. Every type name is read from the shared membership
    /// list and looked up in the compiled assembly, so a name that matches no type fails and names itself.
    /// </summary>
    [TestFixture]
    public class LicensingLayoutArchUnitTest
    {
        private const string ApplicationNamespace = "Lighthouse.Backend.Modules.Licensing.Application";

        private const string AdaptersHttpNamespace = "Lighthouse.Backend.Modules.Licensing.Adapters.Http";

        private const string WhereLicensingLivedBefore = @"^Lighthouse\.Backend\.Services\.(Implementation|Interfaces)\.Licensing(\.|$)";

        private const string TheKeyFileName = "public_key.pem";

        private static readonly Lazy<ModuleDefinition> ProductionModule = new(() => IlDependencies.ReadModule(typeof(Feature).Assembly));

        [OneTimeTearDown]
        public void DisposeModule()
        {
            if (ProductionModule.IsValueCreated)
            {
                ProductionModule.Value.Dispose();
            }
        }

        [Test]
        public void TheLicenseServicesAndTheirInterfaces_LiveInTheLicensingApplication_AndNoneIsLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, ApplicationNamespace, AccessAndLicensingMembership.LicensingApplication, WhereLicensingLivedBefore).Check();
        }

        [Test]
        public void TheLicenseGuard_LivesInTheLicensingHttpAdapters_AndIsNotLeftUnderServices()
        {
            ModuleRules.TypesLiveInTheirLayer(ProductionModule.Value, AdaptersHttpNamespace, AccessAndLicensingMembership.LicensingAdaptersHttp, WhereLicensingLivedBefore).Check();
        }

        [Test]
        public void TheLicencePublicKey_IsEmbeddedOnce_BesideItsVerifier()
        {
            var expected = $"{ApplicationNamespace}.{TheKeyFileName}";

            var keys = ProductionModule.Value.Resources
                .Select(resource => resource.Name)
                .Where(name => name.EndsWith(TheKeyFileName, StringComparison.Ordinal))
                .ToList();

            Assert.That(
                keys,
                Is.EqualTo(new[] { expected }),
                $"The assembly embeds [{string.Join(", ", keys)}] as the licence public key but should embed exactly [{expected}]. " +
                "Why it matters: the verifier reads its key by resource name, so a key left in the old folder or embedded twice either breaks signature " +
                "checks or keeps the module's key outside the module. How to fix: git mv the pem file next to the verifier and name that path in the two " +
                "public_key.pem lines of Lighthouse.Backend.csproj.");
        }
    }
}
