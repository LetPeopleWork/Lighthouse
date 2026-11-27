using Lighthouse.Backend.Models.Events;

namespace Lighthouse.Backend.Modules.Forecasting.Contracts
{
    public record PortfolioForecastsUpdated(int PortfolioId) : IDomainEvent;
}
