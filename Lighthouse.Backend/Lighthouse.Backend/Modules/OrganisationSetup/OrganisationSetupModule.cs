using Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Application.Structure;
using Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Persistence;
using Lighthouse.Backend.Modules.OrganisationSetup.Adapters.Startup;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;
using Lighthouse.Backend.Platform.Seeding;

namespace Lighthouse.Backend.Modules.OrganisationSetup
{
    public static class OrganisationSetupModule
    {
        public static IServiceCollection AddOrganisationSetup(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IWorkTrackingSystemConnectionUsage, WorkTrackingSystemConnectionUsage>();
            serviceCollection.AddScoped<IAppSettingService, AppSettingService>();
            serviceCollection.AddScoped<IFeatureOrderingPolicyProvider, FeatureOrderingPolicyProvider>();
            serviceCollection.AddScoped<IFeatureOrdering, FeatureOrdering>();
            serviceCollection.AddScoped<IFeatureRankSeeder, FeatureRankSeeder>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.DefaultOptionalFeatureApplier>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.IOptionalFeatureApplier, Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.FeatureOrderingApplier>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.OrganisationSetup.Application.OptionalFeatures.OptionalFeatureApplierRegistry>();
            serviceCollection.AddScoped<IFeatureRankingService, FeatureRankingService>();
            serviceCollection.AddScoped<ITerminologyService, TerminologyService>();
            serviceCollection.AddScoped<IBlackoutPeriodService, BlackoutPeriodService>();
            serviceCollection.AddScoped<IRecurringBlackoutRuleService, RecurringBlackoutRuleService>();
            serviceCollection.AddScoped<IConnectionSaveService, Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections.ConnectionSaveService>();

            return serviceCollection;
        }

        public static IServiceCollection AddOrganisationStructure(this IServiceCollection services)
        {
            services.AddScoped<IOrganisationStructureStore, OrganisationStructureStore>();
            services.AddScoped<IConnectionMappingStore, ConnectionMappingStore>();
            services.AddScoped<IOrganisationStructureQuery, OrganisationStructureQuery>();
            services.AddScoped<ISeeder, OrganisationStructureMigrator>();

            return services;
        }
    }
}
