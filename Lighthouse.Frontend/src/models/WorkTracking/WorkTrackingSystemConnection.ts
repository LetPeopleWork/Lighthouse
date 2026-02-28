import type { IAdditionalFieldDefinition } from "./AdditionalFieldDefinition";
import type { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";
import type { IWriteBackMappingDefinition } from "./WriteBackMappingDefinition";

export type WorkTrackingSystemType = "Jira" | "AzureDevOps" | "Linear" | "Csv";

export const AuthenticationMethodKeys = {
	AzureDevOpsPat: "ado.pat",
	JiraCloud: "jira.cloud",
	JiraDataCenter: "jira.datacenter",
	LinearApiKey: "linear.apikey",
	None: "none",
} as const;

export type AuthenticationMethodKey =
	(typeof AuthenticationMethodKeys)[keyof typeof AuthenticationMethodKeys];

export interface IAuthenticationMethodOption {
	key: string;
	displayName: string;
	isSecret: boolean;
	isOptional: boolean;
}

export interface IAuthenticationMethod {
	key: string;
	displayName: string;
	options: IAuthenticationMethodOption[];
}

export interface IWorkTrackingSystemConnection {
	workTrackingSystemGetDataRetrievalDisplayName(): string;
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	authenticationMethodKey: string;
	authenticationMethodDisplayName?: string;
	availableAuthenticationMethods?: IAuthenticationMethod[];
	additionalFieldDefinitions: IAdditionalFieldDefinition[];
	writeBackMappingDefinitions: IWriteBackMappingDefinition[];
}

export class WorkTrackingSystemConnection
	implements IWorkTrackingSystemConnection
{
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	authenticationMethodKey: string;
	authenticationMethodDisplayName?: string;
	availableAuthenticationMethods?: IAuthenticationMethod[];
	additionalFieldDefinitions: IAdditionalFieldDefinition[];
	writeBackMappingDefinitions: IWriteBackMappingDefinition[];

	constructor(data: {
		name: string;
		workTrackingSystem: WorkTrackingSystemType;
		options: IWorkTrackingSystemOption[];
		id?: number | null;
		authenticationMethodKey?: string;
		authenticationMethodDisplayName?: string;
		availableAuthenticationMethods?: IAuthenticationMethod[];
		additionalFieldDefinitions?: IAdditionalFieldDefinition[];
		writeBackMappingDefinitions?: IWriteBackMappingDefinition[];
	}) {
		this.id = data.id ?? null;
		this.name = data.name;
		this.workTrackingSystem = data.workTrackingSystem;
		this.options = data.options;
		this.authenticationMethodKey = data.authenticationMethodKey ?? "";
		this.authenticationMethodDisplayName = data.authenticationMethodDisplayName;
		this.availableAuthenticationMethods = data.availableAuthenticationMethods;
		this.additionalFieldDefinitions = data.additionalFieldDefinitions ?? [];
		this.writeBackMappingDefinitions = data.writeBackMappingDefinitions ?? [];
	}

	workTrackingSystemGetDataRetrievalDisplayName(): string {
		switch (this.workTrackingSystem) {
			case "Jira":
				return "JQL Query";
			case "AzureDevOps":
				return "WIQL Query";
			case "Linear":
				return "Linear Team/Project";
			case "Csv":
				return "CSV File Content";
			default:
				return "Query";
		}
	}
}
