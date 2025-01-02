import { type APIRequestContext, test as base } from "@playwright/test";
import { createProject, deleteProject } from "../helpers/api/projects";
import { createTeam, deleteTeam, updateTeam } from "../helpers/api/teams";
import {
	createAzureDevOpsConnection,
	createJiraConnection,
	deleteWorkTrackingSystemConnection,
} from "../helpers/api/workTrackingSystemConnections";
import { generateRandomName } from "../helpers/names";
import { LighthousePage } from "../models/app/LighthousePage";
import type { OverviewPage } from "../models/overview/OverviewPage";

type LighthouseFixtures = {
	overviewPage: OverviewPage;
};

type LighthouseWithDataFixtures = {
	testData: TestData;
};

type LighthouseWithDefaultSettingsFixtures = {
	defaultSettings: DefaultSettings;
};

type TestData = {
	projects: ModelIdentifier[];
	teams: ModelIdentifier[];
	connections: ModelIdentifier[];
};

type DefaultSettings = {
	defaultProjectSettings: JsonValue;
	defaultTeamSettings: JsonValue;
};

type JsonValue =
	| string
	| number
	| boolean
	| null
	| { [key: string]: JsonValue }
	| JsonValue[];

type ModelIdentifier = {
	id: number;
	name: string;
};

async function deleteTestData(
	request: APIRequestContext,
	testData: TestData,
): Promise<void> {
	for (const project of testData.projects) {
		await deleteProject(request, project.id);
	}

	for (const team of testData.teams) {
		await deleteTeam(request, team.id);
	}

	for (const connection of testData.connections) {
		await deleteWorkTrackingSystemConnection(request, connection.id);
	}
}

async function getDefaultSettings(
	request: APIRequestContext,
): Promise<DefaultSettings> {
	let response = await request.get("/api/AppSettings/DefaultTeamSettings");
	const defaultTeamSettings = await response.json();

	response = await request.get("/api/AppSettings/DefaultProjectSettings");
	const defaultProjectSettings = await response.json();

	return {
		defaultProjectSettings: defaultProjectSettings,
		defaultTeamSettings: defaultTeamSettings,
	};
}

async function restoreDefaultTeamSettings(
	request: APIRequestContext,
	defaultSettings: DefaultSettings,
): Promise<void> {
	await request.put("/api/AppSettings/DefaultTeamSettings", {
		data: defaultSettings.defaultTeamSettings,
	});
	await request.put("/api/AppSettings/DefaultProjectSettings", {
		data: defaultSettings.defaultProjectSettings,
	});
}

async function generateTestData(
	request: APIRequestContext,
	updateTeams: boolean,
): Promise<TestData> {
	const adoConnection = await createAzureDevOpsConnection(
		request,
		generateRandomName(),
	);
	const jiraConnection = await createJiraConnection(
		request,
		generateRandomName(),
	);

	const adoStates = {
		toDo: ["New"],
		doing: ["Active", "Resolved"],
		done: ["Closed"],
	};
	const jiraStates = {
		toDo: ["To Do"],
		doing: ["In Progress"],
		done: ["Done"],
	};

	const team1 = await createTeam(
		request,
		generateRandomName(),
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"',
		["User Story", "Bug"],
		adoStates,
	);
	const team2 = await createTeam(
		request,
		generateRandomName(),
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Cyber Sultans"',
		["User Story", "Bug"],
		adoStates,
	);
	const team3 = await createTeam(
		request,
		generateRandomName(),
		jiraConnection.id,
		'project = "LGHTHSDMO" AND labels = "Lagunitas"',
		["Story", "Bug"],
		jiraStates,
	);

	if (updateTeams) {
		await updateTeamData(request, [team1, team2, team3]);
	}

	const project1 = await createProject(
		request,
		generateRandomName(),
		[team1],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release 1.33.7"',
		["Epic"],
		adoStates,
	);
	const project2 = await createProject(
		request,
		generateRandomName(),
		[team1, team2],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo" AND [System.Tags] CONTAINS "Release Codename Daniel"',
		["Epic"],
		adoStates,
	);
	const project3 = await createProject(
		request,
		generateRandomName(),
		[team3],
		jiraConnection.id,
		'project = "LGHTHSDMO" AND fixVersion = "Oberon Initiative"',
		["Epic"],
		jiraStates,
	);

	return {
		projects: [project1, project2, project3],
		teams: [team1, team2, team3],
		connections: [adoConnection, jiraConnection],
	};
}

export const test = base.extend<LighthouseFixtures>({
	overviewPage: async ({ page }, use) => {
		const lighthousePage = new LighthousePage(page);
		const overviewPage = await lighthousePage.open();

		await use(overviewPage);
	},
});

export const testWithRestoredDefaultSettings =
	test.extend<LighthouseWithDefaultSettingsFixtures>({
		defaultSettings: async ({ request }, use) => {
			const defaultSettings = await getDefaultSettings(request);

			await use(defaultSettings);

			await restoreDefaultTeamSettings(request, defaultSettings);
		},
	});

export const testWithUpdatedTeams = test.extend<LighthouseWithDataFixtures>({
	testData: async ({ request }, use) => {
		const data = await generateTestData(request, true);

		await use(data);

		await deleteTestData(request, data);
	},
});

export const testWithData = test.extend<LighthouseWithDataFixtures>({
	testData: async ({ request }, use) => {
		const data = await generateTestData(request, false);

		await use(data);

		await deleteTestData(request, data);
	},
});

export { expect } from "@playwright/test";

async function updateTeamData(
	request: APIRequestContext,
	teams: ModelIdentifier[],
): Promise<void> {
	const updateTime = new Date(new Date().toUTCString());

	for (const team of teams) {
		await updateTeam(request, team.id);
	}

	const updatedTeams: ModelIdentifier[] = [];

	while (updatedTeams.length < teams.length) {
		for (const team of teams) {
			if (!updatedTeams.some((t) => t.id === team.id)) {
				const response = await request.get(`/api/Teams/${team.id}`);
				const updatedTeam = await response.json();

				if (
					new Date(updatedTeam.lastUpdated).getUTCMilliseconds() >
						updateTime.getUTCMilliseconds() &&
					updatedTeam.featuresInProgress.length > 0
				) {
					updatedTeams.push(updatedTeam);
				}
			}

			await new Promise((resolve) => setTimeout(resolve, 1000));
		}
	}
}
