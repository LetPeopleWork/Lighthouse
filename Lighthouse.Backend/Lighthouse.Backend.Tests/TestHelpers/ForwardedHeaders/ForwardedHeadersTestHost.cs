using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Tests.TestHelpers.ForwardedHeaders
{
    public sealed class ForwardedHeadersTestHost : IDisposable
    {
        private readonly TestWebApplicationFactory<Program> rootFactory;
        private readonly WebApplicationFactory<Program> factory;
        private readonly HttpClient client;

        public ForwardedHeadersTestHost(ForwardedHeadersTestOptions options)
        {
            rootFactory = new TestWebApplicationFactory<Program>();

            factory = rootFactory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(BuildTrustConfiguration(options));
                });

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter>(
                        new ForwardedHeadersTestStartupFilter(options.SimulatedRemoteIp));
                });
            });

            client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });
        }

        public async Task<ObservedRequest> GetObservedRequestAsync(string? forwardedProto = null, string? forwardedHost = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ForwardedHeadersTestStartupFilter.EchoPath);
            request.Headers.Add("X-Forwarded-For", "198.51.100.42");
            if (forwardedProto is not null)
            {
                request.Headers.Add("X-Forwarded-Proto", forwardedProto);
            }

            if (forwardedHost is not null)
            {
                request.Headers.Add("X-Forwarded-Host", forwardedHost);
            }

            using var response = await client.SendAsync(request);
            return await response.Content.ReadFromJsonAsync<ObservedRequest>()
                   ?? throw new InvalidOperationException("Echo endpoint returned no observable request state.");
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
            rootFactory.Dispose();
        }

        private static Dictionary<string, string?> BuildTrustConfiguration(ForwardedHeadersTestOptions options)
        {
            var configuration = new Dictionary<string, string?>();

            for (var index = 0; index < options.TrustedProxies.Count; index++)
            {
                configuration[$"Authentication:TrustedProxies:{index}"] = options.TrustedProxies[index];
            }

            for (var index = 0; index < options.TrustedNetworks.Count; index++)
            {
                configuration[$"Authentication:TrustedNetworks:{index}"] = options.TrustedNetworks[index];
            }

            return configuration;
        }
    }
}
