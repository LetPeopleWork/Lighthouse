using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.Services.Interfaces
{
    public interface IWriteBackTriggerService
    {
        Task TriggerWriteBackForTeam(Team team);

        Task TriggerForecastWriteBackForPortfolio(Portfolio portfolio);

        Task TriggerFeatureWriteBackForPortfolio(Portfolio portfolio);
    }
}
