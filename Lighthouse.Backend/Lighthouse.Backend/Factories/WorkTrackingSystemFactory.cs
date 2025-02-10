using Lighthouse.Backend.Models;
using Lighthouse.Backend.WorkTracking;
using Lighthouse.Backend.WorkTracking.AzureDevOps;
using Lighthouse.Backend.WorkTracking.Jira;

namespace Lighthouse.Backend.Factories
{
    public class WorkTrackingSystemFactory : IWorkTrackingSystemFactory
    {
        private readonly ILogger<WorkTrackingSystemFactory> logger;

        public WorkTrackingSystemFactory(ILogger<WorkTrackingSystemFactory> logger)
        {
            this.logger = logger;
        }

        public WorkTrackingSystemConnection CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            var newConnection = new WorkTrackingSystemConnection
            {
                Name = $"New {workTrackingSystem.ToString()} Connection",
                WorkTrackingSystem = workTrackingSystem
            };

            var defaultOptions = CreateOptionsForWorkTrackingSystem(workTrackingSystem);
            newConnection.Options.AddRange(defaultOptions);

            return newConnection;
        }

        private List<WorkTrackingSystemConnectionOption> CreateOptionsForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            logger.LogDebug("Getting Default WorkTrackingSystemConnectionOption for {workTrackingSystem}", workTrackingSystem);

            switch (workTrackingSystem)
            {
                case WorkTrackingSystems.AzureDevOps:
                    return GetOptionsForAzureDevOps();
                case WorkTrackingSystems.Jira:
                    return GetOptionsForJira();
                default:
                    throw new NotSupportedException("Selected Work Tracking System is Not Supported");
            }
        }

        private List<WorkTrackingSystemConnectionOption> GetOptionsForJira()
        {
            return new List<WorkTrackingSystemConnectionOption>
            {
                new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.Url,
                    Value = string.Empty,
                    IsSecret =false,
                    IsOptional = false,
                },
                new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.Username,
                    Value = string.Empty,
                    IsSecret = false,
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.ApiToken,
                    Value = string.Empty,
                    IsSecret = true,
                    IsOptional = false,
                },
            };
        }

        private List<WorkTrackingSystemConnectionOption> GetOptionsForAzureDevOps()
        {
            return new List<WorkTrackingSystemConnectionOption>
            {
                new WorkTrackingSystemConnectionOption
                {
                    Key = AzureDevOpsWorkTrackingOptionNames.Url,
                    Value = string.Empty,
                    IsSecret = false,
                    IsOptional = false,
                },
                new WorkTrackingSystemConnectionOption
                {
                    Key = AzureDevOpsWorkTrackingOptionNames.PersonalAccessToken,
                    Value = string.Empty,
                    IsSecret = true,
                    IsOptional = false,
                }
            };
        }
    }
}
