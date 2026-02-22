import type { APIRequestContext } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";

type WorkTrackingSystemOption = {
	key: string;
	value: string;
	isSecret: boolean;
	isOptional?: boolean;
};

type AdditionalFieldDefinitions = {
	id: number;
	reference: string;
	displayName: string;
};

export async function createAzureDevOpsConnection(
	api: APIRequestContext,
	connectionName: string,
): Promise<{ id: number; name: string }> {
	const options: WorkTrackingSystemOption[] = [];
	options.push(
		{
			key: "Azure DevOps Url",
			value: "https://dev.azure.com/letpeoplework",
			isSecret: false,
		},
		{
			key: "Personal Access Token",
			value: TestConfig.AzureDevOpsToken,
			isSecret: true,
		},
		{
			key: "Request Timeout In Seconds",
			value: "100",
			isSecret: false,
			isOptional: true,
		},
	);

	const additionalFieldDefinitions: AdditionalFieldDefinitions[] = [
		{
			id: 0,
			reference: "System.AreaPath",
			displayName: "Area Path",
		},
	];

	return createWorkTrackingSystemConnection(api, {
		name: connectionName,
		workTrackingSystem: "AzureDevOps",
		authenticationMethodKey: "ado.pat",
		options: options,
		additionalFieldDefinitions: additionalFieldDefinitions,
	});
}

export async function createJiraConnection(
	api: APIRequestContext,
	connectionName: string,
): Promise<{ id: number; name: string }> {
	const options: WorkTrackingSystemOption[] = [];
	options.push(
		{
			key: "Jira Url",
			value: "https://letpeoplework.atlassian.net",
			isSecret: false,
		},
		{
			key: "Username",
			value: "atlassian.pushchair@huser-berta.com",
			isSecret: false,
		},
		{
			key: "Api Token",
			value: TestConfig.JiraToken,
			isSecret: true,
		},
		{
			key: "Request Timeout In Seconds",
			value: "100",
			isSecret: false,
			isOptional: true,
		},
	);

	const additionalFieldDefinitions: AdditionalFieldDefinitions[] = [
		{
			id: 0,
			reference: "fixVersions",
			displayName: "Fix Versions",
		},
		{
			id: 0,
			reference: "Size Estimate",
			displayName: "Story Points",
		},
	];

	return createWorkTrackingSystemConnection(api, {
		name: connectionName,
		workTrackingSystem: "Jira",
		authenticationMethodKey: "jira.cloud",
		options: options,
		additionalFieldDefinitions: additionalFieldDefinitions,
	});
}

async function createWorkTrackingSystemConnection(
	api: APIRequestContext,
	workTrackingSystemConnectionData: {
		name: string;
		workTrackingSystem: string;
		authenticationMethodKey: string;
		options: WorkTrackingSystemOption[];
		additionalFieldDefinitions: AdditionalFieldDefinitions[];
	},
): Promise<{ id: number; name: string }> {
	const response = await api.post("/api/WorkTrackingSystemConnections", {
		data: {
			id: 0,
			name: workTrackingSystemConnectionData.name,
			workTrackingSystem: workTrackingSystemConnectionData.workTrackingSystem,
			authenticationMethodKey:
				workTrackingSystemConnectionData.authenticationMethodKey,
			options: workTrackingSystemConnectionData.options,
			additionalFieldDefinitions:
				workTrackingSystemConnectionData.additionalFieldDefinitions,
		},
	});

	return response.json();
}

export async function deleteWorkTrackingSystemConnection(
	api: APIRequestContext,
	workTrackingSystemConnectionId: number,
) {
	await api.delete(
		`/api/WorkTrackingSystemConnections/${workTrackingSystemConnectionId}`,
	);
}

export async function deleteWorkTrackingSystemConnectionByName(
	api: APIRequestContext,
	workTrackingSystemConnectionName: string,
) {
	const response = await api.get("/api/WorkTrackingSystemConnections");
	const workTrackingSystems = await response.json();

	const systemToDelete = workTrackingSystems.find(
		(system: { name: string }) =>
			system.name === workTrackingSystemConnectionName,
	);
	if (systemToDelete) {
		await deleteWorkTrackingSystemConnection(api, systemToDelete.id);
	}
}
