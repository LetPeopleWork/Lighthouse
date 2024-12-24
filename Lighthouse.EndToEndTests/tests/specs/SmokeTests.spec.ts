import { expect, test } from '@playwright/test';
import { LighthousePage } from '../models/app/LighthousePage';
import { createProject, deleteProject } from '../helpers/api/projects';
import { createAzureDevOpsConnection, deleteWorkTrackingSystemConnection } from '../helpers/api/workTrackingSystemConnections';
import { createTeam, deleteTeam } from '../helpers/api/teams';

let adoConnection: { id: number, name: string };
let team: { id: number, name: string };
let project1: { id: number, name: string };
let project2: { id: number, name: string };

test.beforeAll(async ({ request }) => {
    adoConnection = await createAzureDevOpsConnection(request, 'Azure DevOps Connection');
    team = await createTeam(request, 'Team 1', adoConnection.id);
    project1 = await createProject(request, 'Project 1', [team], adoConnection.id);
    project2 = await createProject(request, 'Project 2', [team], adoConnection.id);
});

test.afterAll(async ({ request }) => {    
    await deleteProject(request, project1.id);
    await deleteProject(request, project2.id);

    await deleteTeam(request, team.id);

    await deleteWorkTrackingSystemConnection(request, adoConnection.id);
});

test('should open all pages from the header', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);

    await lighthousePage.open();
    await lighthousePage.goToTeams();
    await lighthousePage.goToProjects();
    await lighthousePage.goToSettings();
});

test('should show all projects on dashboard', async ({ page }) => {

    await test.step('Open dashboard', async () => {
        const lighthousePage = new LighthousePage(page);
        const overviewPage = await lighthousePage.open();

        await overviewPage.search('Project');
        expect(await overviewPage.isProjectAvailable(project1)).toBeTruthy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeTruthy();

        await overviewPage.search('Project 1');
        expect(await overviewPage.isProjectAvailable(project1)).toBeTruthy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeFalsy();

        await overviewPage.search('Jambalaya');
        expect(await overviewPage.isProjectAvailable(project1)).toBeFalsy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeFalsy();

        await overviewPage.search('');
        expect(await overviewPage.isProjectAvailable(project1)).toBeTruthy();
        expect(await overviewPage.isProjectAvailable(project2)).toBeTruthy();

    });
});

test('should open the contributors page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open();
    const contributorsPage = await lighthousePage.goToContributors();

    const pageTitle = await contributorsPage.title();
    expect(pageTitle).toContain('CONTRIBUTORS.md');
});

test('should open the issues page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open();
    const reportIssuePage = await lighthousePage.goToReportIssue();

    const pageTitle = await reportIssuePage.title();
    expect(pageTitle).toContain('Issues');
});

test('should open the youtube page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open();
    const youtubePage = await lighthousePage.goToYoutube();

    const pageTitle = await youtubePage.title();
    expect(pageTitle).toContain('LetPeopleWork');
});

test('should open the blog posts page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open();
    const blogPostPage = await lighthousePage.goToBlogPosts();

    const pageTitle = await blogPostPage.title();
    expect(pageTitle).toContain('Let People Work');
    expect(blogPostPage.url()).toBe('https://blog.letpeople.work/');
});

test('should open the github page', async ({ page }) => {
    const lighthousePage = new LighthousePage(page);
    await lighthousePage.open();
    const gitHubPage = await lighthousePage.goToGitHub();

    const pageTitle = await gitHubPage.title();
    expect(pageTitle).toBe('LetPeopleWork · GitHub');
});