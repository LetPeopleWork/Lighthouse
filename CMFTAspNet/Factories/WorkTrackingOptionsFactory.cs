using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.AzureDevOps;

namespace CMFTAspNet.Factories
{
    public class WorkTrackingOptionsFactory : IWorkTrackingOptionsFactory
    {
        public IEnumerable<WorkTrackingSystemOption> CreateOptionsForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            switch (workTrackingSystem)
            {
                case WorkTrackingSystems.AzureDevOps:
                    return GetOptionsForAzureDevOps();
                default:
                    return Enumerable.Empty<WorkTrackingSystemOption>();
            }
        }

        private List<WorkTrackingSystemOption> GetOptionsForAzureDevOps()
        {
            return new List<WorkTrackingSystemOption>
            {
                new WorkTrackingSystemOption(AzureDevOpsWorkTrackingOptionNames.AzureDevOpsUrl, string.Empty),
                new WorkTrackingSystemOption(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, string.Empty, true),
            };
        }
    }
}
