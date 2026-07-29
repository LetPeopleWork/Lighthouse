import type { WorkTrackingSystemType } from "../WorkTracking/WorkTrackingSystemConnection";
import type { IDataRetrievalSchema } from "./DataRetrievalSchema";

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
		// The configured table is the type for an ITSM-first read (C-3, revisit in slice 02).
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

export function getDefaultTeamSchema(
	systemType: WorkTrackingSystemType,
): IDataRetrievalSchema {
	return teamSchemas[systemType] ?? defaultSchema;
}

export function getDefaultPortfolioSchema(
	systemType: WorkTrackingSystemType,
): IDataRetrievalSchema {
	return portfolioSchemas[systemType] ?? defaultSchema;
}
