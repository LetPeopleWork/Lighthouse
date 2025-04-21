using Newtonsoft.Json;

namespace Lighthouse.Backend.MCP
{
    public abstract class LighthouseToolsBase
    {
        private readonly IServiceScopeFactory serviceScopeFactory;

        protected LighthouseToolsBase(IServiceScopeFactory serviceScopeFactory)
        {
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected static T GetServiceFromServiceScope<T>(IServiceScope scope) where T : notnull
        {
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        protected IServiceScope CreateServiceScope()
        {
            return serviceScopeFactory.CreateScope();
        }

        protected string ToJson(object obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.Indented,
                new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
        }
    }
}
