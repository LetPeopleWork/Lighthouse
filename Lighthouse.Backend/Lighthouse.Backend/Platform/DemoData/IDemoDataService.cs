using Lighthouse.Backend.Models.DemoData;

namespace Lighthouse.Backend.Platform.DemoData
{
    public interface IDemoDataService
    {
        IEnumerable<DemoDataScenario> GetAllScenarios();

        Task LoadScenarios(params DemoDataScenario[] scenariosToLoad);
    }
}
