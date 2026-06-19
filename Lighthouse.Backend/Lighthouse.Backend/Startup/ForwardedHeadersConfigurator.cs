using System.Net;
using Lighthouse.Backend.Models.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Lighthouse.Backend.Startup
{
    public static class ForwardedHeadersConfigurator
    {
        public static void Configure(IServiceCollection services, IConfiguration configuration, AuthenticationConfiguration authConfig)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // An empty known-set makes the middleware honour forwarded headers from ANY source, so clearing
                // the ASP.NET loopback defaults is what keeps trust strictly opt-in to the declared proxy set.
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();

                foreach (var proxy in ResolveTrustedProxies(configuration, authConfig))
                {
                    if (IPAddress.TryParse(proxy, out var ip))
                    {
                        options.KnownProxies.Add(ip);
                    }
                }

                foreach (var network in ResolveTrustedNetworks(configuration, authConfig))
                {
                    var parts = network.Split('/');
                    if (parts.Length == 2
                        && IPAddress.TryParse(parts[0], out var prefix)
                        && int.TryParse(parts[1], out var prefixLength))
                    {
                        options.KnownIPNetworks.Add(new IPNetwork(prefix, prefixLength));
                    }
                }

                var trustDeclared = options.KnownProxies.Count > 0 || options.KnownIPNetworks.Count > 0;
                options.ForwardedHeaders = trustDeclared
                    ? Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto |
                      Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost
                    : Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.None;
            });
        }

        private static List<string> ResolveTrustedProxies(IConfiguration configuration, AuthenticationConfiguration authConfig)
        {
            var fromConfiguration = configuration.GetSection("Authentication:TrustedProxies").Get<List<string>>();
            return fromConfiguration is { Count: > 0 } ? fromConfiguration : [.. authConfig.TrustedProxies];
        }

        private static List<string> ResolveTrustedNetworks(IConfiguration configuration, AuthenticationConfiguration authConfig)
        {
            var fromConfiguration = configuration.GetSection("Authentication:TrustedNetworks").Get<List<string>>();
            return fromConfiguration is { Count: > 0 } ? fromConfiguration : [.. authConfig.TrustedNetworks];
        }
    }
}
