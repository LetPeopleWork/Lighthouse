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

        // Rendered on the query field as its placeholder and helper text. Null for a connector with
        // nothing to explain, which then renders exactly what it renders today (#5610, DD-5).
        public string? Placeholder { get; set; }

        public string? HelpText { get; set; }

        /// <summary>
        /// What a team's settings screen asks for, and what it refuses to save without. Twinned in
        /// <c>DataRetrievalSchemaDefaults.ts</c>; the two disagreeing is Bug #5613.
        /// </summary>
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
                WorkTrackingSystems.ServiceNow => new DataRetrievalSchemaDto
                {
                    Key = "servicenow.query",
                    DisplayLabel = "ServiceNow Query (Encoded Query)",
                    InputKind = FreeTextInput,
                    IsRequired = true,
                    // Always, whatever table the connection reads (ADR-123 decision 6, amended
                    // 2026-07-31): a field hidden here is still honoured by the read.
                    IsWorkItemTypesRequired = true,
                    // Column form, not the label form the SPIKE measured matching the whole table.
                    Placeholder = "active=true^assignment_group=Service Desk",
                    HelpText = "To get an encoded query, filter a list in ServiceNow, right-click the filter breadcrumb, and choose Copy query. Both ways this goes wrong are silent: a field name your instance does not know is dropped and the query then matches the whole table, which Lighthouse will not save; a value it does not know matches nothing, so the team looks empty.",
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
