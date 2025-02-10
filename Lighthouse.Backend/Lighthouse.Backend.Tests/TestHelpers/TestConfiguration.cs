using Microsoft.Extensions.Configuration;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    internal static class TestConfiguration
    {
        public static IConfiguration SetupTestConfiguration(Dictionary<string, string?> testConfigValues)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(testConfigValues)
                .Build();
        }
    }
}
