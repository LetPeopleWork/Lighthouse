using Lighthouse.Backend.Models.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Lighthouse.Backend.Tests.Models.Auth
{
    [TestFixture]
    public class AuthorizationConfigurationBindingTest
    {
        [Test]
        public void Bind_FromIndexedEnvironmentVariables_PopulatesEmergencySystemAdminSubjects()
        {
            var environment = new Dictionary<string, string?>
            {
                ["Authorization:Enabled"] = "true",
                ["Authorization:EmergencySystemAdminSubjects:0"] = "first-subject",
                ["Authorization:EmergencySystemAdminSubjects:1"] = "second-subject",
                ["Authorization:GroupClaimName"] = "groups",
            };

            var configuration = BuildConfigurationFromEnvironmentLikeSource(environment);

            var config = BindAuthorizationConfiguration(configuration);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(config.Enabled, Is.True);
                Assert.That(config.GroupClaimName, Is.EqualTo("groups"));
                Assert.That(config.EmergencySystemAdminSubjects, Is.EqualTo(new[] { "first-subject", "second-subject" }));
            }
        }

        [Test]
        public void Bind_FromJsonEncodedStringEnvironmentVariable_LeavesEmergencySystemAdminSubjectsEmpty()
        {
            // Documents the .NET environment-variable binding behaviour: passing the array as a single
            // JSON-encoded string (e.g. Authorization__EmergencySystemAdminSubjects='["a"]') silently
            // binds to the empty default, because the env-var provider stores it as a section Value
            // rather than as Children. CI YAML must use the indexed form (__0, __1, ...).
            var environment = new Dictionary<string, string?>
            {
                ["Authorization:Enabled"] = "true",
                ["Authorization:EmergencySystemAdminSubjects"] = "[\"bc58019b-61fb-4cf9-9315-976faf7238aa\"]",
            };

            var configuration = BuildConfigurationFromEnvironmentLikeSource(environment);

            var config = BindAuthorizationConfiguration(configuration);

            Assert.That(config.EmergencySystemAdminSubjects, Is.Empty);
        }

        private static IConfiguration BuildConfigurationFromEnvironmentLikeSource(IDictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        private static AuthorizationConfiguration BindAuthorizationConfiguration(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            services.Configure<AuthorizationConfiguration>(configuration.GetSection("Authorization"));
            using var provider = services.BuildServiceProvider();
            return provider.GetRequiredService<IOptions<AuthorizationConfiguration>>().Value;
        }
    }
}
