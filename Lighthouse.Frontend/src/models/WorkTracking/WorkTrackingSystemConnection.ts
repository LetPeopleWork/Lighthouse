import type { IAdditionalFieldDefinition } from "./AdditionalFieldDefinition";
import type { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";
import type { IWriteBackMappingDefinition } from "./WriteBackMappingDefinition";

export type WorkTrackingSystemType =
	| "Jira"
	| "AzureDevOps"
	| "Linear"
	| "Csv"
	| "ServiceNow";

export const AuthenticationMethodKeys = {
	AzureDevOpsPat: "ado.pat",
	JiraCloud: "jira.cloud",
	JiraDataCenter: "jira.datacenter",
	JiraScopedToken: "jira.scopedtoken",
	LinearApiKey: "linear.apikey",
	ServiceNowBasic: "servicenow.basic",
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
	isPremium?: boolean;
}

export const ConnectionDefaultNames = [
	"parentOverrideForTeams",
	"estimationForTeams",
	"parentOverrideForPortfolios",
	"dependencyFieldForPortfolios",
	"sizeEstimateFieldForPortfolios",
	"featureOwnerFieldForPortfolios",
	"estimationForPortfolios",
] as const;

export type ConnectionDefaultName = (typeof ConnectionDefaultNames)[number];

/**
 * How many Teams and Portfolios read through a connection. Absent means nobody does. Followers counts,
 * per default, those that read the default rather than a value of their own; a default not listed has none.
 */
export interface IConnectionUsage {
	teams: number;
	portfolios: number;
	followers?: Partial<Record<ConnectionDefaultName, number>>;
}

export const isConnectionDefaultName = (
	name: string,
): name is ConnectionDefaultName =>
	(ConnectionDefaultNames as readonly string[]).includes(name);

/** The Additional Field each default points at, by id; null or absent means no default is set. */
export type IConnectionDefaults = Partial<
	Record<ConnectionDefaultName, number | null>
>;

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
	requiresReconnect?: boolean;
	concurrencyToken?: string;
	usedBy?: IConnectionUsage;
	defaults?: IConnectionDefaults;
	supportedDefaults?: ConnectionDefaultName[];
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
	requiresReconnect?: boolean;
	concurrencyToken?: string;
	usedBy?: IConnectionUsage;
	defaults?: IConnectionDefaults;
	supportedDefaults?: ConnectionDefaultName[];

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
		requiresReconnect?: boolean;
		concurrencyToken?: string;
		usedBy?: IConnectionUsage;
		defaults?: IConnectionDefaults;
		supportedDefaults?: ConnectionDefaultName[];
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
		this.requiresReconnect = data.requiresReconnect;
		this.concurrencyToken = data.concurrencyToken;
		this.usedBy = data.usedBy;
		this.defaults = data.defaults;
		this.supportedDefaults = data.supportedDefaults;
	}

	workTrackingSystemGetDataRetrievalDisplayName(): string {
		switch (this.workTrackingSystem) {
			case "Jira":
				return "JQL Query";
			case "AzureDevOps":
				return "WIQL Query";
			case "Linear":
				return "Linear Team";
			case "Csv":
				return "CSV File Content";
			case "ServiceNow":
				return "ServiceNow Query";
			default:
				return "Query";
		}
	}
}
