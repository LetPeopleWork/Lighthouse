import { test as base } from '@playwright/test';
import { LighthousePage } from '../models/app/LighthousePage';
import { OverviewPage } from '../models/overview/OverviewPage';
import { createAzureDevOpsConnection, createJiraConnection, deleteWorkTrackingSystemConnection } from '../helpers/api/workTrackingSystemConnections';
import { createTeam, deleteTeam } from '../helpers/api/teams';
import { createProject, deleteProject } from '../helpers/api/projects';

type LighthouseFixtures = {
    overviewPage: OverviewPage;
};

type LighthouseWithDataFixtures = {
    testData: {
        projects: { name: string, id: number }[];
        teams: { name: string, id: number }[];
        connections: { name: string, id: number }[];
    };
}

function generateRandomString(): string {
    const characters = 'abcdefghijklmnopqrstuvwxyz';
    let result = '';
    for (let i = 0; i < 10; i++) {
        result += characters.charAt(Math.floor(Math.random() * characters.length));
    }
    return result;
}

export const test = base.extend<LighthouseFixtures>({
    overviewPage: async ({ page }, use) => {
        const lighthousePage = new LighthousePage(page);
        const overviewPage = await lighthousePage.open();

        await use(overviewPage);
    },
});

export const testWithData = test.extend<LighthouseWithDataFixtures>({
    testData: async ({ request }, use) => {
        const adoConnection = await createAzureDevOpsConnection(request, generateRandomString());
        const jiraConnection = await createJiraConnection(request, generateRandomString());

        const team1 = await createTeam(request, generateRandomString(), adoConnection.id, '[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"', ['User Story', 'Bug'], { toDo: ['New'], inProgress: ['Active', 'Resolved'], done: ['Closed'] });
        const team2 = await createTeam(request, generateRandomString(), adoConnection.id, '[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Cyber Sultans"', ['User Story', 'Bug'], { toDo: ['New'], inProgress: ['Active', 'Resolved'], done: ['Closed'] });
        const team3 = await createTeam(request, generateRandomString(), jiraConnection.id, 'project = "LGHTHSDMO" AND labels = "Lagunitas"', ['Story', 'Bug'], { toDo: ['To Do'], inProgress: ['In Progress'], done: ['Done'] });

        const project1 = await createProject(request, generateRandomString(), [team1], adoConnection.id);
        const project2 = await createProject(request, generateRandomString(), [team1, team2], adoConnection.id);
        const project3 = await createProject(request, generateRandomString(), [team3], jiraConnection.id);

        await use({
            projects: [project1, project2, project3],
            teams: [team1, team2, team3],
            connections: [adoConnection, jiraConnection]
        });

        await deleteProject(request, project1.id);
        await deleteProject(request, project2.id);
        await deleteProject(request, project3.id);

        await deleteTeam(request, team1.id);
        await deleteTeam(request, team2.id);
        await deleteTeam(request, team3.id);

        await deleteWorkTrackingSystemConnection(request, adoConnection.id);
        await deleteWorkTrackingSystemConnection(request, jiraConnection.id);
    },
});

export { expect } from '@playwright/test';