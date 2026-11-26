namespace Lighthouse.Backend.Modules.Refinement
{
    public static class RefinementModule
    {
        public static IServiceCollection AddRefinement(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.RefinementList>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.SleYardstickResolver>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.VoterIdentityResolver>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.StageRuleMatcher>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.IRefinementCalendar, Lighthouse.Backend.Modules.Refinement.Application.RefinementCalendar>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.RefinementNeedCalculator>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.IRefinementViewQuery, Lighthouse.Backend.Modules.Refinement.Application.RefinementViewQuery>();
            serviceCollection.AddScoped<Lighthouse.Backend.Modules.Refinement.Application.ISizingLogCommands, Lighthouse.Backend.Modules.Refinement.Application.SizingLogCommands>();

            return serviceCollection;
        }
    }
}
