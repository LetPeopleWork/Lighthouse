using Lighthouse.Backend.Modules.Access.Application;
using Lighthouse.Backend.Modules.Access.Contracts;

namespace Lighthouse.Backend.Modules.Access
{
    public static class AccessModule
    {
        public static IServiceCollection AddAccess(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IApiKeyService, ApiKeyService>();
            serviceCollection.AddScoped<IEmbedSessionTokenService, EmbedSessionTokenService>();
            serviceCollection.AddScoped<IFeatureMoveAuthorization, FeatureMoveAuthorization>();
            serviceCollection.AddSingleton<IAuthConfigurationValidator, AuthConfigurationValidator>();
            serviceCollection.AddScoped<IAuthModeResolver, AuthModeResolver>();
            serviceCollection.AddScoped<ICurrentUserProfileService, CurrentUserProfileService>();
            serviceCollection.AddScoped<IRbacAdministrationService, RbacAdministrationService>();
            serviceCollection.AddScoped<IOidcGroupSnapshotWriter, OidcGroupSnapshotWriter>();

            return serviceCollection;
        }
    }
}
