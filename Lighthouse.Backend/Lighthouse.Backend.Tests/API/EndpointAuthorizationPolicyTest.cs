using Lighthouse.Backend.API;
using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Lighthouse.Backend.Tests.API
{
    [TestFixture]
    public class EndpointAuthorizationPolicyTest
    {
        private static readonly HashSet<Type> AllowedAnonymousControllers =
        [
            typeof(AuthController),
            typeof(VersionController),
        ];

        [Test]
        public void AllowAnonymousControllers_HaveAttribute()
        {
            foreach (var controllerType in AllowedAnonymousControllers)
            {
                var attribute = controllerType.GetCustomAttribute<AllowAnonymousAttribute>();
                Assert.That(attribute, Is.Not.Null,
                    $"{controllerType.Name} must have [AllowAnonymous] to remain accessible without authentication.");
            }
        }

        [Test]
        public void ProtectedControllers_DoNotHaveAllowAnonymous()
        {
            var assembly = typeof(AuthController).Assembly;
            var allControllers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
                .ToList();

            var protectedControllers = allControllers.Except(AllowedAnonymousControllers).ToList();

            Assert.That(protectedControllers, Is.Not.Empty,
                "Expected to find controllers that are not in the anonymous allowlist.");

            foreach (var controllerType in protectedControllers)
            {
                var attribute = controllerType.GetCustomAttribute<AllowAnonymousAttribute>();
                Assert.That(attribute, Is.Null,
                    $"{controllerType.Name} has [AllowAnonymous] but is not in the anonymous allowlist. " +
                    "If this controller should be publicly accessible, add it to AllowedAnonymousControllers in this test.");
            }
        }

        [Test]
        public void AllowList_ContainsOnlyExistingControllers()
        {
            var assembly = typeof(AuthController).Assembly;
            var allControllers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Controller"))
                .ToHashSet();

            foreach (var allowedType in AllowedAnonymousControllers)
            {
                Assert.That(allControllers, Does.Contain(allowedType),
                    $"{allowedType.Name} is in the allowlist but does not exist in the assembly.");
            }
        }
    }
}
