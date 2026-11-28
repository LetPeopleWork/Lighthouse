namespace Lighthouse.Backend.Modules.OrganisationSetup.Domain
{
    public readonly record struct PortfolioSize(int FeatureCount)
    {
        public const int Threshold = 100;

        public bool IsOverThreshold => FeatureCount > Threshold;
    }
}
