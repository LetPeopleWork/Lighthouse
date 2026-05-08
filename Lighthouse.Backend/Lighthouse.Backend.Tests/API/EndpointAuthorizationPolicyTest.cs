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
        public void ExpectedAnonymousEndpoints_HaveAllowAnonymousAttribute()
        {
            var expectations = new Dictionary<Type, string[]?>
    {
        // Entire controller must be anonymous
        { typeof(VersionController), null },

        // Only these actions must be anonymous
        {
            typeof(AuthController),
            new[]
            {
                nameof(AuthController.GetRuntimeAuthStatus),
                nameof(AuthController.Login),
                nameof(AuthController.Logout),
                nameof(AuthController.GetSession),
            }
        }
    };

            foreach (var (controllerType, anonymousActions) in expectations)
            {
                // Entire controller anonymous
                if (anonymousActions is null)
                {
                    var controllerAttribute =
                        controllerType.GetCustomAttribute<AllowAnonymousAttribute>();

                    Assert.That(
                        controllerAttribute,
                        Is.Not.Null,
                        $"{controllerType.Name} must have [AllowAnonymous].");

                    continue;
                }

                // Specific actions anonymous
                foreach (var actionName in anonymousActions)
                {
                    var method = controllerType.GetMethod(actionName);

                    Assert.That(method, Is.Not.Null,
                        $"{controllerType.Name}.{actionName} was not found.");

                    var attribute =
                        method!.GetCustomAttribute<AllowAnonymousAttribute>();

                    Assert.That(
                        attribute,
                        Is.Not.Null,
                        $"{controllerType.Name}.{actionName} must have [AllowAnonymous].");
                }
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
