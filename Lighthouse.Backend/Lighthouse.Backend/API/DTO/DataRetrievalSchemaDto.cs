using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors;
using Lighthouse.Backend.Services.Implementation.WorkTrackingConnectors.ServiceNow;

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

        /// <summary>
        /// What a team's settings screen asks for, and what it refuses to save without.
        /// </summary>
        /// <param name="system">The connection's work tracking system.</param>
        /// <param name="workItemTable">
        /// The ServiceNow table the connection reads, ignored by every other arm. Deliberately
        /// without a default value (ADR-123 decision 6), so a call site cannot inherit leaf-table
        /// semantics by forgetting to answer. Twinned in <c>DataRetrievalSchemaDefaults.ts</c>;
        /// the two disagreeing is Bug #5613, which shipped teams that could not be saved.
        /// </param>
        public static DataRetrievalSchemaDto ForTeam(WorkTrackingSystems system, string workItemTable)
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
                WorkTrackingSystems.ServiceNow => new DataRetrievalSchemaDto
                {
                    Key = "servicenow.query",
                    DisplayLabel = "ServiceNow Query (Encoded Query)",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    // A table with descendants read unfiltered returns the whole instance's work, so
                    // the kinds of work stop being optional (ADR-123 decision 6).
                    IsWorkItemTypesRequired = ServiceNowTableHierarchy.HasDescendants(workItemTable),
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
                // SPIKE Q5 measured no forecastable rollup in ITSM, so the capability is declined here (ADR-116).
                WorkTrackingSystems.ServiceNow => new DataRetrievalSchemaDto
                {
                    Key = "servicenow.query",
                    DisplayLabel = "Not supported for ServiceNow",
                    InputKind = "none",
                    IsRequired = false,
                    IsWorkItemTypesRequired = false,
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
