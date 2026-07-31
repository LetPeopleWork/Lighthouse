import type {
	IWorkTrackingSystemConnection,
	WorkTrackingSystemType,
} from "../WorkTracking/WorkTrackingSystemConnection";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";

// Twinned with ServiceNowWorkTrackingOptionNames.WorkItemTable on the backend.
export const serviceNowWorkItemTableOptionKey = "Work Item Table";

// Twinned with ServiceNowTableHierarchy.RootTables. serviceNowSchemaTwin.enforcement.test.ts
// compares both pairs as sets, so drift in either direction fails (Bug #5613, ADR-123 decision 7).
export const serviceNowHierarchyRootTables = ["task"];

// Everything the schema factories need from a connection. Narrower than the connection itself so a
// caller does not have to build one to ask what a settings screen should show.
type SchemaConnection = Pick<
	IWorkTrackingSystemConnection,
	"workTrackingSystem" | "options"
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
		// Conditional on the connection's table, decided in getDefaultTeamSchema below.
		isWorkItemTypesRequired: false,
		// No wizard: SPIKE Q8 measured table/field discovery unavailable below itil (ADR-116).
		wizardHint: null,
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

function readsSeveralKindsOfWork(connection: SchemaConnection): boolean {
	const workItemTable =
		connection.options.find(
			(option) => option.key === serviceNowWorkItemTableOptionKey,
		)?.value ?? "";

	return serviceNowHierarchyRootTables.includes(workItemTable);
}

// Takes the connection rather than the system type (ADR-123 decision 6), so the Work Item Table
// option key is looked up in exactly one place on this side of the stack, mirroring the backend.
// No component changes: they keep gating on isWorkItemTypesRequired !== false.
export function getDefaultTeamSchema(
	connection: SchemaConnection,
): IDataRetrievalSchema {
	const schema = teamSchemas[connection.workTrackingSystem] ?? defaultSchema;

	if (connection.workTrackingSystem !== "ServiceNow") {
		return schema;
	}

	// A table with descendants read unfiltered returns the whole instance's work, so a team rooted
	// there has to say which kinds are its own.
	return {
		...schema,
		isWorkItemTypesRequired: readsSeveralKindsOfWork(connection),
	};
}

// Takes the connection for symmetry and ignores everything but the system type: ADR-116 decision 5
// declines ServiceNow portfolios whatever table the connection reads.
export function getDefaultPortfolioSchema(
	connection: SchemaConnection,
): IDataRetrievalSchema {
	return portfolioSchemas[connection.workTrackingSystem] ?? defaultSchema;
}
