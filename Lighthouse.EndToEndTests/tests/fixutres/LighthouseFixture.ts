import { type APIRequestContext, test as base } from "@playwright/test";
import { createPortfolio } from "../helpers/api/portfolios";
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
	portfolios: ModelIdentifier[];
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

	const project1 = await createPortfolio(
		request,
		generateRandomName(),
		[lighthouseDevTeam],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse"',
		["Epic"],
		adoStates,
		["Azure DevOps"],
	);
	const project2 = await createPortfolio(
		request,
		generateRandomName(),
		[lighthouseDevTeam, team2],
		adoConnection.id,
		'[System.TeamProject] = "Lighthouse Demo"',
		["Epic"],
		adoStates,
		["Azure DevOps"],
	);
	const project3 = await createPortfolio(
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
		portfolios: [project1, project2, project3],
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
	// Trigger updates for each team
	for (const team of teams) {
		await updateTeam(request, team.id);
	}

	// Give UpdateController a moment to register the work
	await new Promise((resolve) => setTimeout(resolve, 1000));

	const POLL_INTERVAL_MS = 3000;
	const TIMEOUT_MS = 5 * 60 * 1000; // 5 minutes
	const deadline = Date.now() + TIMEOUT_MS;

	while (true) {
		const response = await request.get(`/api/update/status`);
		if (!response.ok()) {
			throw new Error(`Failed to fetch update status: ${response.status}`);
		}

		type UpdateStatusDTO = {
			hasActiveUpdates: boolean;
			activeCount: number;
		};

		const body = (await response.json()) as UpdateStatusDTO;

		if (!body.hasActiveUpdates && body.activeCount === 0) {
			// All updates completed
			return;
		}

		console.log(
			`Waiting for background updates to complete. Active updates: ${body.activeCount}`,
		);

		if (Date.now() > deadline) {
			throw new Error("Timed out waiting for background updates to complete");
		}

		await new Promise((resolve) => setTimeout(resolve, POLL_INTERVAL_MS));
	}
}
