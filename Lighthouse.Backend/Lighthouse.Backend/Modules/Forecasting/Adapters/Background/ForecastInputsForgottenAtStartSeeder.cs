using Lighthouse.Backend.Services.Interfaces.Repositories;
using Lighthouse.Backend.Services.Interfaces.Seeding;

namespace Lighthouse.Backend.Modules.Forecasting.Adapters.Background
{
    // While this instance was down, another build may have computed forecasts without recording what from,
    // so nothing recorded before this start can vouch for the forecasts now stored.
    public class ForecastInputsForgottenAtStartSeeder(IPortfolioRepository portfolioRepository) : ISeeder
    {
        public Task Seed()
        {
            return portfolioRepository.ForgetAllForecastInputs(CancellationToken.None);
        }
    }
}
