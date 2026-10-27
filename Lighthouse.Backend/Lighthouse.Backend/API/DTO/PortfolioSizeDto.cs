using Lighthouse.Backend.Models;

namespace Lighthouse.Backend.API.DTO
{
    public class PortfolioSizeDto(PortfolioSize size)
    {
        public int FeatureCount { get; } = size.FeatureCount;

        public int Threshold { get; } = PortfolioSize.Threshold;

        public bool IsOverThreshold { get; } = size.IsOverThreshold;
    }
}
