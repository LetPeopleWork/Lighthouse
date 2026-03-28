using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;

namespace Lighthouse.Backend.API.DTO
{
    public class DataRetrievalSchemaDto
    {
        private const string FreeTextInput = "freetext";

        public string Key { get; set; } = string.Empty;

        public string DisplayLabel { get; set; } = string.Empty;

        public string InputKind { get; set; } = FreeTextInput;

        public bool IsRequired { get; set; } = true;

        public bool IsWorkItemTypesRequired { get; set; } = true;

        public string? WizardHint { get; set; }

        public static DataRetrievalSchemaDto ForTeam(WorkTrackingSystems system)
        {
            return system switch
            {
                WorkTrackingSystems.AzureDevOps => new DataRetrievalSchemaDto
                {
                    Key = "ado.wiql",
                    DisplayLabel = "WIQL Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "ado-team-wizard",
                },
                WorkTrackingSystems.Jira => new DataRetrievalSchemaDto
                {
                    Key = "jira.jql",
                    DisplayLabel = "JQL Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "jira-team-wizard",
                },
                WorkTrackingSystems.Linear => new DataRetrievalSchemaDto
                {
                    Key = "linear.team",
                    DisplayLabel = "Linear Team",
                    InputKind = "wizard-select",
                    IsRequired = true,
                    IsWorkItemTypesRequired = false,
                    WizardHint = "linear-team-select",
                },
                WorkTrackingSystems.Csv => new DataRetrievalSchemaDto
                {
                    Key = "csv.filedata",
                    DisplayLabel = "CSV File Content",
                    InputKind = "file-upload",
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "csv-team-wizard",
                },
                _ => new DataRetrievalSchemaDto
                {
                    Key = "query",
                    DisplayLabel = "Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                },
            };
        }

        public static DataRetrievalSchemaDto ForPortfolio(WorkTrackingSystems system)
        {
            return system switch
            {
                WorkTrackingSystems.AzureDevOps => new DataRetrievalSchemaDto
                {
                    Key = "ado.wiql",
                    DisplayLabel = "WIQL Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "ado-portfolio-wizard",
                },
                WorkTrackingSystems.Jira => new DataRetrievalSchemaDto
                {
                    Key = "jira.jql",
                    DisplayLabel = "JQL Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "jira-portfolio-wizard",
                },
                WorkTrackingSystems.Linear => new DataRetrievalSchemaDto
                {
                    Key = "linear.projects",
                    DisplayLabel = "Linear Projects",
                    InputKind = "none",
                    IsRequired = false,
                    IsWorkItemTypesRequired = false,
                },
                WorkTrackingSystems.Csv => new DataRetrievalSchemaDto
                {
                    Key = "csv.filedata",
                    DisplayLabel = "CSV File Content",
                    InputKind = "file-upload",
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                    WizardHint = "csv-portfolio-wizard",
                },
                _ => new DataRetrievalSchemaDto
                {
                    Key = "query",
                    DisplayLabel = "Query",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    IsWorkItemTypesRequired = true,
                },
            };
        }
    }
}
