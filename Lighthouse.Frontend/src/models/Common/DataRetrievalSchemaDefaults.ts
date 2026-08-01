import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../WorkTracking/WorkTrackingSystemConnection";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";

// Everything the schema factories need from a connection. Narrower than the connection itself so a
// caller does not have to build one to ask what a settings screen should show.
type SchemaConnection = Pick<
	IWorkTrackingSystemConnection,
	"workTrackingSystem"
>;

const defaultSchema: IDataRetrievalSchema = {
	key: "query",
	displayLabel: "Query",
	inputKind: "freetext",
	isRequired: true,
	isWorkItemTypesRequired: true,
	wizardHint: null,
};

const teamSchemas: Record<WorkTrackingSystemType, IDataRetrievalSchema> = {
	AzureDevOps: {
		key: "ado.wiql",
		displayLabel: "WIQL Query",
		inputKind: "freetext",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "ado-team-wizard",
	},
	Jira: {
		key: "jira.jql",
		displayLabel: "JQL Query",
		inputKind: "freetext",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "jira-team-wizard",
	},
	Linear: {
		key: "linear.team",
		displayLabel: "Linear Team",
		inputKind: "wizard-select",
		isRequired: true,
		isWorkItemTypesRequired: false,
		wizardHint: "linear-team-select",
	},
	Csv: {
		key: "csv.filedata",
		displayLabel: "CSV File Content",
		inputKind: "file-upload",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "csv-team-wizard",
	},
	ServiceNow: {
		key: "servicenow.query",
		displayLabel: "ServiceNow Query (Encoded Query)",
		inputKind: "freetext",
		isRequired: true,
		// Always, whatever table the connection reads (ADR-123 decision 6, amended 2026-07-31):
		// a field hidden here is still honoured by the read.
		isWorkItemTypesRequired: true,
		// No wizard: SPIKE Q8 measured table/field discovery unavailable below itil (ADR-116).
		wizardHint: null,
		// Non-reference fields only: a reference field stores a sys_id, so matching it against a
		// display label selects nothing (#5610).
		placeholder: "active=true^priority=1",
		helpText:
			"To get an encoded query, filter a list in ServiceNow, right-click the filter breadcrumb, and choose Copy query. Both ways this goes wrong are silent: a field name your instance does not know is dropped and the query then matches the whole table, which Lighthouse will not save; a value it does not know matches nothing, so the team looks empty.",
	},
};

const portfolioSchemas: Record<WorkTrackingSystemType, IDataRetrievalSchema> = {
	AzureDevOps: {
		key: "ado.wiql",
		displayLabel: "WIQL Query",
		inputKind: "freetext",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "ado-portfolio-wizard",
	},
	Jira: {
		key: "jira.jql",
		displayLabel: "JQL Query",
		inputKind: "freetext",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "jira-portfolio-wizard",
	},
	Linear: {
		key: "linear.projects",
		displayLabel: "Linear Projects",
		inputKind: "none",
		isRequired: false,
		isWorkItemTypesRequired: false,
		wizardHint: null,
	},
	Csv: {
		key: "csv.filedata",
		displayLabel: "CSV File Content",
		inputKind: "file-upload",
		isRequired: true,
		isWorkItemTypesRequired: true,
		wizardHint: "csv-portfolio-wizard",
	},
	// SPIKE Q5 measured no forecastable rollup in ITSM, so the capability is declined here
	// rather than offered as a field that leads nowhere (ADR-116).
	ServiceNow: {
		key: "servicenow.query",
		displayLabel: "Not supported for ServiceNow",
		inputKind: "none",
		isRequired: false,
		isWorkItemTypesRequired: false,
		wizardHint: null,
	},
};

export function getDefaultTeamSchema(
	connection: SchemaConnection,
): IDataRetrievalSchema {
	return teamSchemas[connection.workTrackingSystem] ?? defaultSchema;
}

export function getDefaultPortfolioSchema(
	connection: SchemaConnection,
): IDataRetrievalSchema {
	return portfolioSchemas[connection.workTrackingSystem] ?? defaultSchema;
}
