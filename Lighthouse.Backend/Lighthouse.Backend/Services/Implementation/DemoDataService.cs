using Lighthouse.Backend.Models.DemoData;
using Lighthouse.Backend.Services.Interfaces;

namespace Lighthouse.Backend.Services.Implementation
{
    public class DemoDataService : IDemoDataService
    {
        public IEnumerable<DemoDataScenario> GetAllScenarios()
        {
            return [];
        }

        public Task LoadScenarios(params DemoDataScenario[] scenarios)
        {
            return Task.CompletedTask;
        }
    }
}
