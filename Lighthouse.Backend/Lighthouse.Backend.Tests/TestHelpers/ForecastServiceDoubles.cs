using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Interfaces.Forecast;
using Moq;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// A forecast double that ForecastUpdater can reach must answer what its run did: a bare Moq double
    /// hands back a null outcome, which no real forecast service ever does. These answer "computed" unless
    /// a test sets up something else.
    /// </summary>
    public static class ForecastServiceDoubles
    {
        public static ForecastRunOutcome Computed => new ForecastRunOutcome.Computed(null, null);

        public static Mock<IForecastService> AnsweringComputed()
        {
            return new Mock<IForecastService>().AnsweringComputedByDefault();
        }

        public static Mock<IForecastService> AnsweringComputedByDefault(this Mock<IForecastService> forecastService)
        {
            forecastService
                .Setup(s => s.UpdateForecastsForPortfolio(It.IsAny<Portfolio>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Computed);

            return forecastService;
        }
    }
}
