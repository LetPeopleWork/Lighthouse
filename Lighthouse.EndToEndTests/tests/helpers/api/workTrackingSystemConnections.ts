import type { APIRequestContext } from "@playwright/test";
import { TestConfig } from "../../../playwright.config";

type WorkTrackingSystemOption = {
	key: string;
	value: string;
	isSecret: boolean;
};

export async function createAzureDevOpsConnection(
	api: APIRequestContext,
	connectionName: string,
): Promise<{ id: number; name: string }> {
	const options: WorkTrackingSystemOption[] = [];
	options.push({
		key: "Azure DevOps Url",
		value: "https://dev.azure.com/letpeoplework",
		isSecret: false,
	});
	options.push({
		key: "Personal Access Token",
		value: TestConfig.AzureDevOpsToken,
		isSecret: true,
	});

	return createWorkTrackingSystemConnection(api, {
		name: connectionName,
		workTrackingSystem: "AzureDevOps",
		options: options,
	});
}

export async function createJiraConnection(
	api: APIRequestContext,
	connectionName: string,
): Promise<{ id: number; name: string }> {
	const options: WorkTrackingSystemOption[] = [];
	options.push({
		key: "Jira Url",
		value: "https://letpeoplework.atlassian.net",
		isSecret: false,
	});
	options.push({
		key: "Username",
		value: "benjhuser@gmail.com",
		isSecret: false,
	});
	options.push({
		key: "Api Token",
		value: TestConfig.JiraToken,
		isSecret: true,
	});

	return createWorkTrackingSystemConnection(api, {
		name: connectionName,
		workTrackingSystem: "Jira",
		options: options,
	});
}

async function createWorkTrackingSystemConnection(
	api: APIRequestContext,
	workTrackingSystemConnectionData: {
		name: string;
		workTrackingSystem: string;
		options: WorkTrackingSystemOption[];
	},
): Promise<{ id: number; name: string }> {
	const response = await api.post("/api/WorkTrackingSystemConnections", {
		data: {
			id: 0,
			name: workTrackingSystemConnectionData.name,
			workTrackingSystem: workTrackingSystemConnectionData.workTrackingSystem,
			options: workTrackingSystemConnectionData.options,
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
