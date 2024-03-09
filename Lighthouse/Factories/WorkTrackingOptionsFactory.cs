using Lighthouse.WorkTracking;
using Lighthouse.WorkTracking.AzureDevOps;
using Lighthouse.WorkTracking.Jira;

namespace Lighthouse.Factories
{
    public class WorkTrackingOptionsFactory : IWorkTrackingOptionsFactory
    {
        public IEnumerable<WorkTrackingSystemOption<T>> CreateOptionsForWorkTrackingSystem<T>(WorkTrackingSystems workTrackingSystem) where T : class
        {
            switch (workTrackingSystem)
            {
                case WorkTrackingSystems.AzureDevOps:
                    return GetOptionsForAzureDevOps<T>();
                case WorkTrackingSystems.Jira:
                    return GetOptionsForJira<T>();
                default:
                    return Enumerable.Empty<WorkTrackingSystemOption<T>>();
            }
        }

        private List<WorkTrackingSystemOption<T>> GetOptionsForJira<T>() where T : class
        {
            return new List<WorkTrackingSystemOption<T>>
            {
                new WorkTrackingSystemOption<T>(JiraWorkTrackingOptionNames.Url, string.Empty, false),
                new WorkTrackingSystemOption<T>(JiraWorkTrackingOptionNames.Username, string.Empty, false),
                new WorkTrackingSystemOption<T>(JiraWorkTrackingOptionNames.ApiToken, string.Empty, true),
            };
        }

        private List<WorkTrackingSystemOption<T>> GetOptionsForAzureDevOps<T>() where T : class
        {
            return new List<WorkTrackingSystemOption<T>>
            {
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.Url, string.Empty, false),
                new WorkTrackingSystemOption<T>(AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken, string.Empty, true),
            };
        }
    }
}
