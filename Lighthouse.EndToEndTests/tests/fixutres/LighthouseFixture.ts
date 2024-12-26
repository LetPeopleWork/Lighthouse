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

        const binaryBlazers = await createTeam(request, 'Binary Blazers', adoConnection.id, '[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Binary Blazers"', ['User Story', 'Bug'], { toDo: ['New'], inProgress: ['Active', 'Resolved'], done: ['Closed'] });
        const cyberSultans = await createTeam(request, 'Cyber Sultans', adoConnection.id, '[System.TeamProject] = "Lighthouse Demo" AND [System.AreaPath] = "Lighthouse Demo\\Cyber Sultans"', ['User Story', 'Bug'], { toDo: ['New'], inProgress: ['Active', 'Resolved'], done: ['Closed'] });
        const lagunitas = await createTeam(request, 'Lagunitas', jiraConnection.id, 'project = "LGHTHSDMO" AND labels = "Lagunitas"', ['Story', 'Bug'], { toDo: ['To Do'], inProgress: ['In Progress'], done: ['Done'] });

        const project1 = await createProject(request, 'Release 1.33.7', [binaryBlazers], adoConnection.id);
        const project2 = await createProject(request, 'Release Codename Daniel', [binaryBlazers, cyberSultans], adoConnection.id);
        const project3 = await createProject(request, 'Oberon', [lagunitas], jiraConnection.id);

        await use({
            projects: [project1, project2, project3],
            teams: [binaryBlazers, cyberSultans, lagunitas],
            connections: [adoConnection, jiraConnection]
        });

        await deleteProject(request, project1.id);
        await deleteProject(request, project2.id);
        await deleteProject(request, project3.id);

        await deleteTeam(request, binaryBlazers.id);
        await deleteTeam(request, cyberSultans.id);
        await deleteTeam(request, lagunitas.id);

        await deleteWorkTrackingSystemConnection(request, adoConnection.id);
        await deleteWorkTrackingSystemConnection(request, jiraConnection.id);
    },
});

export { expect } from '@playwright/test';