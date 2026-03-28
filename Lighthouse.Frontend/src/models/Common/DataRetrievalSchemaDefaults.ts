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
