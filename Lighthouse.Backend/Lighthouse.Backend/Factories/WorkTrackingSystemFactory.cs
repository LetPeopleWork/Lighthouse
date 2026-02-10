using Lighthouse.Backend.Models;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.AzureDevOps;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Jira;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.Linear;

namespace Lighthouse.Backend.Factories
{
    public class WorkTrackingSystemFactory(ILogger<WorkTrackingSystemFactory> logger) : IWorkTrackingSystemFactory
    {
        public WorkTrackingSystemConnection CreateDefaultConnectionForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            var newConnection = new WorkTrackingSystemConnection
            {
                Name = $"New {workTrackingSystem} Connection",
                WorkTrackingSystem = workTrackingSystem,
                AuthenticationMethodKey = AuthenticationMethodKeys.GetDefaultForSystem(workTrackingSystem),
            };

            var defaultOptions = CreateOptionsForWorkTrackingSystem(workTrackingSystem);
            newConnection.Options.AddRange(defaultOptions);

            return newConnection;
        }

        private List<WorkTrackingSystemConnectionOption> CreateOptionsForWorkTrackingSystem(WorkTrackingSystems workTrackingSystem)
        {
            logger.LogDebug("Getting Default WorkTrackingSystemConnectionOption for {WorkTrackingSystem}", workTrackingSystem);

            switch (workTrackingSystem)
            {
                case WorkTrackingSystems.AzureDevOps:
                    return GetOptionsForAzureDevOps();
                case WorkTrackingSystems.Jira:
                    return GetOptionsForJira();
                case WorkTrackingSystems.Linear:
                    return GetOptionsForLinear();
                case WorkTrackingSystems.Csv:
                    return GetOptionsForCsv();
                default:
                    throw new NotSupportedException("Selected Work Tracking System is Not Supported");
            }
        }

        private static List<WorkTrackingSystemConnectionOption> GetOptionsForCsv()
        {
            return
            [
                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.Delimiter,
                    Value = ",",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.DateTimeFormat,
                    Value = "yyyy-MM-dd HH:mm:ss",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.TagSeparator,
                    Value = ";",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.IdHeader,
                    Value = "ID",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.NameHeader,
                    Value = "Name",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.StateHeader,
                    Value = "State",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.TypeHeader,
                    Value = "Type",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.StartedDateHeader,
                    Value = "Started Date",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.ClosedDateHeader,
                    Value = "Closed Date",
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.CreatedDateHeader,
                    Value = "Created Date",
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.ParentReferenceIdHeader,
                    Value = "Parent Reference Id",
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.TagsHeader,
                    Value = "Tags",
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.UrlHeader,
                    Value = "Url",
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.OwningTeamHeader,
                    Value = "Owning Team",
                    IsOptional = true,
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = CsvWorkTrackingOptionNames.EstimatedSizeHeader,
                    Value = "Estimated Size",
                    IsOptional = true,
                }
            ];
        }

        private static List<WorkTrackingSystemConnectionOption> GetOptionsForJira()
        {
            return
            [
                new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.Url,
                    Value = string.Empty,
                    IsSecret = false,
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

                new WorkTrackingSystemConnectionOption
                {
                    Key = JiraWorkTrackingOptionNames.RequestTimeoutInSeconds,
                    Value = "100",
                    IsSecret = false,
                    IsOptional = true,
                }
            ];
        }

        private static List<WorkTrackingSystemConnectionOption> GetOptionsForAzureDevOps()
        {
            return
            [
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
                },

                new WorkTrackingSystemConnectionOption
                {
                    Key = AzureDevOpsWorkTrackingOptionNames.RequestTimeoutInSeconds,
                    Value = "100",
                    IsSecret = false,
                    IsOptional = true,
                }
            ];
        }

        private static List<WorkTrackingSystemConnectionOption> GetOptionsForLinear()
        {
            return
            [
                new WorkTrackingSystemConnectionOption
                {
                    Key = LinearWorkTrackingOptionNames.ApiKey,
                    Value = string.Empty,
                    IsSecret = true,
                    IsOptional = false,
                }
            ];
        }
    }
}
