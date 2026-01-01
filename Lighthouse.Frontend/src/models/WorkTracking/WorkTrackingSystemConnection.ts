import type { IWorkTrackingSystemOption } from "./WorkTrackingSystemOption";

export type WorkTrackingSystemType = "Jira" | "AzureDevOps" | "Linear" | "Csv";

export type DataSourceType = "Query" | "File";

/**
 * Stable authentication method keys for Work Tracking System connections.
 * These keys should match the backend AuthenticationMethodKeys constants.
 */
export const AuthenticationMethodKeys = {
	AzureDevOpsPat: "ado.pat",
	JiraCloud: "jira.cloud",
	JiraDataCenter: "jira.datacenter",
	LinearApiKey: "linear.apikey",
	None: "none",
} as const;

export type AuthenticationMethodKey =
	(typeof AuthenticationMethodKeys)[keyof typeof AuthenticationMethodKeys];

/**
 * Represents an option required by an authentication method.
 */
export interface IAuthenticationMethodOption {
	key: string;
	displayName: string;
	isSecret: boolean;
	isOptional: boolean;
}

/**
 * Represents an authentication method with its display name and required options.
 */
export interface IAuthenticationMethod {
	key: string;
	displayName: string;
	options: IAuthenticationMethodOption[];
}

export interface IWorkTrackingSystemConnection {
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	dataSourceType: DataSourceType;
	authenticationMethodKey: string;
	authenticationMethodDisplayName?: string;
	availableAuthenticationMethods?: IAuthenticationMethod[];
}

export class WorkTrackingSystemConnection
	implements IWorkTrackingSystemConnection
{
	id: number | null;
	name: string;
	workTrackingSystem: WorkTrackingSystemType;
	options: IWorkTrackingSystemOption[];
	dataSourceType: DataSourceType;
	authenticationMethodKey: string;
	authenticationMethodDisplayName?: string;
	availableAuthenticationMethods?: IAuthenticationMethod[];

	constructor(data: {
		name: string;
		workTrackingSystem: WorkTrackingSystemType;
		options: IWorkTrackingSystemOption[];
		dataSourceType?: DataSourceType;
		id?: number | null;
		authenticationMethodKey?: string;
		authenticationMethodDisplayName?: string;
		availableAuthenticationMethods?: IAuthenticationMethod[];
	}) {
		this.id = data.id ?? null;
		this.name = data.name;
		this.workTrackingSystem = data.workTrackingSystem;
		this.options = data.options;
		this.dataSourceType = data.dataSourceType ?? "Query";
		this.authenticationMethodKey = data.authenticationMethodKey ?? "";
		this.authenticationMethodDisplayName = data.authenticationMethodDisplayName;
		this.availableAuthenticationMethods = data.availableAuthenticationMethods;
	}
}
