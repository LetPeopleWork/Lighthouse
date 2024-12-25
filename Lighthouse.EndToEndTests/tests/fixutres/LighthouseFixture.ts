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

export const test = base.extend<LighthouseFixtures>({
    overviewPage: async ({ page }, use) => {
        const lighthousePage = new LighthousePage(page);
        const overviewPage = await lighthousePage.open();

        await use(overviewPage);
    },
});

export const testWithData = test.extend<LighthouseWithDataFixtures>({
    testData: async ({ request }, use) => {
        const adoConnection = await createAzureDevOpsConnection(request, 'Azure DevOps Connection');
        const jiraConnection = await createJiraConnection(request, 'Jira Connection');

        const team1 = await createTeam(request, 'Space Hamsters', adoConnection.id);
        const team2 = await createTeam(request, 'Apollo', adoConnection.id);
        const team3 = await createTeam(request, 'Sputnik', jiraConnection.id);

        const project1 = await createProject(request, 'Moon', [team1], adoConnection.id);
        const project2 = await createProject(request, 'Mars', [team1, team2], adoConnection.id);
        const project3 = await createProject(request, 'Beyond', [team3], jiraConnection.id);

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