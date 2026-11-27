using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Background;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Auth;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.AzureDevOps;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Csv;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Jira;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.Linear;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.Connectors.ServiceNow;
using Lighthouse.Backend.Modules.TrackerIntegration.Adapters.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.ConnectionHealth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application.OAuth;
using Lighthouse.Backend.Modules.TrackerIntegration.Application;
using Lighthouse.Backend.Modules.TrackerIntegration.Contracts;
using Lighthouse.Backend.Platform.Host;

namespace Lighthouse.Backend.Modules.TrackerIntegration
{
    public static class TrackerIntegrationModule
    {
        public static IServiceCollection AddTrackerIntegration(this IServiceCollection serviceCollection, IConfiguration configuration)
        {
            serviceCollection.AddScoped<IWorkTrackingConnectorFactory, WorkTrackingConnectorFactory>();
            serviceCollection.AddScoped<IIssueFactory, IssueFactory>();
            serviceCollection.AddScoped<IWorkTrackingSystemFactory, WorkTrackingSystemFactory>();
            serviceCollection.AddScoped<IAzureDevOpsWorkTrackingConnector, AzureDevOpsWorkTrackingConnector>();
            serviceCollection.AddScoped<IJiraWorkTrackingConnector, JiraWorkTrackingConnector>();
            serviceCollection.AddScoped<ILinearWorkTrackingConnector, LinearWorkTrackingConnector>();
            serviceCollection.AddScoped<CsvWorkTrackingConnector>();
            serviceCollection.AddScoped<IServiceNowWorkTrackingConnector, ServiceNowWorkTrackingConnector>();
            serviceCollection.AddSingleton<IOAuthStateTokenIssuer, OAuthStateTokenIssuer>();
            serviceCollection.AddSingleton<IOAuthProviderRegistry, OAuthProviderRegistry>();

            var stubModeEnabled = string.Equals(
                configuration[UseStubOAuthProviderConfigKey],
                "true",
                StringComparison.OrdinalIgnoreCase);

            if (!stubModeEnabled)
            {
                serviceCollection.AddSingleton<IOAuthProvider, JiraOAuthProvider>();
                serviceCollection.AddSingleton<IOAuthProvider, AdoOAuthProvider>();
            }

            RegisterStubOAuthProviderIfEnabled(serviceCollection, configuration);

            serviceCollection.AddScoped<IOAuthService, OAuthService>();
            serviceCollection.AddScoped<ConnectionHealthCadence>();
            // Registered as itself as well, because a pass over the stale connections resolves one of these
            // per connection so that each gets a database context of its own. The interface then resolves
            // to that same object, so a scope holds one health service rather than two.
            serviceCollection.AddScoped<ConnectionHealthService>();
            serviceCollection.AddScoped<IConnectionHealthService>(services => services.GetRequiredService<ConnectionHealthService>());

            // Registered as itself as well as as the background job, for the same reason the usage-data
            // services are: asking the stale connections at a moment of the caller's choosing is the only
            // way a test can observe this at all, since a test host runs no background work.
            serviceCollection.AddSingleton<ConnectionHealthProber>();
            serviceCollection.AddHostedService(services => services.GetRequiredService<ConnectionHealthProber>());
            serviceCollection.AddScoped<PatAuthStrategy>();
            serviceCollection.AddScoped<JiraCloudBasicAuthStrategy>();
            serviceCollection.AddScoped<LinearApiKeyAuthStrategy>();
            serviceCollection.AddScoped<ServiceNowBasicAuthStrategy>();
            serviceCollection.AddScoped<NoOpAuthStrategy>();
            serviceCollection.AddScoped<OAuthBearerAuthStrategy>();
            serviceCollection.AddScoped<IWorkTrackingAuthStrategyFactory, WorkTrackingAuthStrategyFactory>();

            return serviceCollection;
        }

        private const string UseStubOAuthProviderConfigKey = "Lighthouse:OAuth:UseStubProvider";

        private static void RegisterStubOAuthProviderIfEnabled(IServiceCollection serviceCollection, IConfiguration configuration)
        {
            var useStub = configuration[UseStubOAuthProviderConfigKey];
            if (!string.Equals(useStub, "true", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // Register the stub under its dedicated key so abstraction-honesty integration tests can
            // exercise a brand-new IOAuthProvider via DI only.
            serviceCollection.AddSingleton<IOAuthProvider>(sp =>
            {
                var serviceConfig = sp.GetRequiredService<IServiceConfig>();
                var timeProvider = sp.GetRequiredService<TimeProvider>();
                return new StubOAuthProvider(serviceConfig, timeProvider, AuthenticationMethodKeys.StubOAuth);
            });
            serviceCollection.AddSingleton<IOAuthSchemaExtensions>(
                new OAuthSchemaExtensions(new[] { AuthenticationMethodKeys.StubOAuth }));

            // Substitute the stub for every real *.oauth method declared in the schema so Playwright
            // walking-skeleton scenarios can drive jira.oauth (and future ado.oauth) connections
            // without contacting external identity providers.
            var realOAuthKeys = AuthenticationMethodSchema.GetOAuthProviderKeys().ToList();
            foreach (var key in realOAuthKeys)
            {
                serviceCollection.AddSingleton<IOAuthProvider>(sp =>
                    new StubOAuthProvider(
                        sp.GetRequiredService<IServiceConfig>(),
                        sp.GetRequiredService<TimeProvider>(),
                        key));
            }
        }

        public static void EnsureOAuthProvidersRegistered(WebApplication app)
        {
            var registry = app.Services.GetRequiredService<IOAuthProviderRegistry>();
            var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("OAuthProvidersStartupCheck");

            var extras = app.Services.GetService<IOAuthSchemaExtensions>()?.ExtraOAuthKeys
                ?? Array.Empty<string>();
            var missingKeys = AuthenticationMethodSchema.GetOAuthProviderKeys()
                .Concat(extras)
                .Where(key => !TryResolveProvider(registry, key))
                .ToList();

            if (missingKeys.Count == 0)
            {
                return;
            }

            var message =
                "OAuth authentication methods declared in AuthenticationMethodSchema have no matching " +
                $"IOAuthProvider registered: [{string.Join(", ", missingKeys)}]. " +
                "Every '*.oauth' key in the schema must have a corresponding IOAuthProvider registration in DI.";
            logger.LogCritical("{Message}", message);
            throw new InvalidOperationException(message);
        }

        private static bool TryResolveProvider(IOAuthProviderRegistry registry, string key)
        {
            try
            {
                _ = registry.GetByKey(key);
                return true;
            }
            catch (OAuthProviderNotFoundException)
            {
                return false;
            }
        }
    }
}
