import { type APIRequestContext, test as base } from "@playwright/test";
import { createProject } from "../helpers/api/projects";
import { createTeam, updateTeam } from "../helpers/api/teams";
import {
	createAzureDevOpsConnection,
	createJiraConnection,
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

type TestData = {
	projects: ModelIdentifier[];
	teams: ModelIdentifier[];
	connections: ModelIdentifier[];
};

export type ModelIdentifier = {
	id: number;
	name: string;
};

async function clearConfiguration(request: APIRequestContext): Promise<void> {
	await request.delete("/api/configuration/clear");
}

async function generateTestData(
	request: APIRequestContext,
	teamsToUpdate: number[],
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
		toDo: ["New", "Planned"],
		doing: ["Active", "Resolved"],
		done: ["Closed"],
	};
	const jiraStates = {
		toDo: ["To Do"],
		doing: ["In Progress"],
		done: ["Done"],
	};

	const historicalDate = new Date(Date.now() - 5 * 24 * 60 * 60 * 1000);
	const historicalDateString = historicalDate.toISOString().slice(0, 10);

	const lighthouseDevTeam = await createTeam(
		request,
		generateRandomName(),
		adoConnection.id,
		`[System.TeamProject] = "Lighthouse"`,
		["User Story", "Bug"],
		adoStates,
		["Azure DevOps"],
	);
	const team2 = await createTeam(
		request,
		generateRandomName(),
		adoConnection.id,
		`[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Cyber Sultans" AND ([System.State] <> "Closed"  OR [System.Parent] <> "" OR [System.ChangedDate] >= "${historicalDateString}")`,
		["User Story", "Bug"],
		adoStates,
		["Azure DevOps"],
	);
	const team3 = await createTeam(
		request,
		generateRandomName(),
		jiraConnection.id,
		'project = "LGHTHSDMO" AND labels = "Lagunitas"',
		["Story", "Bug"],
		jiraStates,
		["Jira"],
	);

	const teamsToProcess: ModelIdentifier[] = [];
	if (teamsToUpdate.includes(0)) {
		teamsToProcess.push(lighthouseDevTeam);
	}

	if (teamsToUpdate.includes(1)) {
		teamsToProcess.push(team2);
	}

	if (teamsToUpdate.includes(2)) {
		teamsToProcess.push(team3);
	}

	await updateTeamData(request, teamsToProcess);

	const project1 = await createProject(
		request,
		generateRandomName(),
		[lighthouseDevTeam],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse"',
		["Epic"],
		adoStates,
		["Azure DevOps"],
	);
	const project2 = await createProject(
		request,
		generateRandomName(),
		[lighthouseDevTeam, team2],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo"',
		["Epic"],
		adoStates,
		["Azure DevOps"],
	);
	const project3 = await createProject(
		request,
		generateRandomName(),
		[team3],
		jiraConnection.id,
		'project = "LGHTHSDMO" AND fixVersion = "Oberon Initiative"',
		["Epic"],
		jiraStates,
		["Jira"],
	);

	return {
		projects: [project1, project2, project3],
		teams: [lighthouseDevTeam, team2, team3],
		connections: [adoConnection, jiraConnection],
	};
}

export const test = base.extend<LighthouseFixtures>({
	overviewPage: async ({ page, request }, use) => {
		const lighthousePage = new LighthousePage(page);
		const overviewPage = await lighthousePage.open();

		await use(overviewPage);

		await clearConfiguration(request);
	},
});

export function testWithUpdatedTeams(teamsToUpdate: number[] = [0, 1, 2]) {
	return test.extend<LighthouseWithDataFixtures>({
		testData: async ({ request }, use) => {
			const data = await generateTestData(request, teamsToUpdate);
			await use(data);
			await clearConfiguration(request);
		},
	});
}

export const testWithData = test.extend<LighthouseWithDataFixtures>({
	testData: async ({ request }, use) => {
		const data = await generateTestData(request, []);

		await use(data);

		await clearConfiguration(request);
	},
});

export { expect } from "@playwright/test";

async function updateTeamData(
	request: APIRequestContext,
	teams: ModelIdentifier[],
): Promise<void> {
	for (const team of teams) {
		await updateTeam(request, team.id);
	}
	const updatedTeams: ModelIdentifier[] = [];

	while (updatedTeams.length < teams.length) {
		for (const team of teams) {
			if (!updatedTeams.some((t) => t.id === team.id)) {
				const response = await request.get(
					`/api/teams/${team.id}/metrics/featuresInProgress`,
				);
				const featuresInProgress = await response.json();

				if (featuresInProgress.length > 0) {
					updatedTeams.push(team);
				}
			}

			await new Promise((resolve) => setTimeout(resolve, 5000));
		}
	}
}
