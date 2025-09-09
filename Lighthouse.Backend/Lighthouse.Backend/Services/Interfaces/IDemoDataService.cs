using Lighthouse.Backend.Models.DemoData;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IDemoDataService
    {
        IEnumerable<DemoDataScenario> GetAllScenarios();

        Task LoadScenarios(params DemoDataScenario[] scenarios);
    }
}
