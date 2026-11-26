using Lighthouse.Backend.Models;
using Lighthouse.Backend.Modules.WorkItems.Application.Dependencies;
using Lighthouse.Backend.Modules.WorkItems.Application;
using Lighthouse.Backend.Modules.WorkItems.Domain.Dependencies;

namespace Lighthouse.Backend.Modules.WorkItems
{
    public static class WorkItemsModule
    {
        public static IServiceCollection AddWorkItems(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<IDependencyReconciler, DependencyReconciler>();
            serviceCollection.AddScoped<IDependencyHonourPolicy, DependencyHonourPolicy>();
            serviceCollection.AddScoped<IDependencyDecision, DependencyDecision>();
            serviceCollection.AddScoped<IWhatTheForecastWaitsFor, WhatTheForecastWaitsFor>();
            serviceCollection.AddScoped<IDependencyRefreshReporter, DependencyRefreshReporter>();
            serviceCollection.AddScoped<FeatureDependencyRendering>();
            serviceCollection.AddScoped<ITeamDataService, TeamDataService>();
            serviceCollection.AddScoped<IWorkItemService, WorkItemService>();
            serviceCollection.AddScoped<IRefreshLogService, RefreshLogService>();
            serviceCollection.AddScoped<IRuleEvaluator<WorkItem>, RuleEvaluator<WorkItem>>();
            serviceCollection.AddScoped<IRuleFieldProvider<WorkItem>, WorkItemFieldProvider>();
            serviceCollection.AddScoped<IBlockedItemService, BlockedItemService>();

            return serviceCollection;
        }
    }
}
