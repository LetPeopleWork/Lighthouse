namespace Lighthouse.Backend.Models
{
    public readonly record struct PortfolioSize(int FeatureCount)
    {
        public const int Threshold = 100;

        public bool IsOverThreshold => FeatureCount > Threshold;
    }
}
