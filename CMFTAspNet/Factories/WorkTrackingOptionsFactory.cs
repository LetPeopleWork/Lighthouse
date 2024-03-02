using CMFTAspNet.WorkTracking;
using CMFTAspNet.WorkTracking.AzureDevOps;

namespace CMFTAspNet.Factories
{
    public class WorkTrackingOptionsFactory : IWorkTrackingOptionsFactory
    {
        public IEnumerable<WorkTrackingSystemOption<T>> CreateOptionsForWorkTrackingSystem<T>(WorkTrackingSystems workTrackingSystem) where T : class
        {
            switch (workTrackingSystem)
            {
                case WorkTrackingSystems.AzureDevOps:
                    return GetOptionsForAzureDevOps<T>();
                default:
                    return Enumerable.Empty<WorkTrackingSystemOption<T>>();
            }
        }

        private List<WorkTrackingSystemOption<T>> GetOptionsForAzureDevOps<T>() where T : class
        {
            return new List<WorkTrackingSystemOption<T>>
            {
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.Url, string.Empty, false),
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.TeamProject, string.Empty, false),
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.AreaPaths, string.Empty, false),
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, string.Empty, true),
            };
        }
    }
}
