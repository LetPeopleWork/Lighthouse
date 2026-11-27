using Lighthouse.Backend.Modules.OrganisationSetup.Application.Connections;
using Lighthouse.Backend.Modules.OrganisationSetup.Application;
using Lighthouse.Backend.Modules.OrganisationSetup.Contracts;

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
    }
}
